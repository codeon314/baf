using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BrowserAutomationFramework.Generators;
using BrowserAutomationFramework.Models;

namespace BrowserAutomationFramework.Server
{
    /// <summary>
    /// Local HTTP and WebSocket IPC server that manages connections to browser clients (via Chrome Extension or Tampermonkey userscripts).
    /// </summary>
    public class AutomationServer : IDisposable
    {
        private static readonly JsonSerializerOptions HighDepthSerializerOptions = new()
        {
            MaxDepth = 1024
        };

        private static readonly JsonDocumentOptions HighDepthDocumentOptions = new()
        {
            MaxDepth = 1024
        };

        private readonly int _port;
        private HttpListener? _listener;
        private CancellationTokenSource? _serverCts;
        private readonly ConcurrentDictionary<string, BrowserClient> _clients = new();
        private System.Threading.Timer? _cleanupTimer;
        private BrowserClient? _activeClient;
        private bool _disposed;

        // Chrome Extension State
        private WebSocket? _extensionWebSocket;
        private readonly SemaphoreSlim _extWsSendLock = new(1, 1);
        private readonly ConcurrentQueue<object> _pendingExtensionCommands = new();
        private List<BrowserTabInfo> _extensionTabs = new();
        private readonly object _extensionTabsLock = new();
        private DateTime _lastExtensionSeen = DateTime.MinValue;
        private bool _wasExtensionConnected;

        /// <summary>
        /// Gets the TCP port on which this <see cref="AutomationServer"/> listens.
        /// </summary>
        public int Port => _port;

        /// <summary>
        /// Gets whether the server is currently listening for connections.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets or sets whether switching to a Chrome Extension tab automatically detaches other hooked tabs and selects the new tab as <see cref="ActiveClient"/>.
        /// </summary>
        public bool ExclusiveTabMode { get; set; } = true;

        /// <summary>
        /// Gets or sets the currently active/selected <see cref="BrowserClient"/> managed by this server.
        /// </summary>
        public BrowserClient? ActiveClient
        {
            get => _activeClient;
            set
            {
                if (!ReferenceEquals(_activeClient, value))
                {
                    _activeClient = value;
                    _activeClient?.SetTrackedHighlightSelector(null);
                    OnActiveClientChanged?.Invoke(this, _activeClient);
                }
            }
        }

        /// <summary>
        /// Gets the browser tab ID currently attached by the Chrome Extension, if any.
        /// </summary>
        public int? ActiveExtensionTabId { get; private set; }

        /// <summary>
        /// Gets or sets the browser tab ID most recently requested for an interactive switch, used to auto-select the client upon connection.
        /// </summary>
        public int? PendingAutoSelectTabId { get; set; }

        /// <summary>
        /// Gets whether the BAF Chrome Extension is currently connected to this server.
        /// </summary>
        public bool IsExtensionConnected =>
            (_extensionWebSocket != null && _extensionWebSocket.State == WebSocketState.Open) ||
            (DateTime.UtcNow - _lastExtensionSeen < TimeSpan.FromSeconds(15));

        /// <summary>
        /// Raised when a new <see cref="BrowserClient"/> connects to the server via WebSocket or HTTP polling.
        /// </summary>
        public event EventHandler<BrowserClient>? OnClientConnected;

        /// <summary>
        /// Raised when a <see cref="BrowserClient"/> disconnects or is removed due to inactivity.
        /// </summary>
        public event EventHandler<BrowserClient>? OnClientDisconnected;

        /// <summary>
        /// Raised when a connected <see cref="BrowserClient"/> updates its metadata (such as page title, URL, or IPC transport mode).
        /// </summary>
        public event EventHandler<BrowserClient>? OnClientMetadataUpdated;

        /// <summary>
        /// Raised when <see cref="ActiveClient"/> changes.
        /// </summary>
        public event EventHandler<BrowserClient?>? OnActiveClientChanged;

        /// <summary>
        /// Raised when any connected <see cref="BrowserClient"/> switches its IPC transport mode.
        /// </summary>
        public event EventHandler<BrowserClient>? OnClientModeChanged;

        /// <summary>
        /// Raised when any connected <see cref="BrowserClient"/> streams partial command result data.
        /// </summary>
        public event EventHandler<(BrowserClient Client, CommandResult Result)>? OnClientStreamDataReceived;

        /// <summary>
        /// Raised when any connected <see cref="BrowserClient"/> finishes loading a page.
        /// </summary>
        public event EventHandler<(BrowserClient Client, PageLoadedEventArgs Args)>? OnClientPageLoaded;

        /// <summary>
        /// Raised when the current <see cref="ActiveClient"/> finishes loading a page.
        /// </summary>
        public event EventHandler<PageLoadedEventArgs>? OnActiveClientPageLoaded;

        /// <summary>
        /// Raised when the current <see cref="ActiveClient"/> reports a DOM mutation.
        /// </summary>
        public event EventHandler<DomChangedEventArgs>? OnActiveClientDomChanged;

        /// <summary>
        /// Raised when the highlighted element on the current <see cref="ActiveClient"/> is removed from the DOM.
        /// </summary>
        public event EventHandler? OnActiveClientHighlightedElementRemoved;

        /// <summary>
        /// Raised when the user middle-clicks an element in the current <see cref="ActiveClient"/> browser tab.
        /// </summary>
        public event EventHandler<ElementClickedEventArgs>? OnActiveClientElementClicked;

        /// <summary>
        /// Raised when the BAF Chrome Extension background service worker connects to the server.
        /// </summary>
        public event EventHandler? OnExtensionConnected;

        /// <summary>
        /// Raised when the BAF Chrome Extension background service worker disconnects from the server.
        /// </summary>
        public event EventHandler? OnExtensionDisconnected;

        /// <summary>
        /// Raised whenever the BAF Chrome Extension reports an updated list of open browser tabs.
        /// </summary>
        public event EventHandler<ExtensionTabsEventArgs>? OnExtensionTabsUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationServer"/> class on the specified TCP port.
        /// </summary>
        /// <param name="port">The local TCP port to listen on (<c>http://127.0.0.1:{port}/</c>).</param>
        public AutomationServer(int port = 8081)
        {
            _port = port;
        }

        /// <summary>
        /// Exports/updates the unpacked Chrome Extension files in the default or specified directory to match this server's port and IPC mode.
        /// </summary>
        public string SyncDefaultChromeExtension(
            IpcMode mode = IpcMode.WebSocket,
            string? directoryPath = null,
            string? customScript = null)
        {
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                return ChromeExtensionGenerator.ExportToDirectory(directoryPath!, _port, mode, customScript);
            }
            return ChromeExtensionGenerator.SyncDefaultDirectory(_port, mode, customScript);
        }

        /// <summary>
        /// Starts listening for incoming HTTP and WebSocket connections from browser clients and the Chrome Extension until <see cref="Stop"/> is called or <paramref name="cancellationToken"/> is canceled.
        /// </summary>
        /// <param name="cancellationToken">An optional external token to stop the listener loop.</param>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _serverCts?.Cancel();
            _serverCts?.Dispose();
            _serverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = _serverCts.Token;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            _listener.Start();
            IsRunning = true;

            _cleanupTimer = new System.Threading.Timer(CleanupStaleClients, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            Console.WriteLine($"[AutomationServer] Listening on http://127.0.0.1:{_port}/");

            try
            {
                while (!linkedToken.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = Task.Run(() => HandleContextAsync(context, linkedToken), linkedToken);
                    }
                    catch (HttpListenerException) when (linkedToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException) when (linkedToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Stops the HTTP and WebSocket listener, cancels any active listener loop, and disposes the stale-client cleanup timer.
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            try { _serverCts?.Cancel(); } catch { }
            _cleanupTimer?.Dispose();
            _cleanupTimer = null;

            foreach (var kvp in _clients.ToArray())
            {
                if (_clients.TryRemove(kvp.Key, out var client))
                {
                    client.Disconnect();
                }
            }

            ActiveExtensionTabId = null;
            PendingAutoSelectTabId = null;
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
            ActiveClient = null;
        }

        /// <summary>
        /// Stops the server and releases all managed resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _serverCts?.Dispose();
            _extWsSendLock.Dispose();
            GC.SuppressFinalize(this);
        }

        private void CleanupStaleClients(object? state)
        {
            var now = DateTime.UtcNow;
            var httpThreshold = now.AddSeconds(-15);
            var wsThreshold = now.AddSeconds(-15);

            foreach (var kvp in _clients)
            {
                var client = kvp.Value;
                bool isStale = (client.Mode == IpcMode.HttpPolling && client.LastSeen < httpThreshold) ||
                               (client.Mode == IpcMode.WebSocket && !client.IsWebSocketConnected && client.LastSeen < wsThreshold);

                if (isStale)
                {
                    DisconnectClient(kvp.Key);
                }
            }

            if (_wasExtensionConnected && !IsExtensionConnected)
            {
                _wasExtensionConnected = false;
                OnExtensionDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Retrieves a connected <see cref="BrowserClient"/> by its unique <paramref name="instanceId"/>, or <c>null</c> if not found.
        /// </summary>
        public BrowserClient? GetClient(string instanceId)
        {
            _clients.TryGetValue(instanceId, out var client);
            return client;
        }

        /// <summary>
        /// Returns an enumerable of all currently connected <see cref="BrowserClient"/> instances.
        /// </summary>
        public IEnumerable<BrowserClient> GetAllClients() => _clients.Values;

        /// <summary>
        /// Computes which clients have been disconnected or newly added compared to a UI client list snapshot.
        /// </summary>
        public void GetClientListDelta(
            IEnumerable<BrowserClient> currentUiClients,
            out List<BrowserClient> disconnectedClients,
            out List<BrowserClient> addedClients)
        {
            var activeClients = _clients.Values.ToList();
            var uiList = currentUiClients?.ToList() ?? new List<BrowserClient>();

            disconnectedClients = uiList
                .Where(ui => !activeClients.Any(a => string.Equals(a.InstanceId, ui.InstanceId, StringComparison.Ordinal)))
                .ToList();

            addedClients = activeClients
                .Where(a => !uiList.Any(ui => string.Equals(ui.InstanceId, a.InstanceId, StringComparison.Ordinal)))
                .ToList();
        }

        /// <summary>
        /// Determines whether a newly connected or re-announced <see cref="BrowserClient"/> should become the active selected client in the UI.
        /// </summary>
        public bool ShouldAutoSelectClient(
            BrowserClient client,
            BrowserClient? currentSelectedClient,
            int totalClientCount,
            bool exclusiveTabMode)
        {
            if (client == null) return false;

            bool matchedPending = PendingAutoSelectTabId.HasValue && client.TabId == PendingAutoSelectTabId.Value;
            if (matchedPending)
            {
                PendingAutoSelectTabId = null;
                return true;
            }

            return (ActiveExtensionTabId.HasValue && client.TabId == ActiveExtensionTabId.Value) ||
                   (client.IsExtensionClient && exclusiveTabMode) ||
                   currentSelectedClient == null ||
                   totalClientCount <= 1 ||
                   string.Equals(currentSelectedClient.InstanceId, client.InstanceId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns all open browser tabs reported by the Chrome Extension, optionally filtered by a search string.
        /// </summary>
        public IReadOnlyList<BrowserTabInfo> GetExtensionTabs(string? filter = null)
        {
            lock (_extensionTabsLock)
            {
                if (string.IsNullOrWhiteSpace(filter))
                    return _extensionTabs.ToList();

                return _extensionTabs.Where(t => t.MatchesFilter(filter)).ToList();
            }
        }

        /// <summary>
        /// Returns all open browser tabs reported by the Chrome Extension, optionally filtered by a search string,
        /// and resolves the tab matching <paramref name="preferredSelectedTabId"/> for list selection preservation.
        /// </summary>
        public IReadOnlyList<BrowserTabInfo> GetExtensionTabs(
            string? filter,
            int? preferredSelectedTabId,
            out BrowserTabInfo? resolvedSelectedTab)
        {
            var tabs = GetExtensionTabs(filter);
            int? targetTabId = preferredSelectedTabId ?? ActiveExtensionTabId;
            resolvedSelectedTab = targetTabId.HasValue
                ? tabs.FirstOrDefault(t => t.TabId == targetTabId.Value)
                : null;
            return tabs;
        }

        /// <summary>
        /// Finds the connected <see cref="BrowserClient"/> corresponding to the specified Chrome Extension <see cref="BrowserTabInfo"/>, if connected.
        /// </summary>
        public BrowserClient? GetClientForTab(BrowserTabInfo tab)
        {
            if (tab == null) return null;
            if (_clients.TryGetValue(tab.InstanceId, out var byInstance))
                return byInstance;

            return _clients.Values.FirstOrDefault(c => c.TabId == tab.TabId);
        }

        /// <summary>
        /// Finds the connected <see cref="BrowserClient"/> corresponding to the specified browser tab ID, if connected.
        /// </summary>
        public BrowserClient? GetClientForTab(int tabId)
        {
            if (_clients.TryGetValue($"tab_{tabId}", out var byInstance))
                return byInstance;

            return _clients.Values.FirstOrDefault(c => c.TabId == tabId);
        }

        /// <summary>
        /// Disconnects and removes the <see cref="BrowserClient"/> with the specified <paramref name="instanceId"/>.
        /// Automatically updates <see cref="ActiveClient"/> if the disconnected client was active.
        /// </summary>
        public void DisconnectClient(string instanceId)
        {
            if (_clients.TryRemove(instanceId, out var removed))
            {
                removed.Disconnect();
                OnClientDisconnected?.Invoke(this, removed);

                if (ReferenceEquals(_activeClient, removed))
                {
                    ActiveClient = _clients.Values.FirstOrDefault();
                }
            }
        }

        private bool IsClientActive(BrowserClient client)
        {
            return _activeClient != null &&
                   string.Equals(_activeClient.InstanceId, client.InstanceId, StringComparison.Ordinal);
        }

        private void RegisterNewClientEventsAndSelection(BrowserClient client)
        {
            client.OnPageLoaded += (s, e) =>
            {
                OnClientMetadataUpdated?.Invoke(this, client);
                OnClientPageLoaded?.Invoke(this, (client, e));
                if (IsClientActive(client))
                {
                    OnActiveClientPageLoaded?.Invoke(client, e);
                }
            };

            client.OnModeChanged += (s, e) =>
            {
                OnClientMetadataUpdated?.Invoke(this, client);
                OnClientModeChanged?.Invoke(this, client);
            };

            client.OnStreamDataReceived += (s, res) =>
            {
                OnClientStreamDataReceived?.Invoke(this, (client, res));
            };

            client.OnDomChanged += (s, e) =>
            {
                if (IsClientActive(client))
                {
                    OnActiveClientDomChanged?.Invoke(client, e);
                }
            };

            client.OnHighlightedElementRemoved += (s, e) =>
            {
                if (IsClientActive(client))
                {
                    OnActiveClientHighlightedElementRemoved?.Invoke(client, EventArgs.Empty);
                }
            };

            client.OnElementClicked += (s, e) =>
            {
                if (IsClientActive(client))
                {
                    OnActiveClientElementClicked?.Invoke(client, e);
                }
            };

            OnClientConnected?.Invoke(this, client);

            if (ShouldAutoSelectClient(client, _activeClient, _clients.Count, ExclusiveTabMode))
            {
                ActiveClient = client;
            }
        }

        /// <summary>
        /// Returns a human-readable status message describing the current Chrome Extension connection state and open tab count.
        /// </summary>
        public string GetExtensionStatusMessage()
        {
            return GetExtensionStatusMessage(IsRunning, IsExtensionConnected, GetExtensionTabs().Count);
        }

        /// <summary>
        /// Returns a human-readable status message describing the Chrome Extension connection state and open tab count.
        /// </summary>
        public static string GetExtensionStatusMessage(bool isServerRunning, bool isExtensionConnected, int openTabCount)
        {
            if (isExtensionConnected)
            {
                return $"Extension: Connected ({openTabCount} open tab{(openTabCount == 1 ? "" : "s")})";
            }

            return isServerRunning
                ? "Extension: Waiting for connection..."
                : "Extension: Server stopped";
        }

        /// <summary>
        /// Finds the index of a <see cref="BrowserClient"/> with a matching <see cref="BrowserClient.InstanceId"/> in an <see cref="System.Collections.IList"/>, or returns -1.
        /// </summary>
        public static int FindClientIndex(System.Collections.IList items, BrowserClient? client)
        {
            if (items == null || client == null) return -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is BrowserClient item &&
                    string.Equals(item.InstanceId, client.InstanceId, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Inserts or updates <paramref name="client"/> inside <paramref name="items"/> by matching <see cref="BrowserClient.InstanceId"/>.
        /// </summary>
        public static int UpsertClientInList(System.Collections.IList items, BrowserClient client)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (client == null) throw new ArgumentNullException(nameof(client));

            int idx = FindClientIndex(items, client);
            if (idx >= 0)
            {
                items[idx] = client;
                return idx;
            }

            return items.Add(client);
        }

        /// <summary>
        /// Removes all entries matching <paramref name="client"/>'s <see cref="BrowserClient.InstanceId"/> from <paramref name="items"/>.
        /// </summary>
        public static bool RemoveClientFromList(System.Collections.IList items, BrowserClient? client)
        {
            if (items == null || client == null) return false;
            bool removedAny = false;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] is BrowserClient item &&
                    string.Equals(item.InstanceId, client.InstanceId, StringComparison.Ordinal))
                {
                    items.RemoveAt(i);
                    removedAny = true;
                }
            }
            return removedAny;
        }

        /// <summary>
        /// Refreshes the display entry for <paramref name="client"/> in <paramref name="items"/> if present, returning its index or -1.
        /// </summary>
        public static int RefreshClientInList(System.Collections.IList items, BrowserClient? client)
        {
            int idx = FindClientIndex(items, client);
            if (idx >= 0 && client != null)
            {
                items[idx] = client;
            }
            return idx;
        }

        /// <summary>
        /// Validates that <paramref name="tab"/> allows script injection, instructs the connected Chrome Extension
        /// to inject the automation bridge into the tab, and returns any already-connected <see cref="BrowserClient"/> for the tab.
        /// </summary>
        public async Task<BrowserClient?> SwitchToExtensionTabAsync(
            BrowserTabInfo tab,
            bool focusBrowserTab = false,
            bool detachOtherTabs = true,
            IpcMode mode = IpcMode.WebSocket,
            bool skipIfAlreadyActive = false,
            CancellationToken cancellationToken = default)
        {
            if (tab == null) throw new ArgumentNullException(nameof(tab));
            if (skipIfAlreadyActive && tab.IsAlreadyActiveClient(_activeClient, detachOtherTabs))
            {
                return _activeClient;
            }

            if (!tab.CanInject)
                throw new InvalidOperationException($"Tab #{tab.TabId} ('{tab.Url}') is a restricted internal browser page and cannot be injected.");

            var existingClient = GetClientForTab(tab);
            await SwitchToExtensionTabAsync(tab.TabId, focusBrowserTab, detachOtherTabs, mode, cancellationToken);
            if (existingClient != null)
            {
                ActiveClient = existingClient;
            }
            return existingClient;
        }

        /// <summary>
        /// Instructs the connected Chrome Extension to inject the automation bridge into <paramref name="tabId"/>
        /// and optionally detach any previously hooked tab so interaction switches cleanly to the selected tab.
        /// </summary>
        public async Task SwitchToExtensionTabAsync(
            int tabId,
            bool focusBrowserTab = false,
            bool detachOtherTabs = true,
            IpcMode mode = IpcMode.WebSocket,
            CancellationToken cancellationToken = default)
        {
            if (detachOtherTabs)
            {
                foreach (var kvp in _clients.ToArray())
                {
                    if (kvp.Value.IsExtensionClient && kvp.Value.TabId != tabId)
                    {
                        DisconnectClient(kvp.Key);
                    }
                }
            }

            PendingAutoSelectTabId = tabId;
            ActiveExtensionTabId = tabId;
            await SendExtensionCommandAsync(new
            {
                type = "ATTACH_TAB",
                payload = new
                {
                    tabId,
                    focus = focusBrowserTab,
                    exclusive = detachOtherTabs,
                    forceReinject = true,
                    port = _port,
                    mode = mode == IpcMode.WebSocket ? "ws" : "http"
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Returns all currently hooked/injected browser tabs known to the server.
        /// </summary>
        public IReadOnlyList<BrowserTabInfo> GetHookedExtensionTabs()
        {
            var result = new List<BrowserTabInfo>();
            var seenTabIds = new HashSet<int>();

            lock (_extensionTabsLock)
            {
                foreach (var tab in _extensionTabs)
                {
                    if (tab.IsInjected ||
                        (ActiveExtensionTabId.HasValue && tab.TabId == ActiveExtensionTabId.Value) ||
                        _clients.Values.Any(c => c.TabId == tab.TabId))
                    {
                        if (seenTabIds.Add(tab.TabId))
                        {
                            result.Add(tab);
                        }
                    }
                }
            }

            if (ActiveExtensionTabId.HasValue && seenTabIds.Add(ActiveExtensionTabId.Value))
            {
                result.Add(new BrowserTabInfo
                {
                    TabId = ActiveExtensionTabId.Value,
                    IsInjected = true,
                    InstanceId = $"tab_{ActiveExtensionTabId.Value}"
                });
            }

            foreach (var client in _clients.Values)
            {
                if (client.TabId.HasValue && seenTabIds.Add(client.TabId.Value))
                {
                    result.Add(new BrowserTabInfo
                    {
                        TabId = client.TabId.Value,
                        Title = client.PageTitle ?? string.Empty,
                        Url = client.CurrentUrl ?? string.Empty,
                        IsInjected = true,
                        InstanceId = client.InstanceId
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Detects all currently hooked Chrome Extension tabs and detaches the automation bridge from each of them.
        /// </summary>
        public async Task<int> DetachAllExtensionTabsAsync(CancellationToken cancellationToken = default)
        {
            var hookedTabs = GetHookedExtensionTabs();
            foreach (var tab in hookedTabs)
            {
                await DetachExtensionTabAsync(tab.TabId, cancellationToken);
            }
            return hookedTabs.Count;
        }

        /// <summary>
        /// Instructs the Chrome Extension to dispose and unhook the automation bridge in <paramref name="tabId"/>.
        /// </summary>
        public async Task DetachExtensionTabAsync(int tabId, CancellationToken cancellationToken = default)
        {
            DisconnectClient($"tab_{tabId}");
            if (ActiveExtensionTabId == tabId)
            {
                ActiveExtensionTabId = null;
            }

            await SendExtensionCommandAsync(new
            {
                type = "DETACH_TAB",
                payload = new { tabId }
            }, cancellationToken);
        }

        /// <summary>
        /// Brings the specified browser tab and its window to the foreground in Chrome.
        /// </summary>
        public Task FocusExtensionTabAsync(int tabId, CancellationToken cancellationToken = default)
        {
            return SendExtensionCommandAsync(new
            {
                type = "FOCUS_TAB",
                payload = new { tabId }
            }, cancellationToken);
        }

        /// <summary>
        /// Requests an immediate refresh of the open tabs list from the Chrome Extension.
        /// </summary>
        public Task RequestExtensionTabsRefreshAsync(CancellationToken cancellationToken = default)
        {
            return SendExtensionCommandAsync(new
            {
                type = "REFRESH_TABS"
            }, cancellationToken);
        }

        private async Task SendExtensionCommandAsync(object command, CancellationToken cancellationToken = default)
        {
            var ws = _extensionWebSocket;
            if (ws != null && ws.State == WebSocketState.Open)
            {
                var json = JsonSerializer.Serialize(command);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _extWsSendLock.WaitAsync(cancellationToken);
                try
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
                        return;
                    }
                }
                catch
                {
                    // Fallback to queue if WebSocket send fails
                }
                finally
                {
                    _extWsSendLock.Release();
                }
            }

            _pendingExtensionCommands.Enqueue(command);
        }

        private void MarkExtensionSeen()
        {
            _lastExtensionSeen = DateTime.UtcNow;
            if (!_wasExtensionConnected)
            {
                _wasExtensionConnected = true;
                OnExtensionConnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateExtensionTabsFromPayload(JsonElement payload)
        {
            MarkExtensionSeen();

            int? activeTabId = null;
            if (payload.ValueKind == JsonValueKind.Object &&
                payload.TryGetProperty("activeAttachedTabId", out var activeProp) &&
                activeProp.ValueKind == JsonValueKind.Number &&
                activeProp.TryGetInt32(out int parsedActive))
            {
                activeTabId = parsedActive;
            }

            JsonElement tabsElement = payload;
            if (payload.ValueKind == JsonValueKind.Object &&
                payload.TryGetProperty("tabs", out var tabsProp) &&
                tabsProp.ValueKind == JsonValueKind.Array)
            {
                tabsElement = tabsProp;
            }

            var parsedTabs = new List<BrowserTabInfo>();
            if (tabsElement.ValueKind == JsonValueKind.Array)
            {
                parsedTabs = JsonSerializer.Deserialize<List<BrowserTabInfo>>(tabsElement.GetRawText(), HighDepthSerializerOptions) ?? new List<BrowserTabInfo>();
            }

            lock (_extensionTabsLock)
            {
                _extensionTabs = parsedTabs;
            }

            ActiveExtensionTabId = activeTabId;

            // Synchronize metadata with active extension clients and disconnect closed/detached tabs
            var openInjectedTabIds = new HashSet<int>(parsedTabs.Where(t => t.IsInjected).Select(t => t.TabId));
            foreach (var tab in parsedTabs)
            {
                if (_clients.TryGetValue(tab.InstanceId, out var existingClient))
                {
                    bool changed = existingClient.TabId != tab.TabId;
                    existingClient.TabId = tab.TabId;
                    if (!string.IsNullOrWhiteSpace(tab.Title) && !string.Equals(existingClient.PageTitle, tab.Title, StringComparison.Ordinal))
                    {
                        existingClient.PageTitle = tab.Title;
                        changed = true;
                    }
                    if (!string.IsNullOrWhiteSpace(tab.Url) && !string.Equals(existingClient.CurrentUrl, tab.Url, StringComparison.Ordinal))
                    {
                        existingClient.CurrentUrl = tab.Url;
                        changed = true;
                    }
                    if (changed)
                    {
                        OnClientMetadataUpdated?.Invoke(this, existingClient);
                    }
                }
            }

            var allOpenTabIds = new HashSet<int>(parsedTabs.Select(t => t.TabId));
            foreach (var kvp in _clients.ToArray())
            {
                if (kvp.Value.IsExtensionClient && kvp.Value.TabId.HasValue)
                {
                    int cTabId = kvp.Value.TabId.Value;
                    if (!allOpenTabIds.Contains(cTabId))
                    {
                        DisconnectClient(kvp.Key);
                    }
                    else if (!openInjectedTabIds.Contains(cTabId) &&
                             ActiveExtensionTabId != cTabId &&
                             !kvp.Value.IsWebSocketConnected &&
                             (DateTime.UtcNow - kvp.Value.LastSeen) > TimeSpan.FromSeconds(5))
                    {
                        DisconnectClient(kvp.Key);
                    }
                }
            }

            OnExtensionTabsUpdated?.Invoke(this, new ExtensionTabsEventArgs(parsedTabs, activeTabId));
        }

        private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            var response = context.Response;

            // Handle CORS for Tampermonkey cross-origin requests
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            if (request.IsWebSocketRequest)
            {
                await HandleWebSocketAsync(context, cancellationToken);
                return;
            }

            string path = request.Url?.AbsolutePath ?? "";

            try
            {
                if (path.StartsWith("/api/command/"))
                {
                    string instanceId = path.Substring("/api/command/".Length);
                    var client = GetOrAddClient(instanceId, IpcMode.HttpPolling);
                    client.TouchHttpPolling();

                    if (client.TryGetNextHttpCommand(out var cmd))
                    {
                        await SendJsonAsync(response, new { command = cmd });
                    }
                    else
                    {
                        await SendJsonAsync(response, new { status = "idle" });
                    }
                }
                else if (path.StartsWith("/api/result/"))
                {
                    string instanceId = path.Substring("/api/result/".Length);
                    var client = GetOrAddClient(instanceId, IpcMode.HttpPolling);
                    client.TouchHttpPolling();

                    using var reader = new StreamReader(request.InputStream);
                    string body = await reader.ReadToEndAsync(cancellationToken);
                    var result = JsonSerializer.Deserialize<CommandResult>(body, HighDepthSerializerOptions);

                    if (result != null)
                    {
                        client.HandleResult(result);
                    }

                    await SendJsonAsync(response, new { status = "acknowledged" });
                }
                else if (path.StartsWith("/api/event/"))
                {
                    string instanceId = path.Substring("/api/event/".Length);
                    var client = GetOrAddClient(instanceId, IpcMode.HttpPolling);
                    client.TouchHttpPolling();

                    using var reader = new StreamReader(request.InputStream);
                    string body = await reader.ReadToEndAsync(cancellationToken);
                    var evt = JsonSerializer.Deserialize<EventMessage>(body, HighDepthSerializerOptions);

                    if (evt != null)
                    {
                        client.HandleEvent(evt);
                    }

                    await SendJsonAsync(response, new { status = "acknowledged" });
                }
                else if (path.Equals("/api/ext/status", StringComparison.OrdinalIgnoreCase))
                {
                    MarkExtensionSeen();
                    await SendJsonAsync(response, new
                    {
                        status = "online",
                        port = _port,
                        activeExtensionTabId = ActiveExtensionTabId
                    });
                }
                else if (path.Equals("/api/ext/tabs", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(request.InputStream);
                    string body = await reader.ReadToEndAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        using var doc = JsonDocument.Parse(body, HighDepthDocumentOptions);
                        UpdateExtensionTabsFromPayload(doc.RootElement);
                    }

                    var pending = new List<object>();
                    while (_pendingExtensionCommands.TryDequeue(out var cmd))
                    {
                        pending.Add(cmd);
                    }

                    await SendJsonAsync(response, new { status = "ok", commands = pending });
                }
                else if (path.Equals("/api/ext/select-tab", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(request.InputStream);
                    string body = await reader.ReadToEndAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("tabId", out var tabProp) && tabProp.TryGetInt32(out int tabId))
                        {
                            bool exclusive = !doc.RootElement.TryGetProperty("exclusive", out var exProp) || exProp.ValueKind != JsonValueKind.False;
                            await SwitchToExtensionTabAsync(tabId, focusBrowserTab: false, detachOtherTabs: exclusive, cancellationToken: cancellationToken);
                        }
                    }
                    await SendJsonAsync(response, new { status = "ok" });
                }
                else if (path.Equals("/api/ext/detach-tab", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(request.InputStream);
                    string body = await reader.ReadToEndAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("tabId", out var tabProp) && tabProp.TryGetInt32(out int tabId))
                        {
                            await DetachExtensionTabAsync(tabId, cancellationToken);
                        }
                    }
                    await SendJsonAsync(response, new { status = "ok" });
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutomationServer] Error handling HTTP request: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        private async Task HandleExtensionWebSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            _extensionWebSocket = webSocket;
            MarkExtensionSeen();

            while (_pendingExtensionCommands.TryDequeue(out var queuedCmd))
            {
                await SendExtensionCommandAsync(queuedCmd, cancellationToken);
            }

            var buffer = new byte[1024 * 64];
            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
                            break;
                        }
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    MarkExtensionSeen();
                    string message = Encoding.UTF8.GetString(ms.ToArray());
                    using var doc = JsonDocument.Parse(message, HighDepthDocumentOptions);
                    if (doc.RootElement.TryGetProperty("event", out var evtProp) &&
                        string.Equals(evtProp.GetString(), "EXTENSION_TABS_UPDATED", StringComparison.OrdinalIgnoreCase) &&
                        doc.RootElement.TryGetProperty("payload", out var payloadProp))
                    {
                        UpdateExtensionTabsFromPayload(payloadProp);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutomationServer] Extension WebSocket error: {ex.Message}");
            }
            finally
            {
                if (ReferenceEquals(_extensionWebSocket, webSocket))
                {
                    _extensionWebSocket = null;
                }
                if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                {
                    webSocket.Dispose();
                }
                if (_wasExtensionConnected)
                {
                    _wasExtensionConnected = false;
                    OnExtensionDisconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private async Task HandleWebSocketAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var wsContext = await context.AcceptWebSocketAsync(null);
            var webSocket = wsContext.WebSocket;

            string? role = context.Request.QueryString["role"];
            if (string.Equals(role, "extension", StringComparison.OrdinalIgnoreCase))
            {
                await HandleExtensionWebSocketAsync(webSocket, cancellationToken);
                return;
            }
            
            string instanceId = context.Request.QueryString["instanceId"] ?? Guid.NewGuid().ToString();
            bool isNewClient = false;
            var client = _clients.GetOrAdd(instanceId, id =>
            {
                isNewClient = true;
                return new BrowserClient(id, IpcMode.WebSocket, webSocket);
            });

            if (isNewClient)
            {
                RegisterNewClientEventsAndSelection(client);
            }
            else
            {
                await client.AttachWebSocketAsync(webSocket, cancellationToken);
                if (ShouldAutoSelectClient(client, _activeClient, _clients.Count, ExclusiveTabMode))
                {
                    if (ReferenceEquals(_activeClient, client))
                    {
                        OnActiveClientChanged?.Invoke(this, _activeClient);
                    }
                    else
                    {
                        ActiveClient = client;
                    }
                }
            }

            var buffer = new byte[1024 * 64];

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
                            break;
                        }
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    client.LastSeen = DateTime.UtcNow;
                    string message = Encoding.UTF8.GetString(ms.ToArray());
                    using var doc = JsonDocument.Parse(message, HighDepthDocumentOptions);
                    if (doc.RootElement.TryGetProperty("event", out _))
                    {
                        var evt = JsonSerializer.Deserialize<EventMessage>(message, HighDepthSerializerOptions);
                        if (evt != null)
                        {
                            client.HandleEvent(evt);
                        }
                    }
                    else
                    {
                        var cmdResult = JsonSerializer.Deserialize<CommandResult>(message, HighDepthSerializerOptions);
                        if (cmdResult != null)
                        {
                            client.HandleResult(cmdResult);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutomationServer] WebSocket error: {ex.Message}");
            }
            finally
            {
                client.DetachWebSocket(webSocket);
                if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                {
                    webSocket.Dispose();
                }
            }
        }

        private BrowserClient GetOrAddClient(string instanceId, IpcMode mode)
        {
            bool isNew = false;
            var client = _clients.GetOrAdd(instanceId, id =>
            {
                isNew = true;
                return new BrowserClient(id, mode);
            });

            if (isNew)
            {
                RegisterNewClientEventsAndSelection(client);
            }

            return client;
        }

        private async Task SendJsonAsync(HttpListenerResponse response, object data)
        {
            response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(data);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
    }
}