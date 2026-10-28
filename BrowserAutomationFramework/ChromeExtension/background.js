// BAF Chrome Extension Background Service Worker
'use strict';

const DEFAULT_PORT = 9999;
const FALLBACK_PORTS = [9999, 8081];
let currentPort = DEFAULT_PORT;
let currentMode = 'ws';
let ws = null;
let wsConnected = false;
let reconnectTimer = null;
let heartbeatTimer = null;
let attachedTabIds = new Set();
let activeAttachedTabId = null;

chrome.storage.local.get(['baf_port', 'baf_mode', 'baf_attached_tabs', 'baf_active_tab'], (data) => {
    if (data && data.baf_port) currentPort = Number(data.baf_port) || DEFAULT_PORT;
    if (data && data.baf_mode) currentMode = String(data.baf_mode);
    if (data && Array.isArray(data.baf_attached_tabs)) {
        attachedTabIds = new Set(data.baf_attached_tabs.map(Number).filter(Boolean));
    }
    if (data && data.baf_active_tab) {
        activeAttachedTabId = Number(data.baf_active_tab) || null;
    }
    connectToServer();
});

try {
    chrome.alarms.create('baf_keepalive', { periodInMinutes: 0.4 });
    chrome.alarms.onAlarm.addListener((alarm) => {
        if (alarm && alarm.name === 'baf_keepalive') {
            if (!wsConnected) connectToServer();
            else syncTabsToServer();
        }
    });
} catch (e) {}

function persistState() {
    chrome.storage.local.set({
        baf_port: currentPort,
        baf_mode: currentMode,
        baf_attached_tabs: Array.from(attachedTabIds),
        baf_active_tab: activeAttachedTabId
    });
}

function isInjectableUrl(url) {
    if (!url || typeof url !== 'string') return false;
    const lower = url.toLowerCase();
    if (lower.startsWith('chrome://') ||
        lower.startsWith('edge://') ||
        lower.startsWith('about:') ||
        lower.startsWith('chrome-extension://') ||
        lower.startsWith('devtools://') ||
        lower.startsWith('view-source:') ||
        lower.startsWith('https://chrome.google.com/webstore') ||
        lower.startsWith('https://chromewebstore.google.com')) {
        return false;
    }
    return lower.startsWith('http://') || lower.startsWith('https://') || lower.startsWith('file://');
}

async function collectOpenTabs() {
    try {
        const tabs = await chrome.tabs.query({});
        const openIds = new Set(tabs.map(t => t.id));
        for (const id of Array.from(attachedTabIds)) {
            if (!openIds.has(id)) {
                attachedTabIds.delete(id);
                if (activeAttachedTabId === id) activeAttachedTabId = null;
            }
        }

        return tabs.map(t => {
            const rawUrl = t.url || t.pendingUrl || '';
            return {
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
            };
        });
    } catch (e) {
        return [];
    }
}

async function syncTabsToServer() {
    const tabs = await collectOpenTabs();
    const payload = {
        activeAttachedTabId: activeAttachedTabId,
        attachedTabIds: Array.from(attachedTabIds),
        tabs: tabs
    };

    if (ws && ws.readyState === WebSocket.OPEN) {
        try {
            ws.send(JSON.stringify({ event: 'EXTENSION_TABS_UPDATED', payload: payload }));
            return true;
        } catch (e) {}
    }

    try {
        const res = await fetch(`http://127.0.0.1:${currentPort}/api/ext/tabs`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            const data = await res.json();
            if (data && Array.isArray(data.commands)) {
                for (const cmd of data.commands) {
                    await handleServerExtensionCommand(cmd);
                }
            }
            return true;
        }
    } catch (e) {}
    return false;
}

async function disposeBridgeInTab(tabId) {
    attachedTabIds.delete(tabId);
    if (activeAttachedTabId === tabId) {
        const remaining = Array.from(attachedTabIds);
        activeAttachedTabId = remaining.length > 0 ? remaining[remaining.length - 1] : null;
    }
    persistState();

    try {
        await chrome.scripting.executeScript({
            target: { tabId: tabId },
            world: 'MAIN',
            func: () => {
                if (window.__baf_bridge && typeof window.__baf_bridge.dispose === 'function') {
                    window.__baf_bridge.dispose();
                }
            }
        });
    } catch (e) {}
}

async function injectBridgeIntoTab(tabId, options = {}) {
    const exclusive = options.exclusive !== undefined ? Boolean(options.exclusive) : true;
    const focusTab = Boolean(options.focus);
    const forceReinject = Boolean(options.forceReinject);
    if (options.port) currentPort = Number(options.port) || currentPort;
    if (options.mode) currentMode = String(options.mode);

    let tab;
    try {
        tab = await chrome.tabs.get(tabId);
    } catch (e) {
        attachedTabIds.delete(tabId);
        await syncTabsToServer();
        return { success: false, error: 'Tab not found: ' + tabId };
    }

    const tabUrl = tab.url || tab.pendingUrl || '';
    if (!isInjectableUrl(tabUrl)) {
        return { success: false, error: 'Cannot inject script into restricted browser URL: ' + tabUrl };
    }

    if (exclusive) {
        for (const otherId of Array.from(attachedTabIds)) {
            if (otherId !== tabId) {
                await disposeBridgeInTab(otherId);
            }
        }
    }

    if (focusTab) {
        try {
            await chrome.tabs.update(tabId, { active: true });
            if (tab.windowId) {
                await chrome.windows.update(tab.windowId, { focused: true });
            }
        } catch (e) {}
    }

    try {
        if (!forceReinject) {
            const checkRes = await chrome.scripting.executeScript({
                target: { tabId: tabId },
                world: 'MAIN',
                func: (expectedPort) => {
                    if (window.__baf_bridge && window.__baf_bridge.active && window.__baf_bridge.port === expectedPort) {
                        if (typeof window.__baf_bridge.reannounce === 'function') {
                            window.__baf_bridge.reannounce();
                        }
                        return true;
                    }
                    return false;
                },
                args: [currentPort]
            });

            if (checkRes && checkRes[0] && checkRes[0].result === true) {
                attachedTabIds.add(tabId);
                activeAttachedTabId = tabId;
                persistState();
                await syncTabsToServer();
                return { success: true, tabId: tabId, instanceId: 'tab_' + tabId, reused: true };
            }
        }

        await chrome.scripting.executeScript({
            target: { tabId: tabId },
            world: 'ISOLATED',
            files: ['content_relay.js']
        });

        await chrome.scripting.executeScript({
            target: { tabId: tabId },
            world: 'MAIN',
            func: (cfg) => {
                window.__BAF_EXTENSION_CONFIG = cfg;
            },
            args: [{
                instanceId: 'tab_' + tabId,
                tabId: tabId,
                port: currentPort,
                mode: currentMode
            }]
        });

        await chrome.scripting.executeScript({
            target: { tabId: tabId },
            world: 'MAIN',
            files: ['bridge.js']
        });

        attachedTabIds.add(tabId);
        activeAttachedTabId = tabId;
        persistState();
        await syncTabsToServer();
        return { success: true, tabId: tabId, instanceId: 'tab_' + tabId };
    } catch (err) {
        return { success: false, error: err && err.message ? err.message : String(err) };
    }
}

async function handleServerExtensionCommand(cmd) {
    if (!cmd || !cmd.type) return;
    const payload = cmd.payload || cmd;

    switch (cmd.type) {
        case 'ATTACH_TAB': {
            const tabId = Number(payload.tabId);
            if (tabId) {
                await injectBridgeIntoTab(tabId, {
                    exclusive: payload.exclusive !== undefined ? payload.exclusive : true,
                    focus: Boolean(payload.focus),
                    forceReinject: Boolean(payload.forceReinject),
                    port: payload.port || currentPort,
                    mode: payload.mode || currentMode
                });
            }
            break;
        }
        case 'DETACH_TAB': {
            const tabId = Number(payload.tabId);
            if (tabId) {
                await disposeBridgeInTab(tabId);
                await syncTabsToServer();
            }
            break;
        }
        case 'FOCUS_TAB': {
            const tabId = Number(payload.tabId);
            if (tabId) {
                try {
                    const t = await chrome.tabs.get(tabId);
                    await chrome.tabs.update(tabId, { active: true });
                    if (t && t.windowId) {
                        await chrome.windows.update(t.windowId, { focused: true });
                    }
                } catch (e) {}
                await syncTabsToServer();
            }
            break;
        }
        case 'REFRESH_TABS': {
            await syncTabsToServer();
            break;
        }
    }
}

async function probeAvailableServerPort() {
    const candidates = [currentPort, ...FALLBACK_PORTS.filter(p => p !== currentPort)];
    for (const p of candidates) {
        try {
            const controller = new AbortController();
            const timer = setTimeout(() => controller.abort(), 1200);
            const res = await fetch(`http://127.0.0.1:${p}/api/ext/status`, { signal: controller.signal });
            clearTimeout(timer);
            if (res.ok) {
                if (p !== currentPort) {
                    currentPort = p;
                    persistState();
                }
                return p;
            }
        } catch (e) {}
    }
    return currentPort;
}

function scheduleReconnect(delayMs = 2500) {
    if (reconnectTimer) clearTimeout(reconnectTimer);
    reconnectTimer = setTimeout(() => {
        reconnectTimer = null;
        connectToServer();
    }, delayMs);
}

async function connectToServer() {
    if (ws && (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING)) {
        return;
    }

    const targetPort = await probeAvailableServerPort();
    try {
        ws = new WebSocket(`ws://127.0.0.1:${targetPort}/?role=extension`);
    } catch (e) {
        wsConnected = false;
        scheduleReconnect(2500);
        return;
    }

    ws.onopen = async () => {
        wsConnected = true;
        await syncTabsToServer();
        if (heartbeatTimer) clearInterval(heartbeatTimer);
        heartbeatTimer = setInterval(() => {
            if (ws && ws.readyState === WebSocket.OPEN) {
                syncTabsToServer();
            }
        }, 5000);
    };

    ws.onmessage = async (event) => {
        try {
            const msg = JSON.parse(event.data);
            await handleServerExtensionCommand(msg);
        } catch (e) {}
    };

    ws.onerror = () => {};

    ws.onclose = () => {
        wsConnected = false;
        ws = null;
        if (heartbeatTimer) {
            clearInterval(heartbeatTimer);
            heartbeatTimer = null;
        }
        scheduleReconnect(2000);
    };
}

// Tab Lifecycle Listeners
chrome.tabs.onCreated.addListener(() => { syncTabsToServer(); });
chrome.tabs.onRemoved.addListener((tabId) => {
    if (attachedTabIds.has(tabId)) {
        attachedTabIds.delete(tabId);
        if (activeAttachedTabId === tabId) activeAttachedTabId = null;
        persistState();
    }
    syncTabsToServer();
});
chrome.tabs.onActivated.addListener(() => { syncTabsToServer(); });
chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
    if (changeInfo.status === 'complete' && attachedTabIds.has(tabId)) {
        // Re-inject automatically if a full-page navigation wiped window.__baf_bridge
        await injectBridgeIntoTab(tabId, {
            exclusive: false,
            focus: false,
            forceReinject: false
        });
    } else if (changeInfo.status || changeInfo.title || changeInfo.url) {
        syncTabsToServer();
    }
});

// Handle GM_xmlhttpRequest relay & Popup messages
async function handleGmXhr(req) {
    const controller = new AbortController();
    const timeoutMs = (req && req.timeout) ? Number(req.timeout) : 15000;
    const timer = setTimeout(() => controller.abort(), timeoutMs);
    try {
        const method = (req && req.method) ? String(req.method).toUpperCase() : 'GET';
        const fetchOpts = {
            method: method,
            headers: (req && req.headers) ? req.headers : {},
            signal: controller.signal
        };
        if (req && req.data !== null && req.data !== undefined && method !== 'GET' && method !== 'HEAD') {
            fetchOpts.body = req.data;
        }
        const res = await fetch(req.url, fetchOpts);
        const text = method === 'HEAD' ? '' : await res.text();
        const rawHeaders = [];
        res.headers.forEach((val, key) => {
            rawHeaders.push(`${key}: ${val}`);
        });
        return {
            status: res.status,
            statusText: res.statusText || '',
            responseText: text,
            responseHeaders: rawHeaders.join('\r\n')
        };
    } catch (err) {
        const isAbort = Boolean(err && err.name === 'AbortError');
        return {
            error: isAbort ? 'Request timeout' : (err && err.message ? err.message : String(err)),
            isTimeout: isAbort
        };
    } finally {
        clearTimeout(timer);
    }
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (!message || !message.type) return false;

    if (message.type === 'BAF_GM_XHR') {
        handleGmXhr(message.payload).then(sendResponse);
        return true;
    }

    if (message.type === 'POPUP_GET_STATE') {
        collectOpenTabs().then(tabs => {
            sendResponse({
                connected: wsConnected,
                port: currentPort,
                mode: currentMode,
                activeAttachedTabId: activeAttachedTabId,
                attachedTabIds: Array.from(attachedTabIds),
                tabs: tabs
            });
        });
        return true;
    }

    if (message.type === 'POPUP_SET_PORT') {
        const newPort = Number(message.port);
        if (newPort >= 1024 && newPort <= 65535) {
            currentPort = newPort;
            persistState();
            if (ws) {
                try { ws.close(); } catch (e) {}
                ws = null;
            }
            connectToServer().then(() => sendResponse({ ok: true, port: currentPort }));
            return true;
        }
        sendResponse({ ok: false });
        return false;
    }

    if (message.type === 'POPUP_ATTACH_TAB') {
        const tabId = Number(message.tabId);
        const exclusive = message.exclusive !== undefined ? Boolean(message.exclusive) : true;
        injectBridgeIntoTab(tabId, { exclusive: exclusive, focus: false, forceReinject: true })
            .then(res => {
                // Also notify C# server that user selected this tab from the popup
                fetch(`http://127.0.0.1:${currentPort}/api/ext/select-tab`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ tabId: tabId, exclusive: exclusive })
                }).catch(() => {});
                sendResponse(res);
            });
        return true;
    }

    if (message.type === 'POPUP_DETACH_TAB') {
        const tabId = Number(message.tabId);
        disposeBridgeInTab(tabId).then(async () => {
            fetch(`http://127.0.0.1:${currentPort}/api/ext/detach-tab`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ tabId: tabId })
            }).catch(() => {});
            await syncTabsToServer();
            sendResponse({ ok: true });
        });
        return true;
    }

    return false;
});
