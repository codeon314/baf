using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrowserAutomationFramework.Models;

namespace BrowserAutomationFramework.Generators
{
    /// <summary>
    /// Generates an unpacked Manifest V3 Chrome Extension that reports all open browser tabs to
    /// <see cref="Server.AutomationServer"/> and allows on-demand script injection and tab switching.
    /// </summary>
    public static class ChromeExtensionGenerator
    {
        /// <summary>
        /// Resolves the default ChromeExtension directory inside the solution if present, or inside the application base directory.
        /// </summary>
        public static string GetDefaultExtensionDirectory()
        {
            string baseDir = AppContext.BaseDirectory;
            DirectoryInfo? current = new DirectoryInfo(baseDir);
            while (current != null)
            {
                string candidate = Path.Combine(current.FullName, "BrowserAutomationFramework", "ChromeExtension");
                if (Directory.Exists(Path.Combine(current.FullName, "BrowserAutomationFramework")))
                {
                    return candidate;
                }
                current = current.Parent;
            }

            return Path.Combine(baseDir, "ChromeExtension");
        }

        /// <summary>
        /// Returns <see cref="GetDefaultExtensionDirectory"/> if that directory exists on disk, or an empty string otherwise.
        /// </summary>
        public static string GetExistingDefaultExtensionDirectory()
        {
            string defaultDirectory = GetDefaultExtensionDirectory();
            return Directory.Exists(defaultDirectory) ? defaultDirectory : string.Empty;
        }

        /// <summary>
        /// Synchronizes the unpacked Chrome Extension files in <see cref="GetDefaultExtensionDirectory"/> with the specified port and IPC mode.
        /// </summary>
        public static string SyncDefaultDirectory(
            int port = 9999,
            IpcMode mode = IpcMode.WebSocket,
            string? customScript = null)
        {
            string defaultDir = GetDefaultExtensionDirectory();
            return ExportToDirectory(defaultDir, port, mode, customScript);
        }

        /// <summary>
        /// Writes all Manifest V3 Chrome Extension files (<c>manifest.json</c>, <c>background.js</c>,
        /// <c>content_relay.js</c>, <c>bridge.js</c>, <c>popup.html</c>, <c>popup.js</c>) to the specified folder.
        /// </summary>
        public static string ExportToDirectory(
            string directoryPath,
            int port = 9999,
            IpcMode mode = IpcMode.WebSocket,
            string? customScript = null)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));

            Directory.CreateDirectory(directoryPath);

            var scriptOptions = new ScriptOptions
            {
                Port = port,
                Mode = mode,
                PollingIntervalMs = 250,
                CustomScript = customScript ?? string.Empty
            };

            string bridgeJs = ScriptGenerator.Generate(scriptOptions);

            File.WriteAllText(Path.Combine(directoryPath, "manifest.json"), GenerateManifestJson(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directoryPath, "background.js"), GenerateBackgroundJs(port, mode), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directoryPath, "content_relay.js"), GenerateContentRelayJs(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directoryPath, "bridge.js"), bridgeJs, Encoding.UTF8);
            File.WriteAllText(Path.Combine(directoryPath, "popup.html"), GeneratePopupHtml(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directoryPath, "popup.js"), GeneratePopupJs(port), Encoding.UTF8);

            return directoryPath;
        }

        /// <summary>
        /// Asynchronously writes all Manifest V3 Chrome Extension files to the specified folder.
        /// </summary>
        public static async Task<string> ExportToDirectoryAsync(
            string directoryPath,
            int port = 9999,
            IpcMode mode = IpcMode.WebSocket,
            string? customScript = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));

            Directory.CreateDirectory(directoryPath);

            var scriptOptions = new ScriptOptions
            {
                Port = port,
                Mode = mode,
                PollingIntervalMs = 250,
                CustomScript = customScript ?? string.Empty
            };

            string bridgeJs = ScriptGenerator.Generate(scriptOptions);

            await File.WriteAllTextAsync(Path.Combine(directoryPath, "manifest.json"), GenerateManifestJson(), Encoding.UTF8, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directoryPath, "background.js"), GenerateBackgroundJs(port, mode), Encoding.UTF8, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directoryPath, "content_relay.js"), GenerateContentRelayJs(), Encoding.UTF8, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directoryPath, "bridge.js"), bridgeJs, Encoding.UTF8, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directoryPath, "popup.html"), GeneratePopupHtml(), Encoding.UTF8, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directoryPath, "popup.js"), GeneratePopupJs(port), Encoding.UTF8, cancellationToken);

            return directoryPath;
        }

        /// <summary>
        /// Returns step-by-step user instructions for loading the exported unpacked Chrome Extension into a Chromium browser.
        /// </summary>
        /// <param name="exportedDirectoryPath">The directory path where the unpacked extension files were exported.</param>
        public static string GetLoadInstructions(string exportedDirectoryPath)
        {
            return $"Chrome Extension exported successfully to:\n{exportedDirectoryPath}\n\n" +
                   "To load in Chrome / Edge / Brave:\n" +
                   "1. Open chrome://extensions in your browser.\n" +
                   "2. Enable 'Developer mode' (top-right toggle).\n" +
                   "3. Click 'Load unpacked' and select the exported folder.\n\n" +
                   "Once loaded, all open browser tabs will appear automatically in the 'Chrome Extension - Open Tabs' panel!";
        }

        /// <summary>
        /// Generates the Manifest V3 <c>manifest.json</c> content for the BAF Chrome Extension.
        /// </summary>
        public static string GenerateManifestJson()
        {
            return @"{
  ""manifest_version"": 3,
  ""name"": ""Browser Automation Framework Bridge"",
  ""version"": ""1.0.0"",
  ""description"": ""Reports open browser tabs to Browser Automation Framework and enables one-click script injection and tab switching."",
  ""permissions"": [
    ""tabs"",
    ""scripting"",
    ""storage"",
    ""activeTab"",
    ""alarms""
  ],
  ""host_permissions"": [
    ""<all_urls>"",
    ""http://127.0.0.1/*"",
    ""http://localhost/*"",
    ""ws://127.0.0.1/*"",
    ""ws://localhost/*""
  ],
  ""background"": {
    ""service_worker"": ""background.js""
  },
  ""action"": {
    ""default_title"": ""BAF Automation Bridge"",
    ""default_popup"": ""popup.html""
  }
}
";
        }

        /// <summary>
        /// Generates the <c>content_relay.js</c> script that runs in the <c>ISOLATED</c> world and relays <c>GM_xmlhttpRequest</c> calls to the extension service worker.
        /// </summary>
        public static string GenerateContentRelayJs()
        {
            return @"// BAF Content Script Relay (ISOLATED World)
// Bridges GM_xmlhttpRequest calls from the MAIN world automation script to the privileged background service worker.
(function() {
    'use strict';
    if (window.__baf_content_relay_installed) return;
    window.__baf_content_relay_installed = true;

    window.addEventListener('__baf_gmxhr_req', (e) => {
        let payload = null;
        try {
            payload = JSON.parse((e && e.detail) ? e.detail : '{}');
        } catch (err) {
            return;
        }
        if (!payload || !payload.reqId) return;

        const resEventName = '__baf_gmxhr_res_' + payload.reqId;
        try {
            chrome.runtime.sendMessage({ type: 'BAF_GM_XHR', payload: payload }, (response) => {
                if (chrome.runtime.lastError) {
                    window.dispatchEvent(new CustomEvent(resEventName, {
                        detail: JSON.stringify({ error: chrome.runtime.lastError.message || 'Extension runtime error' })
                    }));
                    return;
                }
                window.dispatchEvent(new CustomEvent(resEventName, {
                    detail: JSON.stringify(response || { error: 'Empty response from background service worker' })
                }));
            });
        } catch (err) {
            window.dispatchEvent(new CustomEvent(resEventName, {
                detail: JSON.stringify({ error: err && err.message ? err.message : String(err) })
            }));
        }
    });
})();
";
        }

        /// <summary>
        /// Generates the Manifest V3 <c>background.js</c> service worker script configured for the specified default port and IPC mode.
        /// </summary>
        /// <param name="defaultPort">The default local server port to connect to.</param>
        /// <param name="defaultMode">The default IPC mode (<see cref="IpcMode.WebSocket"/> or <see cref="IpcMode.HttpPolling"/>).</param>
        public static string GenerateBackgroundJs(int defaultPort = 9999, IpcMode defaultMode = IpcMode.WebSocket)
        {
            string modeStr = defaultMode == IpcMode.WebSocket ? "ws" : "http";
            return $@"// BAF Chrome Extension Background Service Worker
'use strict';

const DEFAULT_PORT = {defaultPort};
const FALLBACK_PORTS = [9999, 8081];
let currentPort = DEFAULT_PORT;
let currentMode = '{modeStr}';
let ws = null;
let wsConnected = false;
let reconnectTimer = null;
let heartbeatTimer = null;
let attachedTabIds = new Set();
let activeAttachedTabId = null;

chrome.storage.local.get(['baf_port', 'baf_mode', 'baf_attached_tabs', 'baf_active_tab'], (data) => {{
    if (data && data.baf_port) currentPort = Number(data.baf_port) || DEFAULT_PORT;
    if (data && data.baf_mode) currentMode = String(data.baf_mode);
    if (data && Array.isArray(data.baf_attached_tabs)) {{
        attachedTabIds = new Set(data.baf_attached_tabs.map(Number).filter(Boolean));
    }}
    if (data && data.baf_active_tab) {{
        activeAttachedTabId = Number(data.baf_active_tab) || null;
    }}
    connectToServer();
}});

try {{
    chrome.alarms.create('baf_keepalive', {{ periodInMinutes: 0.4 }});
    chrome.alarms.onAlarm.addListener((alarm) => {{
        if (alarm && alarm.name === 'baf_keepalive') {{
            if (!wsConnected) connectToServer();
            else syncTabsToServer();
        }}
    }});
}} catch (e) {{}}

function persistState() {{
    chrome.storage.local.set({{
        baf_port: currentPort,
        baf_mode: currentMode,
        baf_attached_tabs: Array.from(attachedTabIds),
        baf_active_tab: activeAttachedTabId
    }});
}}

function isInjectableUrl(url) {{
    if (!url || typeof url !== 'string') return false;
    const lower = url.toLowerCase();
    if (lower.startsWith('chrome://') ||
        lower.startsWith('edge://') ||
        lower.startsWith('about:') ||
        lower.startsWith('chrome-extension://') ||
        lower.startsWith('devtools://') ||
        lower.startsWith('view-source:') ||
        lower.startsWith('https://chrome.google.com/webstore') ||
        lower.startsWith('https://chromewebstore.google.com')) {{
        return false;
    }}
    return lower.startsWith('http://') || lower.startsWith('https://') || lower.startsWith('file://');
}}

async function collectOpenTabs() {{
    try {{
        const tabs = await chrome.tabs.query({{}});
        const openIds = new Set(tabs.map(t => t.id));
        for (const id of Array.from(attachedTabIds)) {{
            if (!openIds.has(id)) {{
                attachedTabIds.delete(id);
                if (activeAttachedTabId === id) activeAttachedTabId = null;
            }}
        }}

        return tabs.map(t => {{
            const rawUrl = t.url || t.pendingUrl || '';
            return {{
                tabId: t.id,
                windowId: t.windowId,
                index: t.index,
                title: t.title || '(Untitled Tab)',
                url: rawUrl,
                active: Boolean(t.active),
                status: t.status || 'complete',
                canInject: isInjectableUrl(rawUrl),
                isInjected: attachedTabIds.has(t.id),
                instanceId: 'tab_' + t.id
            }};
        }});
    }} catch (e) {{
        return [];
    }}
}}

async function syncTabsToServer() {{
    const tabs = await collectOpenTabs();
    const payload = {{
        activeAttachedTabId: activeAttachedTabId,
        attachedTabIds: Array.from(attachedTabIds),
        tabs: tabs
    }};

    if (ws && ws.readyState === WebSocket.OPEN) {{
        try {{
            ws.send(JSON.stringify({{ event: 'EXTENSION_TABS_UPDATED', payload: payload }}));
            return true;
        }} catch (e) {{}}
    }}

    try {{
        const res = await fetch(`http://127.0.0.1:${{currentPort}}/api/ext/tabs`, {{
            method: 'POST',
            headers: {{ 'Content-Type': 'application/json' }},
            body: JSON.stringify(payload)
        }});
        if (res.ok) {{
            const data = await res.json();
            if (data && Array.isArray(data.commands)) {{
                for (const cmd of data.commands) {{
                    await handleServerExtensionCommand(cmd);
                }}
            }}
            return true;
        }}
    }} catch (e) {{}}
    return false;
}}

async function disposeBridgeInTab(tabId) {{
    attachedTabIds.delete(tabId);
    if (activeAttachedTabId === tabId) {{
        const remaining = Array.from(attachedTabIds);
        activeAttachedTabId = remaining.length > 0 ? remaining[remaining.length - 1] : null;
    }}
    persistState();

    try {{
        await chrome.scripting.executeScript({{
            target: {{ tabId: tabId }},
            world: 'MAIN',
            func: () => {{
                if (window.__baf_bridge && typeof window.__baf_bridge.dispose === 'function') {{
                    window.__baf_bridge.dispose();
                }}
            }}
        }});
    }} catch (e) {{}}
}}

async function injectBridgeIntoTab(tabId, options = {{}}) {{
    const exclusive = options.exclusive !== undefined ? Boolean(options.exclusive) : true;
    const focusTab = Boolean(options.focus);
    const forceReinject = Boolean(options.forceReinject);
    if (options.port) currentPort = Number(options.port) || currentPort;
    if (options.mode) currentMode = String(options.mode);

    let tab;
    try {{
        tab = await chrome.tabs.get(tabId);
    }} catch (e) {{
        attachedTabIds.delete(tabId);
        await syncTabsToServer();
        return {{ success: false, error: 'Tab not found: ' + tabId }};
    }}

    const tabUrl = tab.url || tab.pendingUrl || '';
    if (!isInjectableUrl(tabUrl)) {{
        return {{ success: false, error: 'Cannot inject script into restricted browser URL: ' + tabUrl }};
    }}

    if (exclusive) {{
        for (const otherId of Array.from(attachedTabIds)) {{
            if (otherId !== tabId) {{
                await disposeBridgeInTab(otherId);
            }}
        }}
    }}

    if (focusTab) {{
        try {{
            await chrome.tabs.update(tabId, {{ active: true }});
            if (tab.windowId) {{
                await chrome.windows.update(tab.windowId, {{ focused: true }});
            }}
        }} catch (e) {{}}
    }}

    try {{
        if (!forceReinject) {{
            const checkRes = await chrome.scripting.executeScript({{
                target: {{ tabId: tabId }},
                world: 'MAIN',
                func: (expectedPort) => {{
                    if (window.__baf_bridge && window.__baf_bridge.active && window.__baf_bridge.port === expectedPort) {{
                        if (typeof window.__baf_bridge.reannounce === 'function') {{
                            window.__baf_bridge.reannounce();
                        }}
                        return true;
                    }}
                    return false;
                }},
                args: [currentPort]
            }});

            if (checkRes && checkRes[0] && checkRes[0].result === true) {{
                attachedTabIds.add(tabId);
                activeAttachedTabId = tabId;
                persistState();
                await syncTabsToServer();
                return {{ success: true, tabId: tabId, instanceId: 'tab_' + tabId, reused: true }};
            }}
        }}

        await chrome.scripting.executeScript({{
            target: {{ tabId: tabId }},
            world: 'ISOLATED',
            files: ['content_relay.js']
        }});

        await chrome.scripting.executeScript({{
            target: {{ tabId: tabId }},
            world: 'MAIN',
            func: (cfg) => {{
                window.__BAF_EXTENSION_CONFIG = cfg;
            }},
            args: [{{
                instanceId: 'tab_' + tabId,
                tabId: tabId,
                port: currentPort,
                mode: currentMode
            }}]
        }});

        await chrome.scripting.executeScript({{
            target: {{ tabId: tabId }},
            world: 'MAIN',
            files: ['bridge.js']
        }});

        attachedTabIds.add(tabId);
        activeAttachedTabId = tabId;
        persistState();
        await syncTabsToServer();
        return {{ success: true, tabId: tabId, instanceId: 'tab_' + tabId }};
    }} catch (err) {{
        return {{ success: false, error: err && err.message ? err.message : String(err) }};
    }}
}}

async function handleServerExtensionCommand(cmd) {{
    if (!cmd || !cmd.type) return;
    const payload = cmd.payload || cmd;

    switch (cmd.type) {{
        case 'ATTACH_TAB': {{
            const tabId = Number(payload.tabId);
            if (tabId) {{
                await injectBridgeIntoTab(tabId, {{
                    exclusive: payload.exclusive !== undefined ? payload.exclusive : true,
                    focus: Boolean(payload.focus),
                    forceReinject: Boolean(payload.forceReinject),
                    port: payload.port || currentPort,
                    mode: payload.mode || currentMode
                }});
            }}
            break;
        }}
        case 'DETACH_TAB': {{
            const tabId = Number(payload.tabId);
            if (tabId) {{
                await disposeBridgeInTab(tabId);
                await syncTabsToServer();
            }}
            break;
        }}
        case 'FOCUS_TAB': {{
            const tabId = Number(payload.tabId);
            if (tabId) {{
                try {{
                    const t = await chrome.tabs.get(tabId);
                    await chrome.tabs.update(tabId, {{ active: true }});
                    if (t && t.windowId) {{
                        await chrome.windows.update(t.windowId, {{ focused: true }});
                    }}
                }} catch (e) {{}}
                await syncTabsToServer();
            }}
            break;
        }}
        case 'REFRESH_TABS': {{
            await syncTabsToServer();
            break;
        }}
    }}
}}

async function probeAvailableServerPort() {{
    const candidates = [currentPort, ...FALLBACK_PORTS.filter(p => p !== currentPort)];
    for (const p of candidates) {{
        try {{
            const controller = new AbortController();
            const timer = setTimeout(() => controller.abort(), 1200);
            const res = await fetch(`http://127.0.0.1:${{p}}/api/ext/status`, {{ signal: controller.signal }});
            clearTimeout(timer);
            if (res.ok) {{
                if (p !== currentPort) {{
                    currentPort = p;
                    persistState();
                }}
                return p;
            }}
        }} catch (e) {{}}
    }}
    return currentPort;
}}

function scheduleReconnect(delayMs = 2500) {{
    if (reconnectTimer) clearTimeout(reconnectTimer);
    reconnectTimer = setTimeout(() => {{
        reconnectTimer = null;
        connectToServer();
    }}, delayMs);
}}

async function connectToServer() {{
    if (ws && (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING)) {{
        return;
    }}

    const targetPort = await probeAvailableServerPort();
    try {{
        ws = new WebSocket(`ws://127.0.0.1:${{targetPort}}/?role=extension`);
    }} catch (e) {{
        wsConnected = false;
        scheduleReconnect(2500);
        return;
    }}

    ws.onopen = async () => {{
        wsConnected = true;
        await syncTabsToServer();
        if (heartbeatTimer) clearInterval(heartbeatTimer);
        heartbeatTimer = setInterval(() => {{
            if (ws && ws.readyState === WebSocket.OPEN) {{
                syncTabsToServer();
            }}
        }}, 5000);
    }};

    ws.onmessage = async (event) => {{
        try {{
            const msg = JSON.parse(event.data);
            await handleServerExtensionCommand(msg);
        }} catch (e) {{}}
    }};

    ws.onerror = () => {{}};

    ws.onclose = () => {{
        wsConnected = false;
        ws = null;
        if (heartbeatTimer) {{
            clearInterval(heartbeatTimer);
            heartbeatTimer = null;
        }}
        scheduleReconnect(2000);
    }};
}}

// Tab Lifecycle Listeners
chrome.tabs.onCreated.addListener(() => {{ syncTabsToServer(); }});
chrome.tabs.onRemoved.addListener((tabId) => {{
    if (attachedTabIds.has(tabId)) {{
        attachedTabIds.delete(tabId);
        if (activeAttachedTabId === tabId) activeAttachedTabId = null;
        persistState();
    }}
    syncTabsToServer();
}});
chrome.tabs.onActivated.addListener(() => {{ syncTabsToServer(); }});
chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {{
    if (changeInfo.status === 'complete' && attachedTabIds.has(tabId)) {{
        // Re-inject automatically if a full-page navigation wiped window.__baf_bridge
        await injectBridgeIntoTab(tabId, {{
            exclusive: false,
            focus: false,
            forceReinject: false
        }});
    }} else if (changeInfo.status || changeInfo.title || changeInfo.url) {{
        syncTabsToServer();
    }}
}});

// Handle GM_xmlhttpRequest relay & Popup messages
async function handleGmXhr(req) {{
    const controller = new AbortController();
    const timeoutMs = (req && req.timeout) ? Number(req.timeout) : 15000;
    const timer = setTimeout(() => controller.abort(), timeoutMs);
    try {{
        const method = (req && req.method) ? String(req.method).toUpperCase() : 'GET';
        const fetchOpts = {{
            method: method,
            headers: (req && req.headers) ? req.headers : {{}},
            signal: controller.signal
        }};
        if (req && req.data !== null && req.data !== undefined && method !== 'GET' && method !== 'HEAD') {{
            fetchOpts.body = req.data;
        }}
        const res = await fetch(req.url, fetchOpts);
        const text = method === 'HEAD' ? '' : await res.text();
        const rawHeaders = [];
        res.headers.forEach((val, key) => {{
            rawHeaders.push(`${{key}}: ${{val}}`);
        }});
        return {{
            status: res.status,
            statusText: res.statusText || '',
            responseText: text,
            responseHeaders: rawHeaders.join('\r\n')
        }};
    }} catch (err) {{
        const isAbort = Boolean(err && err.name === 'AbortError');
        return {{
            error: isAbort ? 'Request timeout' : (err && err.message ? err.message : String(err)),
            isTimeout: isAbort
        }};
    }} finally {{
        clearTimeout(timer);
    }}
}}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {{
    if (!message || !message.type) return false;

    if (message.type === 'BAF_GM_XHR') {{
        handleGmXhr(message.payload).then(sendResponse);
        return true;
    }}

    if (message.type === 'POPUP_GET_STATE') {{
        collectOpenTabs().then(tabs => {{
            sendResponse({{
                connected: wsConnected,
                port: currentPort,
                mode: currentMode,
                activeAttachedTabId: activeAttachedTabId,
                attachedTabIds: Array.from(attachedTabIds),
                tabs: tabs
            }});
        }});
        return true;
    }}

    if (message.type === 'POPUP_SET_PORT') {{
        const newPort = Number(message.port);
        if (newPort >= 1024 && newPort <= 65535) {{
            currentPort = newPort;
            persistState();
            if (ws) {{
                try {{ ws.close(); }} catch (e) {{}}
                ws = null;
            }}
            connectToServer().then(() => sendResponse({{ ok: true, port: currentPort }}));
            return true;
        }}
        sendResponse({{ ok: false }});
        return false;
    }}

    if (message.type === 'POPUP_ATTACH_TAB') {{
        const tabId = Number(message.tabId);
        const exclusive = message.exclusive !== undefined ? Boolean(message.exclusive) : true;
        injectBridgeIntoTab(tabId, {{ exclusive: exclusive, focus: false, forceReinject: true }})
            .then(res => {{
                // Also notify C# server that user selected this tab from the popup
                fetch(`http://127.0.0.1:${{currentPort}}/api/ext/select-tab`, {{
                    method: 'POST',
                    headers: {{ 'Content-Type': 'application/json' }},
                    body: JSON.stringify({{ tabId: tabId, exclusive: exclusive }})
                }}).catch(() => {{}});
                sendResponse(res);
            }});
        return true;
    }}

    if (message.type === 'POPUP_DETACH_TAB') {{
        const tabId = Number(message.tabId);
        disposeBridgeInTab(tabId).then(async () => {{
            fetch(`http://127.0.0.1:${{currentPort}}/api/ext/detach-tab`, {{
                method: 'POST',
                headers: {{ 'Content-Type': 'application/json' }},
                body: JSON.stringify({{ tabId: tabId }})
            }}).catch(() => {{}});
            await syncTabsToServer();
            sendResponse({{ ok: true }});
        }});
        return true;
    }}

    return false;
}});
";
        }

        /// <summary>
        /// Generates the HTML markup for the Chrome Extension's toolbar popup (<c>popup.html</c>).
        /// </summary>
        public static string GeneratePopupHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>BAF Automation Bridge</title>
    <style>
        body {
            width: 380px;
            margin: 0;
            padding: 12px;
            background-color: #1e1e1e;
            color: #d4d4d4;
            font-family: 'Segoe UI', Tahoma, sans-serif;
            font-size: 13px;
        }
        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 10px;
            padding-bottom: 8px;
            border-bottom: 1px solid #333;
        }
        .title {
            font-weight: 600;
            font-size: 14px;
            color: #fff;
        }
        .badge {
            padding: 2px 8px;
            border-radius: 3px;
            font-size: 11px;
            font-weight: 600;
        }
        .badge-connected { background: #135c2d; color: #73f59b; }
        .badge-disconnected { background: #663c00; color: #ffca6b; }
        .port-row {
            display: flex;
            align-items: center;
            gap: 6px;
            margin-bottom: 10px;
        }
        input[type=""number""] {
            width: 80px;
            padding: 4px 6px;
            background: #252526;
            border: 1px solid #454545;
            color: #fff;
        }
        button {
            background: #0e639c;
            color: #fff;
            border: 1px solid #1177bb;
            padding: 5px 10px;
            cursor: pointer;
            font-size: 12px;
        }
        button:hover { background: #1177bb; }
        button.btn-secondary {
            background: #333337;
            border-color: #46464a;
        }
        button.btn-secondary:hover { background: #3e3e42; }
        button.btn-danger {
            background: #7a2121;
            border-color: #9e2e2e;
        }
        .current-box {
            background: #252526;
            border: 1px solid #3c3c3c;
            padding: 8px;
            margin-bottom: 10px;
        }
        .tab-list {
            max-height: 240px;
            overflow-y: auto;
            border: 1px solid #3c3c3c;
            background: #252526;
        }
        .tab-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 6px 8px;
            border-bottom: 1px solid #2d2d30;
            gap: 8px;
        }
        .tab-item.injected {
            background: rgba(19, 92, 45, 0.28);
            border-left: 3px solid #2ea043;
        }
        .tab-info {
            flex: 1;
            min-width: 0;
        }
        .tab-title {
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            color: #e0e0e0;
            font-weight: 500;
        }
        .tab-url {
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            color: #888;
            font-size: 11px;
        }
    </style>
</head>
<body>
    <div class=""header"">
        <span class=""title"">BAF Automation Bridge</span>
        <span id=""statusBadge"" class=""badge badge-disconnected"">Checking...</span>
    </div>

    <div class=""port-row"">
        <label for=""portInput"">Server Port:</label>
        <input id=""portInput"" type=""number"" min=""1024"" max=""65535"" value=""9999"" />
        <button id=""btnSavePort"" class=""btn-secondary"">Apply / Reconnect</button>
    </div>

    <div class=""current-box"">
        <div style=""margin-bottom: 6px; font-weight: 600;"">Active Browser Tab</div>
        <div id=""currentTabTitle"" class=""tab-title"" style=""margin-bottom: 6px;"">Loading...</div>
        <div style=""display: flex; gap: 6px;"">
            <button id=""btnAttachCurrent"" style=""flex: 1;"">Inject &amp; Switch Here</button>
            <button id=""btnDetachCurrent"" class=""btn-danger"" style=""display: none;"">Detach</button>
        </div>
    </div>

    <div style=""margin-bottom: 4px; font-weight: 600;"">All Open Tabs</div>
    <div id=""tabList"" class=""tab-list""></div>

    <script src=""popup.js""></script>
</body>
</html>
";
        }

        /// <summary>
        /// Generates the JavaScript logic for the Chrome Extension's toolbar popup (<c>popup.js</c>).
        /// </summary>
        /// <param name="defaultPort">The default local server port displayed in the popup UI.</param>
        public static string GeneratePopupJs(int defaultPort = 9999)
        {
            return $@"'use strict';

const statusBadge = document.getElementById('statusBadge');
const portInput = document.getElementById('portInput');
const btnSavePort = document.getElementById('btnSavePort');
const currentTabTitle = document.getElementById('currentTabTitle');
const btnAttachCurrent = document.getElementById('btnAttachCurrent');
const btnDetachCurrent = document.getElementById('btnDetachCurrent');
const tabList = document.getElementById('tabList');

let activeFocusedTab = null;

function renderState(state) {{
    if (!state) return;
    portInput.value = state.port || {defaultPort};

    if (state.connected) {{
        statusBadge.textContent = 'Connected';
        statusBadge.className = 'badge badge-connected';
    }} else {{
        statusBadge.textContent = 'Server Offline';
        statusBadge.className = 'badge badge-disconnected';
    }}

    const tabs = Array.isArray(state.tabs) ? state.tabs : [];
    activeFocusedTab = tabs.find(t => t.active) || tabs[0] || null;

    if (activeFocusedTab) {{
        currentTabTitle.textContent = `[#${{activeFocusedTab.tabId}}] ${{activeFocusedTab.title}}`;
        btnAttachCurrent.disabled = !activeFocusedTab.canInject;
        btnAttachCurrent.textContent = activeFocusedTab.isInjected ? 'Re-Sync This Tab' : 'Inject & Switch Here';
        btnDetachCurrent.style.display = activeFocusedTab.isInjected ? 'inline-block' : 'none';
    }} else {{
        currentTabTitle.textContent = '(No active tab)';
        btnAttachCurrent.disabled = true;
    }}

    tabList.innerHTML = '';
    tabs.forEach(tab => {{
        const row = document.createElement('div');
        row.className = 'tab-item' + (tab.isInjected ? ' injected' : '');

        const info = document.createElement('div');
        info.className = 'tab-info';

        const titleEl = document.createElement('div');
        titleEl.className = 'tab-title';
        titleEl.textContent = (tab.isInjected ? '[HOOKED] ' : '') + `[#${{tab.tabId}}] ${{tab.title}}`;

        const urlEl = document.createElement('div');
        urlEl.className = 'tab-url';
        urlEl.textContent = tab.url;

        info.appendChild(titleEl);
        info.appendChild(urlEl);

        const actionBtn = document.createElement('button');
        if (!tab.canInject) {{
            actionBtn.textContent = 'Restricted';
            actionBtn.disabled = true;
            actionBtn.className = 'btn-secondary';
        }} else if (tab.isInjected) {{
            actionBtn.textContent = 'Detach';
            actionBtn.className = 'btn-danger';
            actionBtn.onclick = () => {{
                chrome.runtime.sendMessage({{ type: 'POPUP_DETACH_TAB', tabId: tab.tabId }}, () => refreshPopup());
            }};
        }} else {{
            actionBtn.textContent = 'Switch';
            actionBtn.onclick = () => {{
                chrome.runtime.sendMessage({{ type: 'POPUP_ATTACH_TAB', tabId: tab.tabId, exclusive: true }}, () => refreshPopup());
            }};
        }}

        row.appendChild(info);
        row.appendChild(actionBtn);
        tabList.appendChild(row);
    }});
}}

function refreshPopup() {{
    chrome.runtime.sendMessage({{ type: 'POPUP_GET_STATE' }}, (state) => {{
        renderState(state);
    }});
}}

btnSavePort.addEventListener('click', () => {{
    const p = Number(portInput.value);
    chrome.runtime.sendMessage({{ type: 'POPUP_SET_PORT', port: p }}, () => {{
        setTimeout(refreshPopup, 250);
    }});
}});

btnAttachCurrent.addEventListener('click', () => {{
    if (!activeFocusedTab) return;
    chrome.runtime.sendMessage({{ type: 'POPUP_ATTACH_TAB', tabId: activeFocusedTab.tabId, exclusive: true }}, () => {{
        refreshPopup();
    }});
}});

btnDetachCurrent.addEventListener('click', () => {{
    if (!activeFocusedTab) return;
    chrome.runtime.sendMessage({{ type: 'POPUP_DETACH_TAB', tabId: activeFocusedTab.tabId }}, () => {{
        refreshPopup();
    }});
}});

refreshPopup();
";
        }
    }
}