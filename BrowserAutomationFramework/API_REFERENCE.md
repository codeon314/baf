# Browser Automation Framework (BAF) — Complete API Reference

**Target Framework:** `.NET 10.0` (`net10.0`)  
**Core Library Project:** `BrowserAutomationFramework/BrowserAutomationFramework.csproj` (`BrowserAutomationFramework.dll`)  
**Studio IDE Project:** `BAF Studio/BAF Studio.csproj` (`BAF Studio.exe`, Root Namespace: `BAFStudio`)  
**Root Namespace:** `BrowserAutomationFramework`

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Quick-Start Guides](#2-quick-start-guides)
   - [2.1 Starting the Server & Interacting with a Browser Client](#21-starting-the-server--interacting-with-a-browser-client)
   - [2.2 Fluent Data Scraping with Pagination & Rate-Limit Recovery](#22-fluent-data-scraping-with-pagination--rate-limit-recovery)
   - [2.3 Running Multi-Step Automation Workflows & Custom Actions](#23-running-multi-step-automation-workflows--custom-actions)
   - [2.4 Reverse-Engineering & Dumping Site Assets](#24-reverse-engineering--dumping-site-assets)
   - [2.5 Using the Unified `AutomationStudioSession` Facade](#25-using-the-unified-automationstudiosession-facade)
3. [Namespace: `BrowserAutomationFramework.Server`](#3-namespace-browserautomationframeworkserver)
   - [3.1 `AutomationServer`](#31-automationserver)
   - [3.2 `BrowserClient`](#32-browserclient)
4. [Namespace: `BrowserAutomationFramework.Extensions`](#4-namespace-browserautomationframeworkextensions)
   - [4.1 `ReactExtensions`](#41-reactextensions)
5. [Namespace: `BrowserAutomationFramework.Automation`](#5-namespace-browserautomationframeworkautomation)
   - [5.1 `AutomationRunner`](#51-automationrunner)
   - [5.2 `AutomationStudioSession`](#52-automationstudiosession)
6. [Namespace: `BrowserAutomationFramework.Scraping`](#6-namespace-browserautomationframeworkscraping)
   - [6.1 `DataScraper`](#61-datascraper)
   - [6.2 `ScrapeBuilder`](#62-scrapebuilder)
   - [6.3 `SelectorHelper`](#63-selectorhelper)
7. [Namespace: `BrowserAutomationFramework.ReverseEngineering`](#7-namespace-browserautomationframeworkreverseengineering)
   - [7.1 `SiteDumper`](#71-sitedumper)
8. [Namespace: `BrowserAutomationFramework.Generators`](#8-namespace-browserautomationframeworkgenerators)
   - [8.1 `ScriptGenerator`](#81-scriptgenerator)
   - [8.2 `ChromeExtensionGenerator`](#82-chromeextensiongenerator)
9. [Namespace: `BrowserAutomationFramework.Models`](#9-namespace-browserautomationframeworkmodels)
   - [9.1 Project & Workflow Models (`AutomationProject`, `AutomationSequence`, `AutomationStep`, `AutomationActionType`)](#91-project--workflow-models)
   - [9.2 Data Scraping Models (`ScrapeDefinition`, `ScrapeField`, `ScrapeExtractionType`, `ScrapeResult`)](#92-data-scraping-models)
   - [9.3 Reverse-Engineering Models (`SiteDumpOptions`, `SiteDumpResult`, `SiteResourceItem`, `SiteResourceType`, `SiteDumpType`, `SiteDumpViewMode`)](#93-reverse-engineering-models)
   - [9.4 DOM & Virtualized Tree Models (`DomNode`, `DomTreeSnapshot`, `DomTreeItem`, `DomTreeViewportState`, etc.)](#94-dom--virtualized-tree-models)
   - [9.5 IPC, Tab & Event Models (`IpcMode`, `ScriptOptions`, `BrowserTabInfo`, `CommandMessage`, `CommandResult`, `EventMessage`, `*EventArgs`)](#95-ipc-tab--event-models)
   - [9.6 Studio Theme & Layout Metrics (`StudioTheme`, `StudioLayoutMetrics`)](#96-studio-theme--layout-metrics)
10. [Namespace: `BrowserAutomationFramework.Utils`](#10-namespace-browserautomationframeworkutils)
    - [10.1 `FileAssociationHelper`](#101-fileassociationhelper)
11. [Browser-Side JavaScript Bridge Protocol](#11-browser-side-javascript-bridge-protocol)

---

## 1. Architecture Overview

`BrowserAutomationFramework` is a self-contained, UI-agnostic browser automation, web scraping, and site reverse-engineering library. It bridges C# applications with live Chromium/browser tabs via either:
- **Unpacked Manifest V3 Chrome Extension** (`ChromeExtensionGenerator`): Reports all open browser tabs to the C# server and supports on-demand one-click script injection, exclusive tab switching, tab focusing, and `GM_xmlhttpRequest` cross-origin relaying.
- **Tampermonkey Userscript** (`ScriptGenerator`): Connects matching pages automatically using full-duplex WebSockets with transparent fallback to CSP-safe `GM_xmlhttpRequest` HTTP polling when page Content-Security-Policy rules block local WebSockets.

| Layer | Key Types | Purpose |
| :--- | :--- | :--- |
| **Transport & Server** | `AutomationServer`, `BrowserClient`, `IpcMode` | Local HTTP/WebSocket IPC server, tab injection control, bidirectional command/event dispatch. |
| **DOM & React Extensions** | `ReactExtensions`, `DomNode`, `DomTreeSnapshot` | React-compatible clicking/input setting, SPA/full-page navigation, O(1) indexed DOM snapshots. |
| **Automation Engine** | `AutomationRunner`, `AutomationProject`, `AutomationSequence`, `AutomationStep` | Modular workflow execution with custom step/command registration and JSON project persistence. |
| **Data Scraping Engine** | `DataScraper`, `ScrapeBuilder`, `ScrapeDefinition`, `ScrapeField`, `ScrapeResult`, `SelectorHelper` | Container/document/table extraction, multi-page pagination, HTTP 429 rate-limit recovery, CSV/JSON/POCO binding. |
| **Reverse Engineering** | `SiteDumper`, `SiteDumpOptions`, `SiteDumpResult`, `SiteResourceItem` | Extraction of document/iframe HTML, external/inline/constructed CSS, and external/inline/dynamic JS. |
| **Generators** | `ScriptGenerator`, `ChromeExtensionGenerator` | Programmatic generation of Tampermonkey userscripts and Manifest V3 Chrome Extensions. |
| **High-Level Facade** | `AutomationStudioSession` | Coordinates `AutomationServer`, `AutomationProject`, `AutomationRunner`, `DataScraper`, and `SiteDumper` with unified logging for **BAF Studio** (`BAF Studio/BAF Studio.csproj`) and custom hosts. |

---

## 2. Quick-Start Guides

### 2.1 Starting the Server & Interacting with a Browser Client

~~~csharp
using BrowserAutomationFramework.Extensions;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

using var server = new AutomationServer(port: 9999);

// Optional: Export/sync the unpacked Chrome Extension folder
server.SyncDefaultChromeExtension(IpcMode.WebSocket);

server.OnClientConnected += async (sender, client) =>
{
    Console.WriteLine($"Connected: {client.DisplayName}");

    // Navigate and wait for transition to complete
    await client.NavigateToUrlAsync("https://example.com/login");

    // Set React-controlled or native input values
    await client.SetReactInputValueAsync("#username", "admin", overwrite: true);
    await client.SetReactInputValueAsync("#password", "secret123", overwrite: true);

    // Simulate a trusted React/native click
    await client.ClickReactElementAsync("button[type='submit']");

    // Evaluate custom JavaScript in the page context
    var evalResult = await client.EvaluateJavaScriptAsync("return document.title;");
    Console.WriteLine($"Page Title: {evalResult.Output}");
};

await server.StartAsync();
~~~

### 2.2 Fluent Data Scraping with Pagination & Rate-Limit Recovery

~~~csharp
using BrowserAutomationFramework.Extensions;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

public class ProductDto
{
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public string Url { get; set; } = "";
}

public async Task ScrapeCatalogAsync(BrowserClient client)
{
    ScrapeResult result = await client.CreateScrape("CatalogScrape")
        .WithContainer("div.product-card")
        .Field("Title", "h2.product-title", ScrapeExtractionType.InnerText)
        .Field("Price", "span.price", ScrapeExtractionType.InnerText, regexPattern: @"([0-9]+\.[0-9]{2})")
        .Field("Url", "a.product-link", ScrapeExtractionType.Href)
        .WithPagination(nextPageSelector: "a.next-page", maxPages: 5, pageDelayMs: 1200, deduplicateRows: true)
        .ExecuteAsync();

    // Bind directly to strongly-typed POCOs
    List<ProductDto> products = result.BindTo<ProductDto>();

    // Or export to disk
    await result.SaveToJsonFileAsync("products.json", recordsOnly: true);
    await result.SaveToCsvFileAsync("products.csv");
}
~~~

### 2.3 Running Multi-Step Automation Workflows & Custom Actions

~~~csharp
using BrowserAutomationFramework.Automation;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

public async Task RunCustomWorkflowAsync(BrowserClient client)
{
    var runner = new AutomationRunner();

    // Register a custom command handler in C#
    runner.RegisterCustomActionHandler("LOG_TIMESTAMP", async (c, step, ct) =>
    {
        Console.WriteLine($"[Custom Step] {step.Value} at {DateTime.UtcNow:O}");
        return new CommandResult { Status = "completed", Output = "Logged" };
    });

    var sequence = new AutomationSequence { Name = "Checkout Flow" };
    sequence.AddStep("Go to Cart", AutomationActionType.Navigate, value: "https://example.com/cart");
    sequence.AddStep("Apply Promo", AutomationActionType.SetValue, selector: "#promo", value: "SAVE20");
    sequence.AddStep("Click Apply", AutomationActionType.Click, selector: "#apply-btn");
    sequence.Steps.Add(new AutomationStep
    {
        Name = "Audit Log",
        ActionType = AutomationActionType.Custom,
        CustomCommandType = "LOG_TIMESTAMP",
        Value = "Promo applied"
    });

    await runner.RunSequenceAsync(client, sequence);
}
~~~

### 2.4 Reverse-Engineering & Dumping Site Assets

~~~csharp
using BrowserAutomationFramework.Extensions;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

public async Task DumpTargetSiteAsync(BrowserClient client)
{
    var options = SiteDumpOptions.Create(
        includeExternal: true,
        includeInline: true,
        includeDynamicScripts: true,
        includeIframes: true);

    SiteDumpResult dump = await client.DumpAllAsync(options);
    Console.WriteLine(dump.GetStatusSummary());

    // Save index.html, css/*, js/*, combined.css, combined.js, and manifest.json
    await dump.SaveToDirectoryAsync(@"C:\Dumps\TargetSite");
}
~~~

### 2.5 Using the Unified `AutomationStudioSession` Facade

~~~csharp
using BrowserAutomationFramework.Automation;
using BrowserAutomationFramework.Models;

using var session = new AutomationStudioSession();
session.OnLog += (s, e) => Console.WriteLine(e.FormattedMessage);

// Load an existing project JSON file and start the server
session.LoadProjectFromFile("my-project.json");
_ = session.StartServerAsync(AutomationStudioSession.DefaultPort, IpcMode.WebSocket);
~~~

---

## 3. Namespace: `BrowserAutomationFramework.Server`

### 3.1 `AutomationServer`

Local HTTP and WebSocket IPC server that manages connections to browser clients (via the BAF Chrome Extension or Tampermonkey userscripts). Implements `IDisposable`.

#### Constructors

| Signature | Description |
| :--- | :--- |
| `AutomationServer(int port = 8081)` | Initializes a new server listening on `http://127.0.0.1:{port}/`. |

#### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Port` | `int` | Gets the TCP port on which the server listens. |
| `IsRunning` | `bool` | Gets whether the server is currently listening for connections. |
| `ExclusiveTabMode` | `bool` | Gets or sets whether switching to a Chrome Extension tab automatically detaches other hooked tabs and selects the new tab as `ActiveClient`. Default: `true`. |
| `ActiveClient` | `BrowserClient?` | Gets or sets the currently active/selected `BrowserClient`. Raising `OnActiveClientChanged` when modified. |
| `ActiveExtensionTabId` | `int?` | Gets the browser tab ID currently attached by the Chrome Extension, if any. |
| `PendingAutoSelectTabId` | `int?` | Gets or sets the tab ID most recently requested for an interactive switch, used to auto-select the client upon connection. |
| `IsExtensionConnected` | `bool` | Gets whether the BAF Chrome Extension is currently connected to the server. |

#### Events

| Event | Delegate Type | Description |
| :--- | :--- | :--- |
| `OnClientConnected` | `EventHandler<BrowserClient>?` | Raised when a new `BrowserClient` connects via WebSocket or HTTP polling. |
| `OnClientDisconnected` | `EventHandler<BrowserClient>?` | Raised when a `BrowserClient` disconnects or is removed due to inactivity. |
| `OnClientMetadataUpdated` | `EventHandler<BrowserClient>?` | Raised when a connected client updates its page title, URL, or IPC transport mode. |
| `OnActiveClientChanged` | `EventHandler<BrowserClient?>?` | Raised when `ActiveClient` changes. |
| `OnClientModeChanged` | `EventHandler<BrowserClient>?` | Raised when any connected client switches its IPC transport mode. |
| `OnClientStreamDataReceived` | `EventHandler<(BrowserClient Client, CommandResult Result)>?` | Raised when any connected client streams partial command output (`status == "streaming"`). |
| `OnClientPageLoaded` | `EventHandler<(BrowserClient Client, PageLoadedEventArgs Args)>?` | Raised when any connected client finishes loading a page. |
| `OnActiveClientPageLoaded` | `EventHandler<PageLoadedEventArgs>?` | Raised when the current `ActiveClient` finishes loading a page. |
| `OnActiveClientDomChanged` | `EventHandler<DomChangedEventArgs>?` | Raised when the current `ActiveClient` reports a DOM mutation. |
| `OnActiveClientHighlightedElementRemoved` | `EventHandler?` | Raised when the highlighted element on `ActiveClient` is removed from the DOM. |
| `OnActiveClientElementClicked` | `EventHandler<ElementClickedEventArgs>?` | Raised when the user middle-clicks an element in the `ActiveClient` browser tab. |
| `OnExtensionConnected` | `EventHandler?` | Raised when the Chrome Extension background service worker connects. |
| `OnExtensionDisconnected` | `EventHandler?` | Raised when the Chrome Extension background service worker disconnects. |
| `OnExtensionTabsUpdated` | `EventHandler<ExtensionTabsEventArgs>?` | Raised whenever the Chrome Extension reports an updated list of open browser tabs. |

#### Methods

| Signature | Description |
| :--- | :--- |
| `Task StartAsync(CancellationToken cancellationToken = default)` | Starts listening for HTTP and WebSocket connections until `Stop()` is called or `cancellationToken` is canceled. |
| `void Stop()` | Stops the listener, cancels active loops, disposes the cleanup timer, and resets `ActiveClient`. |
| `void Dispose()` | Stops the server and releases all managed resources. |
| `string SyncDefaultChromeExtension(IpcMode mode = IpcMode.WebSocket, string? directoryPath = null, string? customScript = null)` | Exports/updates the unpacked Chrome Extension files in the default or specified directory to match this server's port and IPC mode. |
| `BrowserClient? GetClient(string instanceId)` | Retrieves a connected client by its unique `instanceId`, or `null` if not found. |
| `IEnumerable<BrowserClient> GetAllClients()` | Returns all currently connected `BrowserClient` instances. |
| `void GetClientListDelta(IEnumerable<BrowserClient> currentUiClients, out List<BrowserClient> disconnectedClients, out List<BrowserClient> addedClients)` | Computes which clients have disconnected or been added compared to a UI client list snapshot. |
| `bool ShouldAutoSelectClient(BrowserClient client, BrowserClient? currentSelectedClient, int totalClientCount, bool exclusiveTabMode)` | Determines whether a newly connected or re-announced client should become the active selected client. |
| `IReadOnlyList<BrowserTabInfo> GetExtensionTabs(string? filter = null)` | Returns all open browser tabs reported by the Chrome Extension, optionally filtered by a search string. |
| `IReadOnlyList<BrowserTabInfo> GetExtensionTabs(string? filter, int? preferredSelectedTabId, out BrowserTabInfo? resolvedSelectedTab)` | Returns filtered extension tabs and resolves the tab matching `preferredSelectedTabId` (or `ActiveExtensionTabId`). |
| `BrowserClient? GetClientForTab(BrowserTabInfo tab)` | Finds the connected `BrowserClient` corresponding to `tab`, if connected. |
| `BrowserClient? GetClientForTab(int tabId)` | Finds the connected `BrowserClient` corresponding to `tabId`, if connected. |
| `void DisconnectClient(string instanceId)` | Disconnects and removes the client with `instanceId`, updating `ActiveClient` if necessary. |
| `string GetExtensionStatusMessage()` | Returns a human-readable status message describing the Chrome Extension connection state and open tab count. |
| `static string GetExtensionStatusMessage(bool isServerRunning, bool isExtensionConnected, int openTabCount)` | Static formatter for Chrome Extension status messages. |
| `Task<BrowserClient?> SwitchToExtensionTabAsync(BrowserTabInfo tab, bool focusBrowserTab = false, bool detachOtherTabs = true, IpcMode mode = IpcMode.WebSocket, bool skipIfAlreadyActive = false, CancellationToken cancellationToken = default)` | Validates that `tab` is injectable, instructs the Chrome Extension to inject the automation bridge, and returns any already-connected `BrowserClient`. |
| `Task SwitchToExtensionTabAsync(int tabId, bool focusBrowserTab = false, bool detachOtherTabs = true, IpcMode mode = IpcMode.WebSocket, CancellationToken cancellationToken = default)` | Instructs the Chrome Extension to inject the automation bridge into `tabId` and optionally detach other tabs. |
| `IReadOnlyList<BrowserTabInfo> GetHookedExtensionTabs()` | Returns all currently hooked/injected browser tabs known to the server. |
| `Task<int> DetachAllExtensionTabsAsync(CancellationToken cancellationToken = default)` | Detects all currently hooked Chrome Extension tabs and detaches the automation bridge from each of them. |
| `Task DetachExtensionTabAsync(int tabId, CancellationToken cancellationToken = default)` | Disposes and unhooks the automation bridge in `tabId`. |
| `Task FocusExtensionTabAsync(int tabId, CancellationToken cancellationToken = default)` | Brings `tabId` and its browser window to the foreground. |
| `Task RequestExtensionTabsRefreshAsync(CancellationToken cancellationToken = default)` | Requests an immediate refresh of the open tabs list from the Chrome Extension. |
| `static int FindClientIndex(IList items, BrowserClient? client)` | Finds the index of `client` in `items` by `InstanceId`, or returns `-1`. |
| `static int UpsertClientInList(IList items, BrowserClient client)` | Inserts or updates `client` inside `items` by matching `InstanceId`. |
| `static bool RemoveClientFromList(IList items, BrowserClient? client)` | Removes all entries matching `client.InstanceId` from `items`. |
| `static int RefreshClientInList(IList items, BrowserClient? client)` | Refreshes the display entry for `client` in `items` if present. |

---

### 3.2 `BrowserClient`

Represents a connected browser tab instance and manages bidirectional command execution, coalesced DOM snapshot refreshing, and navigation/DOM event streaming.

#### Constructors

| Signature | Description |
| :--- | :--- |
| `BrowserClient(string instanceId, IpcMode mode, WebSocket? webSocket = null)` | Initializes a new client instance with the specified ID, transport mode, and optional WebSocket. |

#### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `InstanceId` | `string` | Unique identifier for this browser client session (e.g., `tab_123` or `bot_xyz`). |
| `Mode` | `IpcMode` | Active IPC transport mode (`WebSocket` or `HttpPolling`). |
| `LastSeen` | `DateTime` | UTC timestamp when the server last received a message or poll from this client. |
| `CurrentUrl` | `string?` | Current URL loaded in the browser tab, if known. |
| `PageTitle` | `string?` | Current document title of the browser tab, if known. |
| `CurrentPageLoadId` | `string?` | Unique correlation ID generated by the browser bridge for the current page load. |
| `LastStatusCode` | `int` | Most recent HTTP status code reported by the browser tab (e.g., `200` or `429`). |
| `LastRetryAfter` | `string?` | Most recent `Retry-After` header value reported when `LastStatusCode == 429`. |
| `TabId` | `int?` | Chrome tab ID if this client was injected by the BAF Chrome Extension. |
| `IsExtensionClient` | `bool` | Gets whether this client was injected by the Chrome Extension rather than Tampermonkey. |
| `DisplayName` | `string` | Formatted display string combining `[Ext]`/`[TM]`, `InstanceId`, and page title or host. |
| `CurrentDomSnapshot` | `DomTreeSnapshot?` | Most recently fetched `DomTreeSnapshot` for this client, if any. |
| `CurrentDomRoot` | `DomNode?` | Root `DomNode` of the most recently fetched DOM tree for this client, if any. |
| `LastHighlightedSelector` | `string?` | CSS selector of the most recently highlighted element on this client. |

#### Events

| Event | Delegate Type | Description |
| :--- | :--- | :--- |
| `OnStreamDataReceived` | `EventHandler<CommandResult>?` | Raised when the browser script streams partial data (`window.streamData`). |
| `OnEventReceived` | `EventHandler<EventMessage>?` | Raised when any raw event message arrives from the browser script. |
| `OnDomChanged` | `EventHandler<DomChangedEventArgs>?` | Raised when the page DOM mutates. |
| `OnHighlightedElementRemoved` | `EventHandler?` | Raised when the currently highlighted element is removed from the DOM. |
| `OnElementClicked` | `EventHandler<ElementClickedEventArgs>?` | Raised when an element is selected in the browser via middle-click. |
| `OnPageLoaded` | `EventHandler<PageLoadedEventArgs>?` | Raised when a page load or in-place navigation completes. |
| `OnModeChanged` | `EventHandler<IpcMode>?` | Raised when the client switches IPC transport modes (e.g., WebSocket to HTTP polling CSP fallback). |

#### Methods

| Signature | Description |
| :--- | :--- |
| `Task<CommandResult> SendCommandAsync(string type, object? payload = null, CancellationToken cancellationToken = default)` | Sends a command to the browser and awaits the final result, automatically handling transient WebSocket reconnections and CSP fallback. |
| `Task<CommandResult> SendRawCommandAsync(string type, string? rawPayload, CancellationToken cancellationToken = default)` | Sends a command to the browser, automatically parsing JSON object/array strings if provided. |
| `DomNode? ResolveDomRoot(DomNode? fallbackDomRoot = null)` | Resolves the most recent root `DomNode` from `CurrentDomRoot`, `CurrentDomSnapshot`, or `fallbackDomRoot`. |
| `void SetTrackedHighlightSelector(string? selector = null)` | Resets or updates `LastHighlightedSelector` without sending a browser command. |
| `void RecordUserDomSelection(string? path, bool updateTrackedHighlight = false)` | Records an explicit user DOM selection so in-flight DOM refreshes preserve the latest user choice. |
| `void SelectDomNode(DomNode? domNode, bool highlightInBrowser = true, int debounceMs = 25)` | Records a user `DomNode` selection and optionally highlights the element in the browser with debouncing. |
| `void HighlightElementDebounced(string? selector, int debounceMs = 25)` | Highlights `selector` in the browser with debouncing and duplicate-selector suppression. |
| `Task RefreshDomSnapshotCoalescedAsync(Func<string?> getCurrentSelectedPath, Action<DomSnapshotRefreshResult> onSnapshotReady, string? targetSelectedPath = null, bool hasTargetPath = false, int timeoutSeconds = 10)` | Executes a coalesced DOM snapshot refresh; queues the latest target request if a refresh is already in flight. |
| `Task HandleElementClickedOrRefreshDomAsync(string? clickedPath, Func<string, bool> trySelectExistingPath, Action onSelectedInExistingTree, Func<string?> getCurrentSelectedPath, Action<DomSnapshotRefreshResult> onSnapshotReady, int timeoutSeconds = 10)` | Handles middle-click selection by trying the existing tree first and falling back to a coalesced DOM snapshot refresh. |

---

## 4. Namespace: `BrowserAutomationFramework.Extensions`

### 4.1 `ReactExtensions`

Static extension methods on `BrowserClient` providing high-level APIs for React-compatible DOM interaction, navigation, DOM snapshots, element highlighting, data scraping, automation execution, and site dumping.

#### DOM Interaction & Navigation Extensions

| Extension Method Signature | Description |
| :--- | :--- |
| `Task<CommandResult> ClickReactElementAsync(this BrowserClient client, string selector, CancellationToken cancellationToken = default)` | Locates an element via CSS selector and simulates a trusted React (`__reactProps$`) or native click event. |
| `Task<CommandResult> ClickReactElementAsync(this BrowserClient client, DomNode domNode, CancellationToken cancellationToken = default)` | Simulates a trusted React or native click event on `domNode.Path`. |
| `Task<CommandResult> SetReactInputValueAsync(this BrowserClient client, string selector, string value, bool overwrite = true, CancellationToken cancellationToken = default)` | Sets the value of an input/textarea element via CSS selector, bypassing React's native value setter tracker. |
| `Task<CommandResult> SetReactInputValueAsync(this BrowserClient client, DomNode domNode, string value, bool overwrite = true, CancellationToken cancellationToken = default)` | Sets the input value on `domNode.Path`, bypassing React's native value tracker. |
| `Task<CommandResult> NavigateToUrlAsync(this BrowserClient client, string url, int timeoutMs = 30000, CancellationToken cancellationToken = default)` | Navigates the browser tab to `url` and waits until the page transition completes. |
| `Task<CommandResult> ReloadPageAsync(this BrowserClient client, int timeoutMs = 30000, CancellationToken cancellationToken = default)` | Reloads the current page and waits until the reloaded page has completely loaded. |
| `Task<CommandResult> WaitForPageLoadAsync(this BrowserClient client, int timeoutMs = 30000, CancellationToken cancellationToken = default)` | Waits until the current page reports `document.readyState === 'complete'`. |
| `Task WaitForPageTransitionAsync(this BrowserClient client, string? previousPageLoadId, bool expectNavigation = true, int timeoutMs = 30000, CancellationToken cancellationToken = default)` | Waits for a full-page or in-page transition to complete relative to `previousPageLoadId`. |
| `Task<CommandResult> EvaluateJavaScriptAsync(this BrowserClient client, string code, CancellationToken cancellationToken = default)` | Evaluates raw asynchronous or synchronous JavaScript in the context of the page. |

#### DOM Tree & Highlighting Extensions

| Extension Method Signature | Description |
| :--- | :--- |
| `Task<CommandResult> GetDomAsync(this BrowserClient client, CancellationToken cancellationToken = default)` | Retrieves the raw JSON-serialized representation of the page DOM. |
| `Task<DomNode?> GetDomNodeAsync(this BrowserClient client, CancellationToken cancellationToken = default)` | Retrieves and deserializes the page DOM into a `DomNode` tree on a background thread and updates `client.CurrentDomRoot`. |
| `Task<DomTreeSnapshot?> GetDomSnapshotAsync(this BrowserClient client, CancellationToken cancellationToken = default)` | Retrieves the DOM and builds an indexed, syntax-tokenized `DomTreeSnapshot` on a background thread, caching it on `client`. |
| `Task<CommandResult> HighlightElementAsync(this BrowserClient client, string selector, CancellationToken cancellationToken = default)` | Highlights a single element on the page using its CSS selector. |
| `Task<CommandResult> RemoveHighlightAsync(this BrowserClient client, CancellationToken cancellationToken = default)` | Removes single and multi-element highlight overlays from the page. |
| `Task<int> HighlightElementsAsync(this BrowserClient client, string selector, string? containerSelector = null, bool matchAllElements = true, CancellationToken cancellationToken = default)` | Highlights all elements matching `selector` (optionally scoped to `containerSelector`) and returns the match count. |
| `Task<int> HighlightScrapeFieldAsync(this BrowserClient client, ScrapeField field, ScrapeDefinition? parentDefinition = null, CancellationToken cancellationToken = default)` | Highlights all elements matching `field` (resolved against `parentDefinition?.ContainerSelector`) and returns the match count. |
| `Task<int> HighlightScrapeContainerAsync(this BrowserClient client, ScrapeDefinition definition, CancellationToken cancellationToken = default)` | Highlights all repeating container elements configured on `definition` and returns the match count. |
| `Task<int> HighlightContainerOrSelectorAsync(this BrowserClient client, string rawSelector, ScrapeDefinition? definition = null, CancellationToken cancellationToken = default)` | Highlights `definition.ContainerSelector` if non-empty, or highlights all elements matching `rawSelector`. |
| `string GeneralizeContainerSelector(this BrowserClient? client, string rawSelector, ScrapeDefinition? definition = null, DomNode? fallbackDomRoot = null)` | Generalizes a container CSS selector using the client's cached DOM root and updates `definition` if provided. |

#### Scraping, Automation & Reverse-Engineering Extensions

| Extension Method Signature | Description |
| :--- | :--- |
| `ScrapeBuilder CreateScrape(this BrowserClient client, string name = "Scrape")` | Creates a fluent `ScrapeBuilder` bound to `client`. |
| `Task<ScrapeResult> ExecuteScrapeAsync(this BrowserClient client, ScrapeDefinition definition, IEnumerable<AutomationSequence>? availableSequences = null, DataScraper? scraper = null, CancellationToken cancellationToken = default)` | Executes `definition` on `client`. |
| `Task<List<ScrapeResult>> ExecuteProjectScrapesAsync(this BrowserClient client, AutomationProject project, DataScraper? scraper = null, CancellationToken cancellationToken = default)` | Executes all active `ScrapeDefinition` items in `project`. |
| `Task<ScrapeResult> ExecuteProjectScrapesCombinedAsync(this BrowserClient client, AutomationProject project, DataScraper? scraper = null, bool deduplicate = false, CancellationToken cancellationToken = default)` | Executes all active scrapes in `project` and combines their records into one `ScrapeResult`. |
| `Task<string?> ScrapeElementAsync(this BrowserClient client, string selector, ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText, string? attributeName = null, CancellationToken cancellationToken = default)` | Extracts a single string value from the first element matching `selector`. |
| `Task<string?> ScrapeElementAsync(this BrowserClient client, DomNode domNode, ScrapeExtractionType? extractionType = null, string? attributeName = null, CancellationToken cancellationToken = default)` | Extracts a single string value from `domNode` using the specified or auto-suggested extraction type. |
| `Task<List<string>> ScrapeElementsAsync(this BrowserClient client, string selector, ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText, string? attributeName = null, CancellationToken cancellationToken = default)` | Extracts a list of values from all elements matching `selector`. |
| `Task<ScrapeResult> ScrapeTableAsync(this BrowserClient client, string tableSelector = "table", CancellationToken cancellationToken = default)` | Automatically extracts an HTML `<table>` into a `ScrapeResult`. |
| `Task<CommandResult?> ExecuteStepAsync(this BrowserClient client, AutomationStep step, AutomationRunner? runner = null, CancellationToken cancellationToken = default)` | Executes a single `AutomationStep` on `client`. |
| `Task ExecuteSequenceAsync(this BrowserClient client, AutomationSequence sequence, AutomationRunner? runner = null, CancellationToken cancellationToken = default)` | Executes an `AutomationSequence` on `client`. |
| `Task ExecuteProjectAsync(this BrowserClient client, AutomationProject project, AutomationRunner? runner = null, CancellationToken cancellationToken = default)` | Executes all active sequences in `project` on `client`. |
| `Task<SiteDumpResult> DumpHtmlAsync(this BrowserClient client, SiteDumpOptions? options = null, SiteDumper? dumper = null, CancellationToken cancellationToken = default)` | Dumps all document and accessible iframe HTML from `client`. |
| `Task<SiteDumpResult> DumpCssAsync(this BrowserClient client, SiteDumpOptions? options = null, SiteDumper? dumper = null, CancellationToken cancellationToken = default)` | Dumps all external, inline, and constructed CSS stylesheets from `client`. |
| `Task<SiteDumpResult> DumpJavaScriptAsync(this BrowserClient client, SiteDumpOptions? options = null, SiteDumper? dumper = null, CancellationToken cancellationToken = default)` | Dumps all external, inline, and dynamic JavaScript resources from `client`. |
| `Task<SiteDumpResult> DumpAllAsync(this BrowserClient client, SiteDumpOptions? options = null, SiteDumper? dumper = null, CancellationToken cancellationToken = default)` | Dumps all HTML, CSS, and JavaScript resources from `client`. |
| `Task<SiteDumpResult> DumpSiteAsync(this BrowserClient client, SiteDumpType dumpType, SiteDumpOptions? options = null, SiteDumper? dumper = null, CancellationToken cancellationToken = default)` | Dumps site assets matching `dumpType` from `client`. |

---

## 5. Namespace: `BrowserAutomationFramework.Automation`

### 5.1 `AutomationRunner`

Modular execution engine for running `AutomationProject`, `AutomationSequence`, and `AutomationStep` workflows.

#### Delegates & Constructors

| Signature | Description |
| :--- | :--- |
| `delegate Task<CommandResult?> StepHandlerDelegate(BrowserClient client, AutomationStep step, CancellationToken cancellationToken)` | Asynchronous handler delegate that executes a single `AutomationStep`. |
| `AutomationRunner(DataScraper? scraper = null, SiteDumper? siteDumper = null)` | Initializes a runner with all default handlers (`Click`, `SetValue`, `Navigate`, `EvaluateScript`, `Scrape`, `Custom`, `DumpSite`) registered. |

#### Properties & Events

| Member | Type | Description |
| :--- | :--- | :--- |
| `Scraper` | `DataScraper` | The `DataScraper` instance used when executing `AutomationActionType.Scrape` steps. |
| `SiteDumper` | `SiteDumper` | The `SiteDumper` instance used when executing `AutomationActionType.DumpSite` steps. |
| `OnSequenceStarted` | `EventHandler<AutomationSequenceEventArgs>?` | Raised when an `AutomationSequence` begins execution. |
| `OnSequenceCompleted` | `EventHandler<AutomationSequenceEventArgs>?` | Raised when an `AutomationSequence` finishes all steps. |
| `OnStepStarted` | `EventHandler<AutomationStepEventArgs>?` | Raised immediately before an `AutomationStep` executes. |
| `OnStepCompleted` | `EventHandler<AutomationStepEventArgs>?` | Raised when an `AutomationStep` completes successfully. |
| `OnStepFailed` | `EventHandler<AutomationStepEventArgs>?` | Raised when an `AutomationStep` throws an exception. |
| `OnScrapeCompleted` | `EventHandler<ScrapeEventArgs>?` | Raised when a `Scrape` step finishes extracting data. |
| `OnSiteDumpCompleted` | `EventHandler<SiteDumpEventArgs>?` | Raised when a `DumpSite` step finishes dumping resources. |

#### Methods

| Signature | Description |
| :--- | :--- |
| `void RegisterStepHandler(AutomationActionType actionType, StepHandlerDelegate handler)` | Registers or overrides the handler for a built-in `AutomationActionType`. |
| `void RegisterCustomActionHandler(string customCommandType, StepHandlerDelegate handler)` | Registers a custom handler keyed by `AutomationStep.CustomCommandType` (case-insensitive). |
| `void RegisterScrapeDefinition(ScrapeDefinition definition)` | Registers a `ScrapeDefinition` so it can be invoked by name from a `Scrape` step. |
| `void SetScrapeDefinitions(IEnumerable<ScrapeDefinition> definitions)` | Replaces the registered `ScrapeDefinition` lookup table. |
| `Task RunProjectAsync(BrowserClient client, AutomationProject project, CancellationToken cancellationToken = default)` | Registers `project.Scrapes` and executes all active sequences in `project`. |
| `Task RunSequencesAsync(BrowserClient client, IEnumerable<AutomationSequence> sequences, CancellationToken cancellationToken = default)` | Runs a collection of sequences sequentially. |
| `Task RunSequenceAsync(BrowserClient client, AutomationSequence sequence, CancellationToken cancellationToken = default)` | Runs all steps in `sequence`, honoring `step.DelayAfterMs`. |
| `Task<CommandResult?> RunStepAsync(BrowserClient client, AutomationStep step, CancellationToken cancellationToken = default)` | Executes a single `AutomationStep` against `client`. |

---

### 5.2 `AutomationStudioSession`

High-level session controller that coordinates `AutomationServer`, `AutomationProject`, `AutomationRunner`, `DataScraper`, and `SiteDumper` into a single facade with unified structured logging and state management. Implements `IDisposable`.

#### Constants & Static Helpers

| Member | Type / Value | Description |
| :--- | :--- | :--- |
| `DefaultPort` | `int` (`9999`) | Default TCP port used by the BAF Studio server. |
| `MinPort` / `MaxPort` | `int` (`1024` / `65535`) | Valid TCP port range bounds. |
| `ClampPort(int port)` | `static int` | Clamps `port` to `[MinPort, MaxPort]`. |
| `DefaultTestCommandType` | `string` (`"EVAL"`) | Default command type for raw command testing. |
| `DefaultTestPayload` | `string` | Default JS payload for raw command testing. |
| `DefaultIpcMode` | `IpcMode` (`WebSocket`) | Default IPC transport mode. |
| `StudioShortTitle`, `StudioTitle`, `StudioVersion`, `StudioDescription`, `StudioAboutMessage` | `string` | BAF Studio application metadata constants. |
| `ProjectFileDialogFilter`, `ProjectFileExtension` | `string` | File dialog filter and default extension (`"json"`) for project files. |
| `ScrapeJsonFileDialogFilter`, `ScrapeCsvFileDialogFilter`, `DumpFileDialogFilter` | `string` | Standard file dialog filters for exports. |
| `GetProjectFilePathFromCommandLineArgs(string[]? args)` | `static string?` | Extracts the first non-switch command-line argument as a candidate project file path. |

#### Key Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Project` | `AutomationProject` | Active `AutomationProject` managed by this session. |
| `HasSequences` / `HasScrapes` | `bool` | Whether `Project` contains any sequences or scrape definitions. |
| `Server` | `AutomationServer?` | Current `AutomationServer` instance if started. |
| `Runner` | `AutomationRunner` | The session's `AutomationRunner` engine. |
| `Scraper` | `DataScraper` | The session's `DataScraper` engine. |
| `SiteDumper` | `SiteDumper` | The session's `SiteDumper` engine. |
| `LastScrapeResult` | `ScrapeResult?` | Most recent `ScrapeResult` produced during this session. |
| `LastSiteDumpResult` | `SiteDumpResult?` | Most recent `SiteDumpResult` produced during this session. |
| `DumpOptions` | `SiteDumpOptions` | Options used for the next reverse-engineering dump. |
| `ActiveDumpViewMode` | `SiteDumpViewMode` | Active reverse-engineering viewer mode. |
| `SelectedDumpResource` | `SiteResourceItem?` | Currently selected dumped resource item. |
| `ResourceFilterText` | `string?` | Current reverse-engineering resource filter text. |
| `ExclusiveTabMode` | `bool` | Whether switching Chrome Extension tabs detaches other tabs automatically. |
| `ActiveClient` | `BrowserClient?` | Gets or sets the currently active `BrowserClient` on `Server`. |
| `IsServerRunning` / `IsExtensionConnected` | `bool` | Current server and Chrome Extension connection status. |

#### Key Methods

- **Server & Extension Lifecycle:** `StartServerAsync`, `TryStartServerAsync`, `StopServer`, `ShutdownAsync`, `TrySyncDefaultChromeExtensionQuietly`, `GetExistingDefaultExtensionDirectory`, `GetExtensionStatusMessage`, `GetExtensionStatusColorRgb`, `GetExtensionTabs`, `GetHookedExtensionTabs`, `SwitchToExtensionTabAsync`, `DetachExtensionTabAsync`, `DetachHookedExtensionTabsAsync`, `FocusExtensionTabAsync`, `RefreshExtensionTabsAsync`.
- **Project & File I/O:** `ResetProject`, `LoadProjectFromFile`, `TryLoadProjectFromFile`, `TryLoadProjectFromCommandLineArgs`, `TryHandleStartupArguments`, `SaveProjectToFile`, `TrySaveProjectToFile`, `GenerateUserscript`, `ExportChromeExtension`, `TryExportChromeExtension`.
- **Project Mutation (URLs, Sequences, Steps, Scrapes, Fields):** `SetMatchAnySite`, `AddMatchUrl`, `UpdateMatchUrl`, `RemoveMatchUrl`, `AddSequence`, `TryAddSequence`, `RemoveSequence`, `RenameSequence`, `MoveSequenceUp`, `MoveSequenceDown`, `SetSequenceActive`, `AddStepToSequence`, `AddStepFromDomNode`, `RemoveStep`, `MoveStepUp`, `MoveStepDown`, `AddScrape`, `TryAddScrape`, `CreateScrapeWithFieldFromDomNode`, `CreateScrapeWithContainerFromDomNode`, `CreateScrapeWithPaginationFromDomNode`, `RemoveScrape`, `RenameScrape`, `MoveScrapeUp`, `MoveScrapeDown`, `SetScrapeActive`, `UpdateScrapeSettings`, `AddFieldToScrape`, `AddFieldFromDomNode`, `RemoveFieldFromScrape`, `MoveFieldUp`, `MoveFieldDown`, `SetContainerFromDomNode`, `SetPaginationNextFromDomNode`, `GeneralizeScrapeFieldSelector`, `GeneralizeContainerSelector`, `TryGeneralizeContainerSelector`.
- **Execution, DOM & Export:** `Log`, `RequireClient`, `ResolveDomRoot`, `CanTargetDomNode`, `SelectDomNode`, `IsActiveClient`, `RefreshActiveClientDomAsync`, `HandleActiveClientElementClickedAsync`, `SendRawCommandAsync`, `TestReactClickAsync`, `TestEvalAsync`, `ClickDomNodeAsync`, `SetDomNodeValueAsync`, `QuickScrapeDomNodeAsync`, `HighlightContainerAsync`, `HighlightScrapeFieldAsync`, `RunActiveSequencesAsync`, `PreviewScrapeAsync`, `RunActiveScrapesAsync`, `GetLastScrapeJsonForCopy`, `EnsureHasScrapeResultForExport`, `TryGetLastScrapeSuggestedFileName`, `ExportLastScrapeToJsonFile`, `ExportLastScrapeToCsvFile`, `ExecuteSiteDumpAsync`, `SetDumpOptions`, `FilterLastSiteDumpResources`, `GetActiveDumpViewText`, `GetActiveDumpViewTextForCopy`, `TryGetActiveDumpViewForExport`, `EnsureHasSiteDumpResultForExport`, `SaveActiveDumpViewToFile`, `SaveLastDumpToFolder`.

---

## 6. Namespace: `BrowserAutomationFramework.Scraping`

### 6.1 `DataScraper`

High-level execution engine for running structured web scraping jobs, single-element extractions, HTML table extractions, and multi-page automated scrapes with automatic HTTP 429 rate-limit recovery.

#### Constructors & Properties

| Member | Description |
| :--- | :--- |
| `DataScraper(AutomationRunner? runner = null)` | Initializes a new `DataScraper` instance, sharing or creating an `AutomationRunner`. |
| `AutomationRunner Runner { get; }` | Gets the `AutomationRunner` used to execute pre-scrape, post-scrape, and pagination sequences. |

#### Events

| Event | Delegate Type | Description |
| :--- | :--- | :--- |
| `OnScrapeStarted` | `EventHandler<ScrapeEventArgs>?` | Raised when a `ScrapeDefinition` begins execution. |
| `OnScrapePageCompleted` | `EventHandler<ScrapePageEventArgs>?` | Raised when a single page finishes extracting records. |
| `OnPageTurnDelay` | `EventHandler<ScrapePageDelayEventArgs>?` | Raised before waiting `PageDelayMs` prior to advancing to the next page. |
| `OnRateLimitDetected` | `EventHandler<ScrapeRateLimitEventArgs>?` | Raised when an HTTP 429 rate-limit response is detected and a retry wait begins. |
| `OnScrapeCompleted` | `EventHandler<ScrapeEventArgs>?` | Raised when a `ScrapeDefinition` finishes extracting all pages. |
| `OnScrapeFailed` | `EventHandler<ScrapeEventArgs>?` | Raised when a `ScrapeDefinition` fails with an exception. |

#### Methods

| Signature | Description |
| :--- | :--- |
| `Task<ScrapeResult> RunScrapeAsync(BrowserClient client, ScrapeDefinition definition, IEnumerable<AutomationSequence>? availableSequences = null, CancellationToken cancellationToken = default)` | Executes `definition`, including pre-scrape sequence hooks, multi-page pagination (URL pattern, sequence, or Next-button selector), HTTP 429 rate-limit backoff, C# regex/trim post-processing, row deduplication, and post-scrape sequence hooks. |
| `Task<List<ScrapeResult>> RunProjectScrapesAsync(BrowserClient client, AutomationProject project, CancellationToken cancellationToken = default)` | Executes all active `ScrapeDefinition` items in `project`. |
| `Task<ScrapeResult> RunProjectScrapesCombinedAsync(BrowserClient client, AutomationProject project, bool deduplicate = false, CancellationToken cancellationToken = default)` | Executes all active scrapes in `project` and merges their records into a single `ScrapeResult`. |
| `Task<ScrapeResult> PreviewFieldAsync(BrowserClient client, ScrapeField field, string? containerSelector = null, CancellationToken cancellationToken = default)` | Previews a single `ScrapeField` against the current page. |
| `Task<string?> ExtractValueAsync(BrowserClient client, string selector, ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText, string? attributeName = null, CancellationToken cancellationToken = default)` | Extracts a single value from the first element matching `selector`. |
| `Task<List<string>> ExtractListAsync(BrowserClient client, string selector, ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText, string? attributeName = null, CancellationToken cancellationToken = default)` | Extracts a list of values from all elements matching `selector`. |
| `Task<ScrapeResult> ExtractTableAsync(BrowserClient client, string tableSelector = "table", CancellationToken cancellationToken = default)` | Automatically extracts an HTML `<table>` into a structured `ScrapeResult` using table headers as column names. |

---

### 6.2 `ScrapeBuilder`

Fluent builder for programmatically constructing and executing `ScrapeDefinition` workflows.

| Method Signature | Description |
| :--- | :--- |
| `ScrapeBuilder(string name = "Scrape", BrowserClient? client = null)` | Initializes a builder with an optional bound `BrowserClient`. |
| `ScrapeBuilder WithContainer(string containerSelector)` | Sets the CSS selector for repeating item containers. |
| `ScrapeBuilder Field(string name, string selector, ScrapeExtractionType extractionType = ScrapeExtractionType.InnerText, string attributeName = "", bool matchAllElements = false, string defaultValue = "", string regexPattern = "")` | Appends a `ScrapeField` to extract. |
| `ScrapeBuilder WithPreScrapeAutomation(AutomationSequence sequence)` | Configures an `AutomationSequence` to run prior to scraping. |
| `ScrapeBuilder WithPostScrapeAutomation(AutomationSequence sequence)` | Configures an `AutomationSequence` to run after scraping finishes. |
| `ScrapeBuilder WithPageDelay(int pageDelayMs)` | Sets the delay in milliseconds before turning to each subsequent page. |
| `ScrapeBuilder WithPagination(string nextPageSelector, int maxPages = 5, int pageDelayMs = 1000, bool deduplicateRows = false)` | Enables multi-page scraping by clicking `nextPageSelector` between pages. |
| `ScrapeBuilder WithUrlPagination(string urlPattern, int maxPages = 5, int startPage = 1, int pageDelayMs = 1000, bool deduplicateRows = false)` | Enables multi-page scraping using a URL template (e.g., `https://example.com/items?page={page}`). |
| `ScrapeBuilder WithPaginationSequence(AutomationSequence paginationSequence, int maxPages = 5, int pageDelayMs = 1000, bool deduplicateRows = false)` | Enables multi-page scraping using a custom `AutomationSequence` to advance pages. |
| `ScrapeDefinition Build()` | Returns the constructed `ScrapeDefinition`. |
| `Task<ScrapeResult> ExecuteAsync(BrowserClient? client = null, DataScraper? scraper = null, CancellationToken cancellationToken = default)` | Executes the built `ScrapeDefinition` against the bound or provided `BrowserClient`. |

---

### 6.3 `SelectorHelper`

Static utility methods for generalizing CSS selectors, relativizing child selectors against containers, and resolving pagination URL templates.

| Static Method Signature | Description |
| :--- | :--- |
| `string ResolvePaginationUrl(string urlPattern, int pageNumber)` | Resolves a pagination URL supporting `{{page}}`, `{page}`, `{0}`, existing `?page=N`/`&p=N`/`&pg=N` query parameters, trailing `=`, or automatic `?page=N` appending. |
| `string StripNthChild(string segment)` | Removes `:nth-child(N)` from a single selector segment. |
| `string GeneralizeSelector(string selector)` | Removes `:nth-child(N)` from the final segment of `selector` so it matches all sibling elements of the same tag. |
| `string GeneralizeContainerSelector(string selector, DomNode? rootDom = null)` | Generalizes a container selector, including higher-level repeating structural wrappers when `rootDom` is supplied. |
| `string GeneralizeRepeatingFieldSelector(string selector, DomNode? rootDom = null)` | Generalizes a repeating field selector by finding repeating row/container ancestors in `rootDom` or table rows. |
| `bool IsInsideContainer(string containerSelector, string fieldSelector)` | Determines whether `fieldSelector` is located inside (or equals) `containerSelector`. |
| `string MakeRelativeSelector(string containerSelector, string fieldSelector)` | Converts a full DOM path into a selector relative to `containerSelector` (or `":scope"` if equal). |

---

## 7. Namespace: `BrowserAutomationFramework.ReverseEngineering`

### 7.1 `SiteDumper`

High-level reverse-engineering engine for dumping all loaded HTML, CSS stylesheets, and JavaScript assets from a connected browser tab.

#### Events

| Event | Delegate Type | Description |
| :--- | :--- | :--- |
| `OnDumpStarted` | `EventHandler<SiteDumpEventArgs>?` | Raised when a site dump operation starts. |
| `OnDumpCompleted` | `EventHandler<SiteDumpEventArgs>?` | Raised when a site dump operation completes successfully. |
| `OnDumpFailed` | `EventHandler<SiteDumpEventArgs>?` | Raised when a site dump operation fails with an exception. |

#### Methods

| Signature | Description |
| :--- | :--- |
| `Task<SiteDumpResult> DumpHtmlAsync(BrowserClient client, SiteDumpOptions? options = null, CancellationToken cancellationToken = default)` | Dumps all document and accessible iframe HTML from `client`. |
| `Task<SiteDumpResult> DumpCssAsync(BrowserClient client, SiteDumpOptions? options = null, CancellationToken cancellationToken = default)` | Dumps all external, inline, and constructed CSS stylesheets from `client`. |
| `Task<SiteDumpResult> DumpJavaScriptAsync(BrowserClient client, SiteDumpOptions? options = null, CancellationToken cancellationToken = default)` | Dumps all external, inline, and dynamically loaded JavaScript files from `client`. |
| `Task<SiteDumpResult> DumpAllAsync(BrowserClient client, SiteDumpOptions? options = null, CancellationToken cancellationToken = default)` | Dumps all HTML, CSS, and JavaScript in a single operation. |
| `Task<SiteDumpResult> DumpSiteAsync(BrowserClient client, SiteDumpType dumpType, SiteDumpOptions? options = null, CancellationToken cancellationToken = default)` | Executes a site dump of `dumpType` against `client`. |

---

## 8. Namespace: `BrowserAutomationFramework.Generators`

### 8.1 `ScriptGenerator`

Generates the browser-side JavaScript automation bridge for Tampermonkey userscripts and the BAF Chrome Extension.

| Static Method Signature | Description |
| :--- | :--- |
| `string Generate(AutomationProject project, int port, IpcMode mode, string? customScript = null)` | Generates a complete userscript configured for `project`'s URL match rules, `port`, and `mode`. |
| `string Generate(ScriptOptions options)` | Generates a complete userscript from `ScriptOptions`. |

### 8.2 `ChromeExtensionGenerator`

Generates an unpacked Manifest V3 Chrome Extension (`manifest.json`, `background.js`, `content_relay.js`, `bridge.js`, `popup.html`, `popup.js`).

| Static Method Signature | Description |
| :--- | :--- |
| `string GetDefaultExtensionDirectory()` | Resolves the default `ChromeExtension` directory inside the solution or application base directory. |
| `string GetExistingDefaultExtensionDirectory()` | Returns `GetDefaultExtensionDirectory()` if it exists on disk, or `""` otherwise. |
| `string SyncDefaultDirectory(int port = 9999, IpcMode mode = IpcMode.WebSocket, string? customScript = null)` | Synchronizes the unpacked Chrome Extension files in `GetDefaultExtensionDirectory()`. |
| `string ExportToDirectory(string directoryPath, int port = 9999, IpcMode mode = IpcMode.WebSocket, string? customScript = null)` | Writes all 6 Manifest V3 Chrome Extension files to `directoryPath`. |
| `Task<string> ExportToDirectoryAsync(string directoryPath, int port = 9999, IpcMode mode = IpcMode.WebSocket, string? customScript = null, CancellationToken cancellationToken = default)` | Asynchronously writes all 6 Manifest V3 Chrome Extension files to `directoryPath`. |
| `string GetLoadInstructions(string exportedDirectoryPath)` | Returns step-by-step user instructions for loading the unpacked extension in Chromium browsers. |
| `string GenerateManifestJson()` | Generates the Manifest V3 `manifest.json` content. |
| `string GenerateContentRelayJs()` | Generates `content_relay.js` (bridges `GM_xmlhttpRequest` from `MAIN` world to the service worker). |
| `string GenerateBackgroundJs(int defaultPort = 9999, IpcMode defaultMode = IpcMode.WebSocket)` | Generates the Manifest V3 `background.js` service worker. |
| `string GeneratePopupHtml()` | Generates `popup.html` for the extension toolbar popup. |
| `string GeneratePopupJs(int defaultPort = 9999)` | Generates `popup.js` for the extension toolbar popup. |

---

## 9. Namespace: `BrowserAutomationFramework.Models`

### 9.1 Project & Workflow Models

#### `AutomationProject`
Represents a complete automation and scraping project.
- **Constants:** `FileDialogFilter`, `DefaultFileExtension` (`"json"`).
- **Properties:** `MatchAnySite` (`bool`), `MatchUrls` (`List<string>`), `Sequences` (`List<AutomationSequence>`), `Scrapes` (`List<ScrapeDefinition>`), `HasSequences`, `HasScrapes`, `HasActiveSequences`, `HasActiveScrapes`, `HasValidMatchRules`.
- **Methods:** `GetActiveSequences()`, `GetActiveScrapes()`, `Reset()`, `AddMatchUrl(url)`, `UpdateMatchUrl(index, url)`, `RemoveMatchUrl(url)`, `GetSequenceNames()`, `GetSequenceDropdownOptions(noneLabel)`, `ResolveSequenceDropdownSelection(sequenceName, noneLabel)`, `GetSequenceDropdownState(...)`, `GetSuggestedSequenceName()`, `AddSequence(name)`, `TryAddSequence(name)`, `RemoveSequence(sequence)`, `RenameSequence(sequence, newName)`, `MoveSequenceUp(index)`, `MoveSequenceDown(index)`, `GetSuggestedScrapeName()`, `AddScrape(name)`, `TryAddScrape(name)`, `RemoveScrape(scrape)`, `RenameScrape(scrape, newName)`, `MoveScrapeUp(index)`, `MoveScrapeDown(index)`, `CreateScriptOptions(port, mode, customScript)`, `ToJson(writeIndented)`, `FromJson(json)`, `SaveToFile(filePath)`, `SaveToFileAsync(filePath, ct)`, `LoadFromFile(filePath)`, `LoadFromFileAsync(filePath, ct)`.

#### `AutomationSequence`
Ordered sequence of `AutomationStep` actions.
- **Properties:** `Id` (`string`), `Name` (`string`), `IsActive` (`bool`), `Steps` (`List<AutomationStep>`).
- **Methods:** `AddStep(name, actionType, selector, value, overwrite)`, `AddStepFromDomNode(domNode, actionType, value, overwrite)`, `RemoveStep(step)`, `MoveStepUp(index)`, `MoveStepDown(index)`.

#### `AutomationStep` & `AutomationActionType`
Single workflow step within an `AutomationSequence`.
- **`AutomationActionType` Enum Values:** `Click`, `SetValue`, `Navigate`, `EvaluateScript`, `Scrape`, `Custom`, `DumpSite`.
- **`AutomationStep` Properties:** `Name` (`string`), `ActionType` (`AutomationActionType`), `Selector` (`string`), `Value` (`string`), `CustomCommandType` (`string`), `Overwrite` (`bool`), `DelayAfterMs` (`int`, default `500`).

---

### 9.2 Data Scraping Models

#### `ScrapeDefinition`
Defines a structured web scraping job.
- **Constants & Clamping:** `MinMaxPages` (`1`), `MaxMaxPages` (`10000`), `MinPaginationStartPage` (`1`), `MaxPaginationStartPage` (`10000`), `MinPageDelayMs` (`0`), `MaxPageDelayMs` (`3600000`), `ClampMaxPages(value)`, `ClampPaginationStartPage(value)`, `ClampPageDelayMs(value)`, `Default`.
- **Properties:** `Id`, `Name`, `IsActive`, `ContainerSelector`, `Fields` (`List<ScrapeField>`), `PreScrapeSequenceName`, `PostScrapeSequenceName`, `EnablePagination`, `NextPageSelector`, `PaginationUrlPattern`, `PaginationStartPage`, `PaginationPageIncrement`, `PaginationSequenceName`, `MaxPages`, `WaitBeforeScrapeMs`, `PageDelayMs`, `DeduplicateRows`, `HasFields`.
- **Methods:** `AddField(...)`, `RemoveField(field)`, `UpdateSettings(...)`, `MoveFieldUp(index)`, `MoveFieldDown(index)`, `SetContainerAndRelativizeFields(containerSelector)`, `GeneralizeAndSetContainer(rawContainerSelector, rootDom)`, `SetContainerFromDomNode(domNode, rootDom)`, `AddFieldFromDomNode(...)`, `SetPaginationNextFromDomNode(domNode, rootDom, minimumMaxPages)`, `GetUniqueFieldName(baseName)`, `NormalizeSequenceSelection(selectedSequenceName)`.

#### `ScrapeField` & `ScrapeExtractionType`
Defines a single column/field to extract.
- **`ScrapeExtractionType` Enum Values:**
  - `InnerText`, `TextContent`, `InnerHtml`, `OuterHtml`, `Value`, `Attribute`, `Href`, `Src`, `DataAttribute`, `ComputedStyle`, `Exists`, `Count`.
- **`ScrapeField` Properties:** `Name`, `Selector`, `ExtractionType`, `AttributeName`, `MatchAllElements`, `DefaultValue`, `RegexPattern`, `RegexGroup`, `Trim`.
- **Methods:** `GeneralizeSelector(DomNode? rootDom = null)`, `Clone()`.

#### `ScrapeResult`
Holds extracted records and provides export/binding utilities.
- **Constants:** `JsonFileDialogFilter`, `CsvFileDialogFilter`, `JsonFileExtension` (`"json"`), `CsvFileExtension` (`"csv"`).
- **Properties:** `ScrapeId`, `ScrapeName`, `Success`, `ErrorMessage`, `TimestampUtc`, `PagesScraped`, `Records` (`List<Dictionary<string, string?>>`), `RecordCount`, `HasRecords`.
- **Methods:** `GetStatusSummary()`, `GetColumnNames()`, `GetColumnValues(fieldName)`, `GetFirstValue(fieldName)`, `ToJson(writeIndented, recordsOnly)`, `ToCsv(delimiter)`, `ToDataTable()`, `BindTo<T>()`, `Combine(results, combinedName, deduplicate)`, `Merge(other, deduplicate)`, `GetSuggestedFileName(extension)`, `SaveToJsonFile(...)`, `SaveToJsonFileAsync(...)`, `SaveToCsvFile(...)`, `SaveToCsvFileAsync(...)`.

---

### 9.3 Reverse-Engineering Models

#### `SiteDumpOptions`
Controls which page resources are captured during a dump.
- **Properties:** `IncludeHtml`, `IncludeCss`, `IncludeJavaScript`, `IncludeInlineCss`, `IncludeExternalCss`, `IncludeConstructedCss`, `IncludeInlineScripts`, `IncludeExternalScripts`, `IncludePerformanceResourceScripts`, `IncludeIframes`, `FetchTimeoutMs` (default `15000`).
- **Methods:** `SiteDumpOptions.Create(...)`, `Clone()`, `SiteDumpOptions.ForType(dumpType, baseOptions)`.

#### `SiteDumpResult`
Contains all extracted HTML, CSS, and JavaScript assets from a dump operation.
- **Properties:** `DumpType`, `PageUrl`, `PageTitle`, `TimestampUtc`, `Success`, `ErrorMessage`, `Html`, `HtmlResources`, `CssResources`, `JavaScriptResources`, `TotalResourceCount`, `HasResources`, `TotalSizeBytes`.
- **Methods:** `GetAllResources()`, `FilterResources(filter, category, searchContent)`, `GetStatusSummary()`, `GetFormattedTotalSize()`, `GetCombinedHtml()`, `GetCombinedCss()`, `GetCombinedJavaScript()`, `GetCombinedContent(viewType)`, `GetDefaultViewMode()`, `GetViewContent(...)`, `ToJson(writeIndented)`, `SaveViewContentToFile(...)`, `SaveViewContentToFileAsync(...)`, `SaveCombinedToFile(...)`, `SaveCombinedToFileAsync(...)`, `SaveToDirectory(directoryPath, includeCombinedFiles, includeManifest)`, `SaveToDirectoryAsync(...)`.

#### `SiteResourceItem`, `SiteResourceType`, `SiteDumpType`, `SiteDumpViewMode`
- **`SiteResourceType` Enum:** `Html`, `Css`, `JavaScript`.
- **`SiteDumpType` Enum:** `Html`, `Css`, `JavaScript`, `All`.
- **`SiteDumpViewMode` Enum:** `SelectedResource`, `CombinedHtml`, `CombinedCss`, `CombinedJavaScript`, `CombinedAll`.
- **`SiteResourceItem` Properties:** `Index`, `Name`, `Category`, `SourceType`, `Url`, `Content`, `SizeBytes`, `Error`.
- **`SiteResourceItem` Methods:** `GetFormattedSize()`, `GetFormattedDetails()`, `MatchesFilter(filter, searchContent)`, `SaveToFile(filePath)`, `SaveToFileAsync(filePath, ct)`.

---

### 9.4 DOM & Virtualized Tree Models

#### `DomNode`
Represents a serialized DOM element (`type == "element"`) or text node (`type == "text"`).
- **Properties:** `Type`, `IsElement`, `IsText`, `DisplayName`, `HasPath`, `TagName`, `Path`, `Text`, `Attributes` (`Dictionary<string, string>?`), `Children` (`List<DomNode>?`).
- **Methods:** `CreateSnapshot()`, `FindByPath(path)`, `QueryAll(predicate)`, `GetFormattedDetails()`, `CreateStep(actionType, value, overwrite)`, `GetGeneralizedPath(rootDom)`, `GetBestInteractiveSelector(rootDom)`, `GetSuggestedExtractionType()`, `GetSuggestedFieldName(extractionType)`, `CreateScrapeField(...)`.

#### `DomTreeSnapshot`, `DomTreeItem`, `DomSnapshotRefreshResult`
- **`DomTreeSnapshot`:** Thread-safe, pre-indexed DOM tree snapshot built via `DomTreeSnapshot.Build(DomNode rootNode)`. Provides O(1) lookup via `ItemsByKey`, `FindItemByPath(path)`, `FindNodeByPath(path)`, `PrepareExpandedState(...)`, `ExpandAncestors(...)`, and `BuildVisibleRows(expandedKeys)`.
- **`DomTreeItem`:** Pre-tokenized node entry containing `Node`, `UniqueKey`, `ParentKey`, `Depth`, `Children`, `HasChildren`, `OpenParts` (`DomDisplayPart[]`), `CollapsedSuffixParts` (`DomDisplayPart[]`), and `EstimatedCharLength`.
- **`DomSnapshotRefreshResult`:** Result of a coalesced DOM refresh containing `Snapshot`, `ResolvedSelectedPath`, `ScrollSelectionIntoView`, `SelectedNode`, and `SelectedNodeDetails`.

#### `DomTreeViewportState` & Supporting Rendering Structs
Platform-independent state and layout engine for virtualized DOM tree controls:
- **Supporting Types:** `DomTokenColor` (`Punctuation`, `TagName`, `AttributeName`, `AttributeValue`, `TextContent`), `DomTokenColorExtensions`, `DomTreeNavigationKey` (`Up`, `Down`, `PageUp`, `PageDown`, `Home`, `End`, `Left`, `Right`), `DomDisplayPart`, `DomTokenRenderSlice`, `DomRowRenderLayout`, `DomRowMeasureCache`, `DomRect`, `DomScrollBarMetrics`.
- **`DomTreeViewportState` Methods:** `UpdateFontMetrics`, `InvalidateMeasurements`, `ApplySnapshot`, `ApplyRefreshResult`, `RebuildVisibleRows`, `ToggleExpansion`, `SelectNodeByPath`, `SelectRow`, `NavigateByKey`, `HandlePointerDown`, `HandlePointerDoubleClick`, `UpdateHoverAtY`, `ClearHover`, `ApplyMouseWheel`, `CalculateScrollBarMetrics`, `CalculateScrollBarLayout`, `GetVisibleRowRange`, `EnsureItemMeasured`, `CalculateRowRenderLayout`, `EnumerateAncestorGuideLineXs`, `EnumerateVisibleTokens`, `EnsureRowVisible`.

---

### 9.5 IPC, Tab & Event Models

- **`IpcMode` Enum:** `HttpPolling`, `WebSocket`.
- **`ScriptOptions`:** Configuration for `ScriptGenerator.Generate` (`ScriptName`, `Namespace`, `Version`, `MatchUrls`, `Port`, `Mode`, `PollingIntervalMs`, `CustomScript`, `ForcedInstanceId`).
- **`BrowserTabInfo`:** Open browser tab metadata reported by the Chrome Extension (`TabId`, `WindowId`, `Index`, `Title`, `Url`, `Active`, `Status`, `CanInject`, `IsInjected`, `InstanceId`, `IsAlreadyActiveClient(...)`, `MatchesFilter(filter)`).
- **`CommandMessage` / `CommandResult` / `EventMessage`:** Wire-format IPC DTOs.
- **Event Args Classes:** `DomChangedEventArgs`, `ElementClickedEventArgs`, `PageLoadedEventArgs`, `ScrapePageDelayEventArgs`, `ScrapeRateLimitEventArgs`, `AutomationSequenceEventArgs`, `AutomationStepEventArgs`, `ScrapeEventArgs`, `ScrapePageEventArgs`, `SiteDumpEventArgs`, `ExtensionTabsEventArgs`, `AutomationLogEventArgs`, `AutomationLogLevel` & `AutomationLogLevelExtensions`.

---

### 9.6 Studio Theme & Layout Metrics

- **`StudioTheme`:** Shared dark-mode RGB color tuples (`DarkBackground`, `DarkSurface`, `SelectionBackground`, `DisabledText`, `Foreground`, `BrightForeground`, `DialogBackground`, `DialogActionBarBackground`, `DialogButtonBorder`, `DefaultButtonBackground`, `TitleBarBackground`, `TitleBarActiveText`, `TitleBarInactiveText`, `SuccessStatus`, `WarningStatus`).
- **`StudioLayoutMetrics`:** DPI-aware proportional splitter distance calculators (`CalculateLeftSidebarSplitterDistance`, `CalculateDomSplitterDistance`, `CalculateSequencesSplitterDistance`, `CalculateEditorSplitterDistance`, `CalculateScraperMainSplitterDistance`, `CalculateScraperRightSplitterDistance`, `CalculateFieldsSplitterDistance`, `CalculateReverseEngineeringMainSplitterDistance`, `CalculateReverseEngineeringLeftSplitterDistance`).

---

## 10. Namespace: `BrowserAutomationFramework.Utils`

### 10.1 `FileAssociationHelper`

Windows Registry shell file-association helper (`[SupportedOSPlatform("windows")]`).

| Static Method Signature | Description |
| :--- | :--- |
| `void EnsureAssociation(string extension, string progId, string description, string applicationFilePath)` | Registers `extension` (e.g., `".baf"`) with `progId` and `applicationFilePath` under `HKCU\Software\Classes` and notifies the Windows Shell (`SHChangeNotify`). |

---

## 11. Browser-Side JavaScript Bridge Protocol

### Built-In Bridge Commands

| Command Type | Payload Format | Description |
| :--- | :--- | :--- |
| `REACT_CLICK` | `{ selector: string }` | Simulates a React `__reactProps$` click, SPA click, or safe in-place navigation on `selector`. |
| `SET_VALUE` | `{ selector: string, value: string, overwrite: bool }` | Sets native/React input value and dispatches `input` and `change` events. |
| `NAVIGATE` | `{ url: string }` | Navigates to `url` (using in-place fetch/DOM swap for same-origin HTML to preserve bridge state and capture HTTP 429 headers). |
| `RELOAD_PAGE` | `null` | Reloads the current page via `safeNavigate`. |
| `WAIT_FOR_PAGE_LOAD` | `{ timeoutMs: number }` | Waits until `document.readyState === 'complete'` and returns page state & HTTP status. |
| `EVAL` | `{ code: string }` | Executes async JavaScript in the page context and returns the resolved value. |
| `GET_DOM` | `null` | Serializes `document.documentElement` into a `DomNode` hierarchy (up to depth 300). |
| `HIGHLIGHT_ELEMENT` | `{ selector: string }` | Tracks and highlights a single DOM element with a fixed overlay. |
| `REMOVE_HIGHLIGHT` | `null` | Clears single and multi-element highlight overlays. |
| `HIGHLIGHT_ELEMENTS` | `{ selector: string, containerSelector: string, matchAllElements: bool }` | Highlights all matched elements for 4 seconds and returns `{ count: number }`. |
| `SCRAPE_DATA` | `{ containerSelector: string, fields: ScrapeField[] }` | Extracts structured records from repeating containers or document-wide fields. |
| `SCRAPE_TABLE` | `{ selector: string }` | Extracts headers and rows from an HTML `<table>`. |
| `DUMP_HTML` / `DUMP_CSS` / `DUMP_JS` / `DUMP_SITE` | `SiteDumpOptions` | Dumps HTML, CSS, and/or JavaScript assets from the live page. |

### Registering Custom JavaScript Actions & Streaming Data

When generating a script via `ScriptOptions.CustomScript`, custom actions can be registered in JavaScript using `registerAction(name, handler)` and invoked from C# via `client.SendCommandAsync(name, payload)`:

~~~javascript
registerAction('CUSTOM_SCROLL_SCRAPE', async (payload) => {
    for (let i = 1; i <= payload.steps; i++) {
        window.scrollBy(0, 600);
        await new Promise(r => setTimeout(r, 300));
        // Stream intermediate progress back to C# (triggers BrowserClient.OnStreamDataReceived)
        await window.streamData(payload.taskId, { step: i, scrollY: window.scrollY });
    }
    return { finalScrollY: window.scrollY };
});
~~~