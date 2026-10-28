// ==UserScript==
// @name         Generated Automation Bridge
// @namespace    automation.bridge
// @version      1.0.0
// @match        *://*/*
// @connect      127.0.0.1
// @connect      localhost
// @connect      *
// @grant        GM_xmlhttpRequest
// @grant        GM_addElement
// @run-at       document-idle
// ==/UserScript==

(function() {
    'use strict';

    if (window.__baf_bridge && typeof window.__baf_bridge.dispose === 'function') {
        try { window.__baf_bridge.dispose(); } catch (e) {}
    }

    const EXT_CFG = (typeof window.__BAF_EXTENSION_CONFIG === 'object' && window.__BAF_EXTENSION_CONFIG) ? window.__BAF_EXTENSION_CONFIG : null;
    let isDisposed = false;
    let __bafGmReqCounter = 0;

    if (typeof window.GM_xmlhttpRequest !== 'function' && EXT_CFG) {
        window.GM_xmlhttpRequest = function(details) {
            const reqId = 'gmxhr_' + Date.now() + '_' + (++__bafGmReqCounter) + '_' + Math.random().toString(36).substr(2, 6);
            const resEventName = '__baf_gmxhr_res_' + reqId;
            const onResponse = (e) => {
                window.removeEventListener(resEventName, onResponse);
                try {
                    const res = JSON.parse((e && e.detail) ? e.detail : '{}');
                    if (res.error) {
                        if (res.isTimeout && typeof details.ontimeout === 'function') details.ontimeout(new Error(res.error));
                        else if (typeof details.onerror === 'function') details.onerror(new Error(res.error));
                    } else if (typeof details.onload === 'function') {
                        details.onload({
                            status: res.status || 200,
                            statusText: res.statusText || '',
                            responseText: res.responseText !== undefined && res.responseText !== null ? res.responseText : '',
                            responseHeaders: res.responseHeaders || ''
                        });
                    }
                } catch (err) {
                    if (typeof details.onerror === 'function') details.onerror(err);
                }
            };
            window.addEventListener(resEventName, onResponse, { once: true });
            window.dispatchEvent(new CustomEvent('__baf_gmxhr_req', {
                detail: JSON.stringify({
                    reqId: reqId,
                    method: details.method || 'GET',
                    url: details.url,
                    data: details.data !== undefined ? details.data : null,
                    headers: details.headers || {},
                    timeout: details.timeout || 15000
                })
            }));
        };
    }
    const GM_xmlhttpRequest = (typeof window.GM_xmlhttpRequest === 'function') ? window.GM_xmlhttpRequest : undefined;
    const PORT = (EXT_CFG && EXT_CFG.port) ? Number(EXT_CFG.port) : 9999;
    const FORCED_INSTANCE_ID = (EXT_CFG && EXT_CFG.instanceId) ? String(EXT_CFG.instanceId) : null;
    function getOrCreateInstanceId() {
        if (FORCED_INSTANCE_ID) {
            return FORCED_INSTANCE_ID;
        }
        const key = '__baf_instance_id';
        try {
            if (window.name && window.name.startsWith('bot_')) {
                sessionStorage.setItem(key, window.name);
                return window.name;
            }
            const stored = sessionStorage.getItem(key);
            if (stored && stored.startsWith('bot_')) {
                window.name = stored;
                return stored;
            }
        } catch (e) {}
        const newId = 'bot_' + Math.random().toString(36).substr(2, 9);
        try {
            window.name = newId;
            sessionStorage.setItem(key, newId);
        } catch (e) {}
        return newId;
    }
    const INSTANCE_ID = getOrCreateInstanceId();
    let PAGE_LOAD_ID = 'page_' + Date.now() + '_' + Math.random().toString(36).substr(2, 7);
    let currentPageState = null;

    function showRateLimitBanner(url, retryAfter) {
        try {
            let banner = document.getElementById('__baf_ratelimit_banner');
            if (!banner) {
                banner = document.createElement('div');
                banner.id = '__baf_ratelimit_banner';
                banner.style.position = 'fixed';
                banner.style.top = '0';
                banner.style.left = '0';
                banner.style.right = '0';
                banner.style.backgroundColor = 'rgba(200, 80, 0, 0.95)';
                banner.style.color = '#fff';
                banner.style.padding = '10px 16px';
                banner.style.fontFamily = 'Segoe UI, sans-serif';
                banner.style.fontSize = '14px';
                banner.style.fontWeight = 'bold';
                banner.style.zIndex = '2147483647';
                banner.style.textAlign = 'center';
                banner.style.boxShadow = '0 2px 8px rgba(0,0,0,0.4)';
                (document.documentElement || document.body).appendChild(banner);
            }
            const retryMsg = retryAfter ? ` (Retry-After: ${retryAfter})` : '';
            banner.textContent = `[Automation Bridge] HTTP 429 Rate Limit detected on ${url}${retryMsg} — Keeping session alive and waiting to retry...`;
        } catch (e) {}
    }

    function clearRateLimitBanner() {
        try {
            const banner = document.getElementById('__baf_ratelimit_banner');
            if (banner) banner.remove();
        } catch (e) {}
    }

    function parseRetryAfterFromRawHeaders(rawHeaders) {
        if (!rawHeaders || typeof rawHeaders !== 'string') return null;
        const lines = rawHeaders.split(/\r?\n/);
        for (const line of lines) {
            const idx = line.indexOf(':');
            if (idx > 0) {
                const key = line.substring(0, idx).trim().toLowerCase();
                const val = line.substring(idx + 1).trim();
                if (key === 'retry-after' || key === 'x-retry-after' || key === 'ratelimit-reset' || key === 'x-ratelimit-reset') {
                    return val;
                }
            }
        }
        return null;
    }

    async function probeCurrentPageHeaders() {
        if (typeof GM_xmlhttpRequest === 'function') {
            try {
                const gmRes = await new Promise((resolve) => {
                    GM_xmlhttpRequest({
                        method: 'HEAD',
                        url: window.location.href,
                        onload: (res) => {
                            const retry = parseRetryAfterFromRawHeaders(res.responseHeaders || '');
                            resolve({ status: res.status, retryAfter: retry });
                        },
                        onerror: () => resolve(null),
                        ontimeout: () => resolve(null),
                        timeout: 5000
                    });
                });
                if (gmRes && gmRes.status > 0) {
                    return gmRes;
                }
            } catch (e) {}
        }

        try {
            const res = await fetch(window.location.href, {
                method: 'HEAD',
                credentials: 'same-origin',
                cache: 'no-store'
            });
            let retry = null;
            if (res.headers && typeof res.headers.get === 'function') {
                retry = res.headers.get('Retry-After') ||
                        res.headers.get('retry-after') ||
                        res.headers.get('X-Retry-After') ||
                        res.headers.get('RateLimit-Reset') ||
                        res.headers.get('X-RateLimit-Reset');
            }
            return { status: res.status, retryAfter: retry };
        } catch (e) {}

        return null;
    }

    async function detectPageHttpStatus() {
        let statusCode = 200;
        let retryAfter = null;

        try {
            const navEntries = performance.getEntriesByType && performance.getEntriesByType('navigation');
            if (navEntries && navEntries.length > 0 && typeof navEntries[0].responseStatus === 'number' && navEntries[0].responseStatus > 0) {
                statusCode = navEntries[0].responseStatus;
            }
        } catch (e) {}

        const titleText = (document.title || '').trim();
        const bodySample = (document.body && document.body.innerText) ? document.body.innerText.substring(0, 2000) : '';
        if (statusCode !== 429) {
            if (/\b429\b/.test(titleText) || /too many requests/i.test(titleText) ||
                (/\b429\b/.test(bodySample) && /(too many requests|rate limit|retry after)/i.test(bodySample))) {
                statusCode = 429;
            }
        }

        if (statusCode === 429) {
            const probed = await probeCurrentPageHeaders();
            if (probed && probed.retryAfter) {
                retryAfter = probed.retryAfter;
            }
            if (!retryAfter && bodySample) {
                const match = bodySample.match(/retry[- ]after[:\s]+([0-9]+(?:\s*(?:seconds?|secs?|s))?|[A-Za-z]{3},\s+\d{1,2}\s+[A-Za-z]{3}\s+\d{4}\s+\d{2}:\d{2}:\d{2}\s+GMT)/i);
                if (match && match[1]) {
                    retryAfter = match[1].trim();
                }
            }
        }

        return { statusCode, retryAfter };
    }

    function waitForDocumentComplete(timeoutMs = 30000) {
        return new Promise((resolve) => {
            const buildStateAsync = async () => {
                if (currentPageState && currentPageState.pageLoadId === PAGE_LOAD_ID) {
                    return currentPageState;
                }
                const httpInfo = await detectPageHttpStatus();
                currentPageState = {
                    pageLoadId: PAGE_LOAD_ID,
                    url: window.location.href,
                    readyState: document.readyState,
                    title: document.title || '',
                    statusCode: httpInfo.statusCode,
                    retryAfter: httpInfo.retryAfter
                };
                return currentPageState;
            };
            if (document.readyState === 'complete') {
                requestAnimationFrame(() => { buildStateAsync().then(resolve); });
                return;
            }
            let settled = false;
            const finish = () => {
                if (settled) return;
                settled = true;
                window.removeEventListener('load', finish);
                document.removeEventListener('readystatechange', onReadyState);
                requestAnimationFrame(() => { buildStateAsync().then(resolve); });
            };
            const onReadyState = () => {
                if (document.readyState === 'complete') finish();
            };
            window.addEventListener('load', finish, { once: true });
            document.addEventListener('readystatechange', onReadyState);
            setTimeout(finish, timeoutMs);
        });
    }


    async function safeNavigate(rawUrl, isReload = false) {
        const targetUrl = new URL(rawUrl || window.location.href, window.location.href).href;
        const prevLoadId = PAGE_LOAD_ID;

        let targetParsed;
        try {
            targetParsed = new URL(targetUrl);
        } catch (e) {
            setTimeout(() => { window.location.href = targetUrl; }, 25);
            return { navigatingTo: targetUrl, previousPageLoadId: prevLoadId };
        }

        if (targetParsed.origin !== window.location.origin) {
            setTimeout(() => { window.location.href = targetUrl; }, 25);
            return { navigatingTo: targetUrl, previousPageLoadId: prevLoadId };
        }

        try {
            const res = await fetch(targetUrl, {
                method: 'GET',
                credentials: 'same-origin',
                redirect: 'follow',
                cache: isReload ? 'reload' : 'no-store',
                headers: {
                    'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8'
                }
            });

            const finalUrl = res.url || targetUrl;

            if (res.status === 429) {
                let retryAfter = null;
                if (res.headers && typeof res.headers.get === 'function') {
                    retryAfter = res.headers.get('Retry-After') ||
                                 res.headers.get('retry-after') ||
                                 res.headers.get('X-Retry-After') ||
                                 res.headers.get('RateLimit-Reset') ||
                                 res.headers.get('X-RateLimit-Reset');
                }
                const errBody = await res.text().catch(() => '');
                if (!retryAfter && errBody) {
                    const match = errBody.match(/retry[- ]after[:\s]+([0-9]+(?:\s*(?:seconds?|secs?|s))?|[A-Za-z]{3},\s+\d{1,2}\s+[A-Za-z]{3}\s+\d{4}\s+\d{2}:\d{2}:\d{2}\s+GMT)/i);
                    if (match && match[1]) {
                        retryAfter = match[1].trim();
                    }
                }

                try { history.replaceState({}, document.title || '', finalUrl); } catch (e) {}
                showRateLimitBanner(finalUrl, retryAfter);

                PAGE_LOAD_ID = 'page_' + Date.now() + '_' + Math.random().toString(36).substr(2, 7);
                currentPageState = {
                    pageLoadId: PAGE_LOAD_ID,
                    url: finalUrl,
                    readyState: 'complete',
                    title: document.title || '',
                    statusCode: 429,
                    retryAfter: retryAfter
                };

                if (typeof sendEvent === 'function') {
                    sendEvent('PAGE_LOADED', currentPageState);
                }

                return {
                    navigatingTo: finalUrl,
                    previousPageLoadId: prevLoadId,
                    pageLoadId: PAGE_LOAD_ID,
                    statusCode: 429,
                    retryAfter: retryAfter
                };
            }

            const contentType = (res.headers && typeof res.headers.get === 'function')
                ? (res.headers.get('content-type') || '')
                : '';

            if (!contentType.toLowerCase().includes('text/html') && !contentType.toLowerCase().includes('application/xhtml+xml')) {
                setTimeout(() => { window.location.href = finalUrl; }, 25);
                return { navigatingTo: finalUrl, previousPageLoadId: prevLoadId };
            }

            const html = await res.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');

            if (!doc || !doc.body) {
                setTimeout(() => { window.location.href = finalUrl; }, 25);
                return { navigatingTo: finalUrl, previousPageLoadId: prevLoadId };
            }

            const bodyTextLen = (doc.body.textContent || '').trim().length;
            const hasExternalScripts = doc.querySelectorAll('script[src]').length > 0;
            if (bodyTextLen < 20 && hasExternalScripts && doc.body.children.length <= 3) {
                setTimeout(() => { window.location.href = finalUrl; }, 25);
                return { navigatingTo: finalUrl, previousPageLoadId: prevLoadId };
            }

            clearHighlight(false);
            clearRateLimitBanner();

            try {
                if (isReload || finalUrl === window.location.href) {
                    history.replaceState({}, doc.title || '', finalUrl);
                } else {
                    history.pushState({}, doc.title || '', finalUrl);
                }
            } catch (e) {}

            if (doc.title !== undefined) {
                document.title = doc.title;
            }

            if (doc.head && document.head) {
                const existingLinks = new Set(
                    Array.from(document.head.querySelectorAll('link[rel="stylesheet"]'))
                        .map(l => l.getAttribute('href'))
                        .filter(Boolean)
                );
                doc.head.querySelectorAll('link[rel="stylesheet"]').forEach(link => {
                    const href = link.getAttribute('href');
                    if (href && !existingLinks.has(href)) {
                        document.head.appendChild(link.cloneNode(true));
                    }
                });
            }

            Array.from(document.body.attributes).forEach(attr => document.body.removeAttribute(attr.name));
            Array.from(doc.body.attributes).forEach(attr => document.body.setAttribute(attr.name, attr.value));
            document.body.innerHTML = doc.body.innerHTML;

            try { window.scrollTo(0, 0); } catch (e) {}

            PAGE_LOAD_ID = 'page_' + Date.now() + '_' + Math.random().toString(36).substr(2, 7);
            currentPageState = {
                pageLoadId: PAGE_LOAD_ID,
                url: window.location.href,
                readyState: 'complete',
                title: document.title || '',
                statusCode: res.status || 200,
                retryAfter: null
            };

            if (typeof sendEvent === 'function') {
                sendEvent('PAGE_LOADED', currentPageState);
                sendEvent('DOM_CHANGED', { highlightedPath: null });
            }

            return {
                navigatingTo: window.location.href,
                previousPageLoadId: prevLoadId,
                pageLoadId: PAGE_LOAD_ID,
                statusCode: currentPageState.statusCode
            };
        } catch (err) {
            setTimeout(() => { window.location.href = targetUrl; }, 25);
            return { navigatingTo: targetUrl, previousPageLoadId: prevLoadId };
        }
    }

    // --- React & DOM Utilities ---
    const ReactUtils = {
        simulateClick: async function(element) {
            const target = (element.closest && element.closest('a, button, input, summary, [role="button"]'))
                || (element.querySelector && element.querySelector('a[href], button, input[type="submit"], input[type="button"], [role="button"]'))
                || element;

            const tag = (target.tagName || '').toLowerCase();
            const href = (tag === 'a' && target.href) ? target.href : null;
            const hrefAttr = (tag === 'a' && target.getAttribute) ? (target.getAttribute('href') || '').trim() : '';
            const targetAttr = (tag === 'a' && target.getAttribute) ? (target.getAttribute('target') || '').trim() : '';
            const isNavigableLink = tag === 'a' && href && hrefAttr && hrefAttr !== '#' && !hrefAttr.toLowerCase().startsWith('javascript:') && targetAttr !== '_blank';

            if (isNavigableLink) {
                let spaPrevented = false;
                const onWindowClick = (ev) => {
                    if (ev.defaultPrevented) {
                        spaPrevented = true;
                    } else {
                        ev.preventDefault();
                    }
                };
                window.addEventListener('click', onWindowClick, { capture: false, once: true });
                try {
                    if (typeof target.click === 'function') {
                        target.click();
                    } else {
                        target.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, composed: true, view: window, button: 0, buttons: 1 }));
                    }
                } catch (e) {
                } finally {
                    window.removeEventListener('click', onWindowClick, { capture: false });
                }

                if (spaPrevented) {
                    return 'SPA link click executed (' + href + ')';
                }

                await safeNavigate(href, false);
                return 'Link click executed (' + href + ')';
            }

            const candidates = target !== element ? [element, target] : [element];
            for (const cand of candidates) {
                try {
                    const reactKey = Object.keys(cand).find(k => k.startsWith('__reactProps$') || k.startsWith('__reactEventHandlers$'));
                    if (reactKey && cand[reactKey]) {
                        const props = cand[reactKey];
                        if (typeof props.onClick === 'function') {
                            const nativeEvent = new MouseEvent('click', { bubbles: true, cancelable: true, view: window, button: 0, buttons: 1 });
                            const fakeNativeEvent = new Proxy(nativeEvent, {
                                get(t, prop) {
                                    if (prop === 'isTrusted') return true;
                                    const value = Reflect.get(t, prop);
                                    if (typeof value === 'function') return value.bind(t);
                                    return value;
                                }
                            });
                            props.onClick({
                                bubbles: true,
                                cancelable: true,
                                defaultPrevented: false,
                                isTrusted: true,
                                preventDefault: function() { this.defaultPrevented = true; },
                                stopPropagation: () => {},
                                target: cand,
                                currentTarget: cand,
                                nativeEvent: fakeNativeEvent,
                                button: 0
                            });
                            return 'React click simulated';
                        }
                    }
                } catch (e) { console.error('React click failed', e); }
            }

            try {
                if (typeof target.focus === 'function') target.focus();
            } catch (e) {}

            try {
                const downOpts = { bubbles: true, cancelable: true, composed: true, view: window, button: 0, buttons: 1 };
                const upOpts = { bubbles: true, cancelable: true, composed: true, view: window, button: 0, buttons: 0 };
                if (typeof PointerEvent === 'function') {
                    target.dispatchEvent(new PointerEvent('pointerdown', downOpts));
                }
                target.dispatchEvent(new MouseEvent('mousedown', downOpts));
                if (typeof PointerEvent === 'function') {
                    target.dispatchEvent(new PointerEvent('pointerup', upOpts));
                }
                target.dispatchEvent(new MouseEvent('mouseup', upOpts));
            } catch (e) {}

            try {
                if (typeof target.click === 'function') {
                    target.click();
                } else {
                    target.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, composed: true, view: window, button: 0, buttons: 1 }));
                }
            } catch (e) {}

            return 'Native/Fallback click executed';
        },
        setNativeValue: function(element, value, overwrite) {
            element.focus();
            if (overwrite) {
                try { element.select(); } catch(e) {}
                const valueSetter = Object.getOwnPropertyDescriptor(element, 'value')?.set;
                const prototype = Object.getPrototypeOf(element);
                const prototypeValueSetter = Object.getOwnPropertyDescriptor(prototype, 'value')?.set;
                if (prototypeValueSetter && valueSetter !== prototypeValueSetter) {
                    prototypeValueSetter.call(element, '');
                } else if (valueSetter) {
                    valueSetter.call(element, '');
                } else {
                    element.value = '';
                }
            }
            let success = false;
            try { success = document.execCommand('insertText', false, value); } catch (e) {}
            if (!success) {
                const valueSetter = Object.getOwnPropertyDescriptor(element, 'value')?.set;
                const prototype = Object.getPrototypeOf(element);
                const prototypeValueSetter = Object.getOwnPropertyDescriptor(prototype, 'value')?.set;
                const currentValue = overwrite ? '' : element.value;
                if (prototypeValueSetter && valueSetter !== prototypeValueSetter) {
                    prototypeValueSetter.call(element, currentValue + value);
                } else if (valueSetter) {
                    valueSetter.call(element, currentValue + value);
                } else {
                    element.value = currentValue + value;
                }
            }
            element.dispatchEvent(new Event('input', { bubbles: true }));
            element.dispatchEvent(new Event('change', { bubbles: true }));
            return 'Value set';
        }
    };

    // --- Highlight State & Tracking ---
    let highlightOverlay = null;
    let highlightedElement = null;
    let highlightRafId = null;
    let lastHighlightRect = { top: null, left: null, width: null, height: null, display: null };

    function ensureHighlightOverlay() {
        if (!highlightOverlay || !highlightOverlay.isConnected) {
            highlightOverlay = document.createElement('div');
            highlightOverlay.id = '__baf_highlight_overlay';
            highlightOverlay.style.position = 'fixed';
            highlightOverlay.style.backgroundColor = 'rgba(0, 150, 255, 0.3)';
            highlightOverlay.style.border = '2px solid rgba(0, 150, 255, 0.8)';
            highlightOverlay.style.boxSizing = 'border-box';
            highlightOverlay.style.pointerEvents = 'none';
            highlightOverlay.style.zIndex = '2147483647';
            highlightOverlay.style.margin = '0';
            highlightOverlay.style.padding = '0';
            highlightOverlay.style.display = 'none';
            (document.documentElement || document.body).appendChild(highlightOverlay);
        }
        return highlightOverlay;
    }

    let multiHighlightContainer = null;
    let multiHighlightTimer = null;

    function clearMultiHighlights() {
        if (multiHighlightTimer) {
            clearTimeout(multiHighlightTimer);
            multiHighlightTimer = null;
        }
        if (multiHighlightContainer && multiHighlightContainer.isConnected) {
            multiHighlightContainer.innerHTML = '';
            multiHighlightContainer.style.display = 'none';
        }
    }

    function clearHighlight(notifyServer = false) {
        clearMultiHighlights();
        const hadElement = highlightedElement !== null;
        highlightedElement = null;
        lastHighlightRect = { top: null, left: null, width: null, height: null, display: null };
        if (highlightRafId !== null) {
            cancelAnimationFrame(highlightRafId);
            highlightRafId = null;
        }
        if (highlightOverlay) {
            highlightOverlay.style.display = 'none';
        }
        if (notifyServer && hadElement && typeof sendEvent === 'function') {
            sendEvent('ELEMENT_REMOVED', null);
        }
    }

    function updateHighlightPosition() {
        if (!highlightedElement) return;

        if (!highlightedElement.isConnected || !document.documentElement.contains(highlightedElement)) {
            clearHighlight(true);
            return;
        }

        const overlay = ensureHighlightOverlay();
        const rect = highlightedElement.getBoundingClientRect();
        const newDisplay = (rect.width === 0 && rect.height === 0) ? 'none' : 'block';

        if (newDisplay === 'none') {
            if (lastHighlightRect.display !== 'none') {
                overlay.style.display = 'none';
                lastHighlightRect.display = 'none';
            }
            return;
        }

        if (rect.top !== lastHighlightRect.top ||
            rect.left !== lastHighlightRect.left ||
            rect.width !== lastHighlightRect.width ||
            rect.height !== lastHighlightRect.height ||
            lastHighlightRect.display !== 'block') {
            overlay.style.top = rect.top + 'px';
            overlay.style.left = rect.left + 'px';
            overlay.style.width = rect.width + 'px';
            overlay.style.height = rect.height + 'px';
            overlay.style.display = 'block';

            lastHighlightRect = {
                top: rect.top,
                left: rect.left,
                width: rect.width,
                height: rect.height,
                display: 'block'
            };
        }
    }

    function startHighlightTracking() {
        if (highlightRafId !== null) {
            cancelAnimationFrame(highlightRafId);
        }
        const tick = () => {
            if (!highlightedElement) {
                highlightRafId = null;
                return;
            }
            updateHighlightPosition();
            if (highlightedElement) {
                highlightRafId = requestAnimationFrame(tick);
            }
        };
        highlightRafId = requestAnimationFrame(tick);
    }

    window.addEventListener('scroll', updateHighlightPosition, { capture: true, passive: true });
    window.addEventListener('resize', updateHighlightPosition, { capture: true, passive: true });

    // --- DOM Observer ---
    let domTimeout;
    const observer = new MutationObserver((mutations) => {
        const isIgnoredNode = (node) => {
            if (!node) return false;
            if (node.id === '__baf_highlight_overlay' || node.id === '__baf_multi_highlight_container' || node.id === '__baf_ratelimit_banner') return true;
            if (node.closest && node.closest('#__baf_multi_highlight_container')) return true;
            if (node.id && typeof node.id === 'string' && node.id.startsWith('eval_') && node.id.endsWith('_script')) return true;
            return false;
        };
        
        const hasMeaningfulChange = mutations.some(m => {
            if (isIgnoredNode(m.target)) return false;
            if (m.type === 'childList') {
                let meaningfulNodes = false;
                for (let i = 0; i < m.addedNodes.length; i++) {
                    if (!isIgnoredNode(m.addedNodes[i])) meaningfulNodes = true;
                }
                for (let i = 0; i < m.removedNodes.length; i++) {
                    if (!isIgnoredNode(m.removedNodes[i])) meaningfulNodes = true;
                }
                if (!meaningfulNodes && (m.addedNodes.length > 0 || m.removedNodes.length > 0)) {
                    return false;
                }
            }
            return true;
        });

        if (!hasMeaningfulChange) return;

        if (highlightedElement) {
            updateHighlightPosition();
        }

        clearTimeout(domTimeout);
        domTimeout = setTimeout(() => {
            if (typeof sendEvent === 'function') {
                const currentHighlightedPath = (highlightedElement && highlightedElement.isConnected)
                    ? getElementPath(highlightedElement)
                    : null;
                sendEvent('DOM_CHANGED', { highlightedPath: currentHighlightedPath });
            }
        }, 500);
    });
    observer.observe(document.documentElement, { childList: true, subtree: true, attributes: true, characterData: true });

    // --- Command Registry ---
    const actionHandlers = Object.create(null);

    function registerAction(name, handler) {
        actionHandlers[name] = handler;
    }

    // Built-in Commands
    registerAction('REACT_CLICK', async (payload) => {
        const el = document.querySelector(payload.selector);
        if (!el) throw new Error('Element not found: ' + payload.selector);
        return await ReactUtils.simulateClick(el);
    });

    registerAction('SET_VALUE', async (payload) => {
        const el = document.querySelector(payload.selector);
        if (!el) throw new Error('Element not found: ' + payload.selector);
        const overwrite = payload.overwrite !== undefined ? payload.overwrite : true;
        return ReactUtils.setNativeValue(el, payload.value, overwrite);
    });

    registerAction('NAVIGATE', async (payload) => {
        const rawUrl = (payload && payload.url) ? String(payload.url).trim() : (typeof payload === 'string' ? payload.trim() : '');
        if (!rawUrl) throw new Error('Navigation URL cannot be empty.');
        return await safeNavigate(rawUrl, false);
    });

    registerAction('RELOAD_PAGE', async (payload) => {
        const reloadUrl = (currentPageState && currentPageState.url) ? currentPageState.url : window.location.href;
        return await safeNavigate(reloadUrl, true);
    });

    registerAction('WAIT_FOR_PAGE_LOAD', async (payload) => {
        const timeoutMs = (payload && payload.timeoutMs) ? Number(payload.timeoutMs) : 30000;
        return await waitForDocumentComplete(timeoutMs);
    });

    registerAction('EVAL', async (payload) => {
        const evalFallback = async () => {
            const AsyncFunction = Object.getPrototypeOf(async function() {}).constructor;
            const fn = new AsyncFunction(payload.code);
            return await fn();
        };

        return new Promise((resolve, reject) => {
            const id = 'eval_' + Math.random().toString(36).substr(2, 9);
            let started = false;
            let settled = false;

            const cleanup = () => {
                document.removeEventListener(id, listener);
                document.removeEventListener(id + '_start', startListener);
                const s = document.getElementById(id + '_script');
                if (s) s.remove();
            };

            const startListener = () => {
                started = true;
            };

            const listener = (e) => {
                if (settled) return;
                settled = true;
                cleanup();
                try {
                    const data = JSON.parse(e.detail);
                    if (data.err) reject(new Error(data.err));
                    else resolve(data.res);
                } catch (err) {
                    reject(err);
                }
            };

            document.addEventListener(id + '_start', startListener, { once: true });
            document.addEventListener(id, listener, { once: true });

            const code = `
                (async function() {
                    document.dispatchEvent(new CustomEvent('${id}_start'));
                    let result = { err: null, res: null };
                    try {
                        result.res = await (async function() { ${payload.code} })();
                    } catch (e) {
                        result.err = e.message || String(e);
                    } finally {
                        document.dispatchEvent(new CustomEvent('${id}', { detail: JSON.stringify(result) }));
                        const s = document.getElementById('${id}_script');
                        if (s) s.remove();
                    }
                })();
            `;

            try {
                if (typeof GM_addElement === 'function') {
                    GM_addElement('script', { id: id + '_script', textContent: code });
                } else {
                    const script = document.createElement('script');
                    script.id = id + '_script';
                    script.textContent = code;
                    (document.body || document.documentElement).appendChild(script);
                }

                setTimeout(async () => {
                    if (!started && !settled) {
                        settled = true;
                        cleanup();
                        try {
                            const res = await evalFallback();
                            resolve(res);
                        } catch (fallbackErr) {
                            reject(new Error('Script execution blocked by CSP: ' + (fallbackErr.message || String(fallbackErr))));
                        }
                    }
                }, 60);
            } catch (e) {
                if (!settled) {
                    settled = true;
                    cleanup();
                    evalFallback().then(resolve).catch(err => reject(new Error('Script injection failed: ' + (err.message || e.message))));
                }
            }
        });
    });

    function getElementPath(el) {
        if (!el || el.nodeType !== Node.ELEMENT_NODE) return '';
        if (el.id === '__baf_highlight_overlay' || el.id === '__baf_multi_highlight_container' || el.id === '__baf_ratelimit_banner') return '';
        if (el.closest && el.closest('#__baf_multi_highlight_container')) return '';
        if (el.id && typeof el.id === 'string' && el.id.startsWith('eval_') && el.id.endsWith('_script')) return '';
        
        let path = '';
        if (el.tagName.toLowerCase() === 'html') {
            path = 'html';
        } else if (el.tagName.toLowerCase() === 'body') {
            path = 'body';
        } else {
            let current = el;
            let pathParts = [];
            while (current && current.nodeType === Node.ELEMENT_NODE && current.tagName.toLowerCase() !== 'html' && current.tagName.toLowerCase() !== 'body') {
                let index = 1;
                let sibling = current.previousElementSibling;
                while (sibling) {
                    index++;
                    sibling = sibling.previousElementSibling;
                }
                pathParts.unshift(`${current.tagName.toLowerCase()}:nth-child(${index})`);
                current = current.parentElement;
            }
            if (current && current.tagName.toLowerCase() === 'body') pathParts.unshift('body');
            else if (current && current.tagName.toLowerCase() === 'html') pathParts.unshift('html');
            path = pathParts.join(' > ');
        }
        return path;
    }

    const MAX_DOM_DEPTH = 300;

    function isInternalBridgeNode(el) {
        if (!el || el.nodeType !== Node.ELEMENT_NODE) return false;
        if (el.id === '__baf_highlight_overlay' || el.id === '__baf_multi_highlight_container' || el.id === '__baf_ratelimit_banner') return true;
        if (el.id && typeof el.id === 'string' && el.id.startsWith('eval_') && el.id.endsWith('_script')) return true;
        return false;
    }

    function serializeElement(el, depth = 0, computedPath = null) {
        if (depth > MAX_DOM_DEPTH) return null;
        if (el.nodeType === Node.TEXT_NODE) {
            const text = el.textContent.trim();
            return text ? { type: 'text', text: text } : null;
        }
        if (el.nodeType !== Node.ELEMENT_NODE) return null;
        if (isInternalBridgeNode(el)) return null;

        const tag = el.tagName.toLowerCase();
        let path = computedPath;
        if (!path) {
            if (tag === 'html') path = 'html';
            else if (tag === 'body') path = 'body';
            else path = getElementPath(el);
        }

        const node = {
            type: 'element',
            tagName: tag,
            path: path,
            attributes: {}
        };

        const attrs = el.attributes;
        if (attrs) {
            for (let i = 0, len = attrs.length; i < len; i++) {
                const attr = attrs[i];
                node.attributes[attr.name] = attr.value;
            }
        }

        node.children = [];
        if (depth < MAX_DOM_DEPTH) {
            let elementIndex = 0;
            const childNodes = el.childNodes;
            for (let i = 0, len = childNodes.length; i < len; i++) {
                const child = childNodes[i];
                if (child.nodeType === Node.ELEMENT_NODE) {
                    elementIndex++;
                    if (isInternalBridgeNode(child)) continue;
                    const childTag = child.tagName.toLowerCase();
                    const childPath = childTag === 'body'
                        ? 'body'
                        : `${path} > ${childTag}:nth-child(${elementIndex})`;
                    const serializedChild = serializeElement(child, depth + 1, childPath);
                    if (serializedChild) node.children.push(serializedChild);
                } else if (child.nodeType === Node.TEXT_NODE) {
                    const serializedText = serializeElement(child, depth + 1, null);
                    if (serializedText) node.children.push(serializedText);
                }
            }
        }
        return node;
    }

    registerAction('GET_DOM', async (payload) => {
        return serializeElement(document.documentElement, 0, 'html');
    });

    registerAction('HIGHLIGHT_ELEMENT', async (payload) => {
        const el = document.querySelector(payload.selector);
        if (!el) {
            clearHighlight(false);
            return 'Element not found';
        }
        
        highlightedElement = el;
        lastHighlightRect = { top: null, left: null, width: null, height: null, display: null };
        updateHighlightPosition();
        startHighlightTracking();
        
        return 'Highlighted';
    });

    registerAction('REMOVE_HIGHLIGHT', async (payload) => {
        clearHighlight(false);
        return 'Highlight removed';
    });

    // --- Data Scraping Utilities & Actions ---
    const ScrapeUtils = {
        stripNthChild: function(seg) {
            return (seg || '').replace(/:nth-child\(\d+\)/gi, '').trim();
        },
        resolveRelativeSelector: function(containerSelector, fieldSelector) {
            if (!fieldSelector || fieldSelector === ':scope' || fieldSelector === 'self') return ':scope';
            if (!containerSelector) return fieldSelector;

            const cParts = containerSelector.split('>').map(s => s.trim()).filter(Boolean);
            const fParts = fieldSelector.split('>').map(s => s.trim()).filter(Boolean);

            if (fParts.length >= cParts.length && cParts.length > 0) {
                let isMatch = true;
                for (let i = 0; i < cParts.length; i++) {
                    if (this.stripNthChild(cParts[i]).toLowerCase() !== this.stripNthChild(fParts[i]).toLowerCase()) {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch) {
                    if (fParts.length === cParts.length) return ':scope';
                    return ':scope > ' + fParts.slice(cParts.length).join(' > ');
                }
            }
            return fieldSelector;
        },
        queryInContainer: function(containerEl, containerSelector, fieldSelector) {
            if (!containerEl) return [];
            const rel = this.resolveRelativeSelector(containerSelector, fieldSelector);
            if (rel === ':scope' || rel === 'self') return [containerEl];

            let matched = [];
            try { matched = Array.from(containerEl.querySelectorAll(rel)); } catch (e) {}
            if (matched.length === 0 && rel.startsWith(':scope > ')) {
                try { matched = Array.from(containerEl.querySelectorAll(rel.substring(9))); } catch (e) {}
            }
            if (matched.length === 0 && rel !== fieldSelector) {
                try { matched = Array.from(document.querySelectorAll(fieldSelector)); } catch (e) {}
            }
            return matched;
        },
        queryFieldOnDocument: function(fieldSelector, matchAll) {
            if (!fieldSelector) return [];
            if (!matchAll) {
                try {
                    const el = document.querySelector(fieldSelector);
                    return el ? [el] : [];
                } catch (e) { return []; }
            }

            let els = [];
            try { els = Array.from(document.querySelectorAll(fieldSelector)); } catch (e) {}

            if (els.length <= 1 && fieldSelector.includes(':nth-child(')) {
                const parts = fieldSelector.split('>').map(s => s.trim()).filter(Boolean);
                for (let i = parts.length - 2; i >= 0; i--) {
                    const segTag = this.stripNthChild(parts[i]).toLowerCase();
                    if (segTag === 'td' || segTag === 'th') continue;
                    if (parts[i].includes(':nth-child(')) {
                        const testParts = parts.slice();
                        testParts[i] = this.stripNthChild(testParts[i]);
                        try {
                            const found = Array.from(document.querySelectorAll(testParts.join(' > ')));
                            if (found.length > 1) return found;
                        } catch (e) {}
                    }
                }
                if (parts.length > 0 && parts[parts.length - 1].includes(':nth-child(')) {
                    const lastTag = this.stripNthChild(parts[parts.length - 1]).toLowerCase();
                    if (lastTag !== 'td' && lastTag !== 'th') {
                        const testParts = parts.slice();
                        testParts[testParts.length - 1] = this.stripNthChild(testParts[testParts.length - 1]);
                        try {
                            const found = Array.from(document.querySelectorAll(testParts.join(' > ')));
                            if (found.length > 1) return found;
                        } catch (e) {}
                    }
                }
            }
            return els;
        },
        extractValue: function(el, field, matchCount) {
            const type = String(field.extractionType ?? 'InnerText').toLowerCase();
            let val = null;

            if (type === 'count' || type === '11') {
                val = String(matchCount !== undefined ? matchCount : (el ? 1 : 0));
            } else if (type === 'exists' || type === '10') {
                val = el ? 'true' : 'false';
            } else if (el) {
                switch (type) {
                    case 'innertext':
                    case '0':
                        val = el.innerText !== undefined ? el.innerText : el.textContent;
                        break;
                    case 'textcontent':
                    case '1':
                        val = el.textContent;
                        break;
                    case 'innerhtml':
                    case '2':
                        val = el.innerHTML;
                        break;
                    case 'outerhtml':
                    case '3':
                        val = el.outerHTML;
                        break;
                    case 'value':
                    case '4':
                        val = el.value !== undefined ? String(el.value) : el.getAttribute('value');
                        break;
                    case 'attribute':
                    case '5':
                        val = field.attributeName ? el.getAttribute(field.attributeName) : '';
                        break;
                    case 'href':
                    case '6':
                        val = el.href || el.getAttribute('href') || (el.closest && el.closest('a') ? el.closest('a').href : '') || (el.querySelector && el.querySelector('a') ? el.querySelector('a').href : '');
                        break;
                    case 'src':
                    case '7':
                        val = el.src || el.getAttribute('src') || (el.querySelector && el.querySelector('img,video,audio,source,iframe') ? el.querySelector('img,video,audio,source,iframe').src : '');
                        break;
                    case 'dataattribute':
                    case '8': {
                        if (field.attributeName) {
                            const attr = field.attributeName.startsWith('data-') ? field.attributeName : 'data-' + field.attributeName;
                            val = el.getAttribute(attr);
                        }
                        break;
                    }
                    case 'computedstyle':
                    case '9':
                        val = field.attributeName ? window.getComputedStyle(el).getPropertyValue(field.attributeName) : '';
                        break;
                    default:
                        val = el.innerText !== undefined ? el.innerText : el.textContent;
                        break;
                }
            }

            if (val === null || val === undefined || val === '') {
                return field.defaultValue !== undefined && field.defaultValue !== null ? String(field.defaultValue) : '';
            }

            let strVal = String(val);
            if (field.trim !== false) {
                strVal = strVal.trim();
            }

            if (field.regexPattern) {
                try {
                    const rx = new RegExp(field.regexPattern);
                    const m = rx.exec(strVal);
                    if (m) {
                        const grp = field.regexGroup || 0;
                        strVal = m[grp] !== undefined ? m[grp] : m[0];
                    } else {
                        strVal = field.defaultValue || '';
                    }
                } catch (e) {}
            }

            return strVal;
        }
    };

    registerAction('HIGHLIGHT_ELEMENTS', async (payload) => {
        clearHighlight(false);
        const selector = payload.selector || '';
        const containerSelector = payload.containerSelector || '';
        const matchAll = payload.matchAllElements !== false;

        let elements = [];
        if (containerSelector && selector) {
            let containers = [];
            try { containers = Array.from(document.querySelectorAll(containerSelector)); } catch (e) {}
            containers.forEach(c => {
                const children = ScrapeUtils.queryInContainer(c, containerSelector, selector);
                children.forEach(ch => elements.push(ch));
            });
        } else if (selector) {
            elements = ScrapeUtils.queryFieldOnDocument(selector, matchAll);
        }

        if (!multiHighlightContainer || !multiHighlightContainer.isConnected) {
            multiHighlightContainer = document.createElement('div');
            multiHighlightContainer.id = '__baf_multi_highlight_container';
            multiHighlightContainer.style.position = 'fixed';
            multiHighlightContainer.style.top = '0';
            multiHighlightContainer.style.left = '0';
            multiHighlightContainer.style.width = '0';
            multiHighlightContainer.style.height = '0';
            multiHighlightContainer.style.pointerEvents = 'none';
            multiHighlightContainer.style.zIndex = '2147483646';
            (document.documentElement || document.body).appendChild(multiHighlightContainer);
        }

        multiHighlightContainer.innerHTML = '';
        multiHighlightContainer.style.display = 'block';

        elements.forEach(el => {
            if (!el || !el.getBoundingClientRect) return;
            const rect = el.getBoundingClientRect();
            if (rect.width === 0 && rect.height === 0) return;

            const box = document.createElement('div');
            box.style.position = 'fixed';
            box.style.top = rect.top + 'px';
            box.style.left = rect.left + 'px';
            box.style.width = rect.width + 'px';
            box.style.height = rect.height + 'px';
            box.style.backgroundColor = 'rgba(0, 220, 100, 0.25)';
            box.style.border = '2px solid rgba(0, 220, 100, 0.9)';
            box.style.boxSizing = 'border-box';
            box.style.pointerEvents = 'none';
            multiHighlightContainer.appendChild(box);
        });

        multiHighlightTimer = setTimeout(clearMultiHighlights, 4000);
        return { count: elements.length };
    });

    registerAction('SCRAPE_DATA', async (payload) => {
        const containerSelector = (payload.containerSelector || '').trim();
        const fields = Array.isArray(payload.fields) ? payload.fields : [];
        const records = [];

        if (fields.length === 0) {
            return { records: [] };
        }

        if (containerSelector) {
            let containers = [];
            try { containers = Array.from(document.querySelectorAll(containerSelector)); } catch (e) {}
            for (const containerEl of containers) {
                const record = {};
                for (const field of fields) {
                    const matched = ScrapeUtils.queryInContainer(containerEl, containerSelector, field.selector);
                    const extType = String(field.extractionType ?? '').toLowerCase();
                    if (field.matchAllElements && matched.length > 1 && extType !== 'count' && extType !== '11' && extType !== 'exists' && extType !== '10') {
                        record[field.name] = matched
                            .map(m => ScrapeUtils.extractValue(m, field, matched.length))
                            .filter(v => v !== '')
                            .join('; ');
                    } else {
                        record[field.name] = ScrapeUtils.extractValue(matched[0] || null, field, matched.length);
                    }
                }
                records.push(record);
            }
        } else {
            const fieldMatches = fields.map(f => ({
                field: f,
                elements: ScrapeUtils.queryFieldOnDocument(f.selector, Boolean(f.matchAllElements))
            }));

            const hasMultiField = fieldMatches.some(fm => fm.field.matchAllElements);
            if (!hasMultiField) {
                const record = {};
                for (const fm of fieldMatches) {
                    record[fm.field.name] = ScrapeUtils.extractValue(fm.elements[0] || null, fm.field, fm.elements.length);
                }
                records.push(record);
            } else {
                let maxRows = 0;
                for (const fm of fieldMatches) {
                    if (fm.field.matchAllElements && fm.elements.length > maxRows) {
                        maxRows = fm.elements.length;
                    }
                }
                for (let rowIdx = 0; rowIdx < maxRows; rowIdx++) {
                    const record = {};
                    for (const fm of fieldMatches) {
                        const targetEl = fm.field.matchAllElements
                            ? (fm.elements[rowIdx] || null)
                            : (fm.elements[0] || null);
                        record[fm.field.name] = ScrapeUtils.extractValue(targetEl, fm.field, fm.elements.length);
                    }
                    records.push(record);
                }
            }
        }

        return { records: records };
    });

    registerAction('SCRAPE_TABLE', async (payload) => {
        const selector = (payload && payload.selector) ? payload.selector : 'table';
        const table = document.querySelector(selector);
        if (!table) throw new Error('Table not found: ' + selector);

        const headers = [];
        const headerCells = table.querySelectorAll('thead th, thead td');
        if (headerCells.length > 0) {
            headerCells.forEach((cell, idx) => {
                const text = (cell.innerText || cell.textContent || '').trim();
                headers.push(text || ('Column_' + (idx + 1)));
            });
        }

        const rows = Array.from(table.querySelectorAll('tbody tr'));
        const targetRows = rows.length > 0 ? rows : Array.from(table.querySelectorAll('tr'));
        const records = [];

        targetRows.forEach((tr, rowIdx) => {
            if (headerCells.length === 0 && rowIdx === 0 && tr.querySelectorAll('th').length > 0) {
                tr.querySelectorAll('th').forEach((th, idx) => {
                    const text = (th.innerText || th.textContent || '').trim();
                    headers.push(text || ('Column_' + (idx + 1)));
                });
                return;
            }
            const cells = Array.from(tr.querySelectorAll('td, th'));
            if (cells.length === 0) return;

            const record = {};
            cells.forEach((cell, colIdx) => {
                const colName = headers[colIdx] || ('Column_' + (colIdx + 1));
                record[colName] = (cell.innerText || cell.textContent || '').trim();
            });
            records.push(record);
        });

        return { records: records };
    });

    // --- Reverse Engineering & Site Dump Utilities ---
    const ReverseEngineeringUtils = {
        getByteSize: function(str) {
            try {
                return new Blob([str || '']).size;
            } catch (e) {
                return (str || '').length;
            }
        },
        deriveResourceName: function(rawUrl, fallbackPrefix, index, defaultExt) {
            try {
                if (rawUrl && !rawUrl.startsWith('data:') && !rawUrl.startsWith('blob:')) {
                    const parsed = new URL(rawUrl, window.location.href);
                    const pathname = parsed.pathname || '';
                    const segments = pathname.split('/').filter(Boolean);
                    if (segments.length > 0) {
                        let lastSeg = decodeURIComponent(segments[segments.length - 1]).replace(/[^a-zA-Z0-9._-]/g, '_');
                        if (lastSeg && lastSeg !== '/') {
                            if (!lastSeg.toLowerCase().endsWith('.' + defaultExt.toLowerCase())) {
                                lastSeg += '.' + defaultExt;
                            }
                            return lastSeg;
                        }
                    }
                }
            } catch (e) {}
            return `${fallbackPrefix}-${index}.${defaultExt}`;
        },
        fetchAssetText: function(url, timeoutMs = 15000) {
            return new Promise((resolve) => {
                const reqFn = (typeof GM_xmlhttpRequest === 'function')
                    ? GM_xmlhttpRequest
                    : (typeof GM !== 'undefined' && typeof GM.xmlHttpRequest === 'function' ? GM.xmlHttpRequest.bind(GM) : null);

                const tryNativeFetch = async (fallbackReason) => {
                    try {
                        const res = await fetch(url, { credentials: 'include', cache: 'force-cache' });
                        if (res.ok) {
                            const txt = await res.text();
                            resolve({ content: txt, error: null });
                            return;
                        }
                    } catch (e) {}

                    try {
                        const resOmit = await fetch(url, { credentials: 'omit', cache: 'force-cache' });
                        if (resOmit.ok) {
                            const txt = await resOmit.text();
                            resolve({ content: txt, error: null });
                            return;
                        }
                        resolve({ content: '', error: `HTTP ${resOmit.status}` });
                    } catch (err) {
                        resolve({ content: '', error: fallbackReason || (err && err.message ? err.message : 'Failed to fetch asset') });
                    }
                };

                if (!reqFn) {
                    tryNativeFetch();
                    return;
                }

                try {
                    reqFn({
                        method: 'GET',
                        url: url,
                        timeout: timeoutMs,
                        onload: (res) => {
                            if (res.status >= 200 && res.status < 400 && typeof res.responseText === 'string') {
                                resolve({ content: res.responseText, error: null });
                            } else {
                                tryNativeFetch(`HTTP ${res.status}`);
                            }
                        },
                        onerror: () => tryNativeFetch('GM_xmlhttpRequest network error'),
                        ontimeout: () => tryNativeFetch('GM_xmlhttpRequest timeout'),
                        onabort: () => tryNativeFetch('GM_xmlhttpRequest aborted')
                    });
                } catch (e) {
                    tryNativeFetch(e && e.message ? e.message : 'Request exception');
                }
            });
        },
        serializeCssRules: function(sheet) {
            try {
                if (!sheet || !sheet.cssRules) return '';
                return Array.from(sheet.cssRules).map(r => r.cssText).join('\n');
            } catch (e) {
                return '';
            }
        },
        getCleanDocumentHtml: function(doc) {
            const targetDoc = doc || document;
            let doctypeStr = '';
            try {
                if (targetDoc.doctype) {
                    const dt = targetDoc.doctype;
                    doctypeStr = '<!DOCTYPE ' + dt.name +
                        (dt.publicId ? ' PUBLIC "' + dt.publicId + '"' : '') +
                        (!dt.publicId && dt.systemId ? ' SYSTEM' : '') +
                        (dt.systemId ? ' "' + dt.systemId + '"' : '') + '>\n';
                }
            } catch (e) {}

            if (!targetDoc.documentElement) return doctypeStr;

            const clone = targetDoc.documentElement.cloneNode(true);
            try {
                const artifacts = clone.querySelectorAll(
                    '#__baf_highlight_overlay, #__baf_multi_highlight_container, #__baf_ratelimit_banner, script[id^="eval_"][id$="_script"]'
                );
                artifacts.forEach(node => node.remove());
            } catch (e) {}

            return doctypeStr + clone.outerHTML;
        },
        dumpHtmlResources: async function(options) {
            const htmlResources = [];
            const mainHtml = this.getCleanDocumentHtml(document);
            htmlResources.push({
                index: 1,
                name: this.deriveResourceName(window.location.href, 'document', 1, 'html'),
                category: 'Html',
                sourceType: 'Document',
                url: window.location.href,
                content: mainHtml,
                sizeBytes: this.getByteSize(mainHtml),
                error: null
            });

            if (!options || options.includeIframes !== false) {
                const iframes = Array.from(document.querySelectorAll('iframe'));
                for (let i = 0; i < iframes.length; i++) {
                    const frame = iframes[i];
                    try {
                        const frameDoc = frame.contentDocument || (frame.contentWindow && frame.contentWindow.document);
                        if (frameDoc && frameDoc.documentElement) {
                            const frameHtml = this.getCleanDocumentHtml(frameDoc);
                            if (frameHtml && frameHtml.trim().length > 0) {
                                const frameUrl = frame.src || (frameDoc.location ? frameDoc.location.href : window.location.href);
                                const idx = htmlResources.length + 1;
                                htmlResources.push({
                                    index: idx,
                                    name: this.deriveResourceName(frameUrl, 'iframe', i + 1, 'html'),
                                    category: 'Html',
                                    sourceType: 'Iframe',
                                    url: frameUrl,
                                    content: frameHtml,
                                    sizeBytes: this.getByteSize(frameHtml),
                                    error: null
                                });
                            }
                        }
                    } catch (e) {
                        // Cross-origin iframe; try fetching src if available
                        if (frame.src && frame.src.startsWith('http')) {
                            const fetched = await this.fetchAssetText(frame.src, (options && options.fetchTimeoutMs) || 15000);
                            if (fetched.content) {
                                const idx = htmlResources.length + 1;
                                htmlResources.push({
                                    index: idx,
                                    name: this.deriveResourceName(frame.src, 'iframe', i + 1, 'html'),
                                    category: 'Html',
                                    sourceType: 'Iframe',
                                    url: frame.src,
                                    content: fetched.content,
                                    sizeBytes: this.getByteSize(fetched.content),
                                    error: fetched.error
                                });
                            }
                        }
                    }
                }
            }

            return { mainHtml, htmlResources };
        },
        dumpCssResources: async function(options) {
            const cssResources = [];
            const seenHrefs = new Set();
            const seenSheets = new Set();
            const timeoutMs = (options && options.fetchTimeoutMs) || 15000;
            const includeExternal = !options || options.includeExternalCss !== false;
            const includeInline = !options || options.includeInlineCss !== false;
            const includeConstructed = !options || options.includeConstructedCss !== false;

            let inlineCounter = 0;
            const styleNodes = Array.from(document.querySelectorAll('link[rel~="stylesheet" i], style'));

            for (const node of styleNodes) {
                const tag = (node.tagName || '').toLowerCase();
                if (tag === 'link') {
                    if (!includeExternal) continue;
                    const href = node.href || node.getAttribute('href') || '';
                    if (!href) continue;

                    let resolvedUrl = href;
                    try { resolvedUrl = new URL(href, window.location.href).href; } catch (e) {}
                    if (seenHrefs.has(resolvedUrl)) continue;
                    seenHrefs.add(resolvedUrl);
                    if (node.sheet) seenSheets.add(node.sheet);

                    const fetched = await this.fetchAssetText(resolvedUrl, timeoutMs);
                    let content = fetched.content;
                    let error = fetched.error;

                    if (!content && node.sheet) {
                        const fallbackCss = this.serializeCssRules(node.sheet);
                        if (fallbackCss) {
                            content = fallbackCss;
                            error = null;
                        }
                    }

                    const idx = cssResources.length + 1;
                    cssResources.push({
                        index: idx,
                        name: this.deriveResourceName(resolvedUrl, 'stylesheet', idx, 'css'),
                        category: 'Css',
                        sourceType: 'External',
                        url: resolvedUrl,
                        content: content || '',
                        sizeBytes: this.getByteSize(content || ''),
                        error: error
                    });
                } else if (tag === 'style') {
                    if (!includeInline) continue;
                    if (node.sheet) seenSheets.add(node.sheet);

                    let cssText = (node.textContent || '').trim();
                    let sourceType = 'Inline';
                    if (!cssText && node.sheet) {
                        cssText = this.serializeCssRules(node.sheet);
                        if (cssText) sourceType = 'Constructed';
                    }

                    if (!cssText) continue;
                    inlineCounter++;
                    const idx = cssResources.length + 1;
                    cssResources.push({
                        index: idx,
                        name: `inline-style-${inlineCounter}.css`,
                        category: 'Css',
                        sourceType: sourceType,
                        url: window.location.href,
                        content: cssText,
                        sizeBytes: this.getByteSize(cssText),
                        error: null
                    });
                }
            }

            // Capture any additional stylesheets in document.styleSheets not represented above
            if (document.styleSheets) {
                for (let i = 0; i < document.styleSheets.length; i++) {
                    const sheet = document.styleSheets[i];
                    if (seenSheets.has(sheet)) continue;
                    seenSheets.add(sheet);

                    if (sheet.href) {
                        if (!includeExternal) continue;
                        if (seenHrefs.has(sheet.href)) continue;
                        seenHrefs.add(sheet.href);

                        const fetched = await this.fetchAssetText(sheet.href, timeoutMs);
                        let content = fetched.content || this.serializeCssRules(sheet);
                        const idx = cssResources.length + 1;
                        cssResources.push({
                            index: idx,
                            name: this.deriveResourceName(sheet.href, 'stylesheet', idx, 'css'),
                            category: 'Css',
                            sourceType: 'External',
                            url: sheet.href,
                            content: content || '',
                            sizeBytes: this.getByteSize(content || ''),
                            error: content ? null : fetched.error
                        });
                    } else if (includeConstructed) {
                        const rulesText = this.serializeCssRules(sheet);
                        if (rulesText) {
                            inlineCounter++;
                            const idx = cssResources.length + 1;
                            cssResources.push({
                                index: idx,
                                name: `constructed-style-${inlineCounter}.css`,
                                category: 'Css',
                                sourceType: 'Constructed',
                                url: window.location.href,
                                content: rulesText,
                                sizeBytes: this.getByteSize(rulesText),
                                error: null
                            });
                        }
                    }
                }
            }

            // Capture adoptedStyleSheets if present
            if (includeConstructed && Array.isArray(document.adoptedStyleSheets)) {
                for (let i = 0; i < document.adoptedStyleSheets.length; i++) {
                    const sheet = document.adoptedStyleSheets[i];
                    if (seenSheets.has(sheet)) continue;
                    const rulesText = this.serializeCssRules(sheet);
                    if (rulesText) {
                        const idx = cssResources.length + 1;
                        cssResources.push({
                            index: idx,
                            name: `adopted-style-${i + 1}.css`,
                            category: 'Css',
                            sourceType: 'Constructed',
                            url: window.location.href,
                            content: rulesText,
                            sizeBytes: this.getByteSize(rulesText),
                            error: null
                        });
                    }
                }
            }

            return cssResources;
        },
        dumpJsResources: async function(options) {
            const jsResources = [];
            const seenUrls = new Set();
            const timeoutMs = (options && options.fetchTimeoutMs) || 15000;
            const includeExternal = !options || options.includeExternalScripts !== false;
            const includeInline = !options || options.includeInlineScripts !== false;
            const includePerfResources = !options || options.includePerformanceResourceScripts !== false;

            let inlineCounter = 0;
            const scriptElements = Array.from(document.querySelectorAll('script'));

            for (const script of scriptElements) {
                if (script.id && typeof script.id === 'string' && script.id.startsWith('eval_') && script.id.endsWith('_script')) {
                    continue;
                }

                const rawSrc = script.src || script.getAttribute('src') || '';
                if (rawSrc) {
                    if (!includeExternal) continue;
                    let resolvedUrl = rawSrc;
                    try { resolvedUrl = new URL(rawSrc, window.location.href).href; } catch (e) {}
                    if (seenUrls.has(resolvedUrl)) continue;
                    seenUrls.add(resolvedUrl);

                    const fetched = await this.fetchAssetText(resolvedUrl, timeoutMs);
                    const idx = jsResources.length + 1;
                    jsResources.push({
                        index: idx,
                        name: this.deriveResourceName(resolvedUrl, 'script', idx, 'js'),
                        category: 'JavaScript',
                        sourceType: 'External',
                        url: resolvedUrl,
                        content: fetched.content || '',
                        sizeBytes: this.getByteSize(fetched.content || ''),
                        error: fetched.error
                    });
                } else if (includeInline) {
                    const code = (script.textContent || '').trim();
                    if (!code) continue;
                    inlineCounter++;
                    const scriptType = (script.getAttribute('type') || '').trim();
                    const ext = scriptType.includes('json') ? 'json' : 'js';
                    const idx = jsResources.length + 1;
                    jsResources.push({
                        index: idx,
                        name: `inline-script-${inlineCounter}.${ext}`,
                        category: 'JavaScript',
                        sourceType: scriptType ? `Inline (${scriptType})` : 'Inline',
                        url: window.location.href,
                        content: code,
                        sizeBytes: this.getByteSize(code),
                        error: null
                    });
                }
            }

            // Check performance resource timing entries for dynamically loaded scripts
            if (includePerfResources && typeof performance !== 'undefined' && typeof performance.getEntriesByType === 'function') {
                try {
                    const entries = performance.getEntriesByType('resource') || [];
                    for (const entry of entries) {
                        const resUrl = entry.name || '';
                        if (!resUrl.startsWith('http://') && !resUrl.startsWith('https://')) continue;
                        if (resUrl.includes('127.0.0.1:' + PORT) || resUrl.includes('localhost:' + PORT)) continue;

                        const isScriptInitiator = entry.initiatorType === 'script';
                        const isJsExtension = /\.(m?js)(\?|$)/i.test(resUrl);
                        if (!isScriptInitiator && !isJsExtension) continue;
                        if (seenUrls.has(resUrl)) continue;
                        seenUrls.add(resUrl);

                        const fetched = await this.fetchAssetText(resUrl, timeoutMs);
                        const idx = jsResources.length + 1;
                        jsResources.push({
                            index: idx,
                            name: this.deriveResourceName(resUrl, 'dynamic-script', idx, 'js'),
                            category: 'JavaScript',
                            sourceType: 'DynamicResource',
                            url: resUrl,
                            content: fetched.content || '',
                            sizeBytes: this.getByteSize(fetched.content || ''),
                            error: fetched.error
                        });
                    }
                } catch (e) {}
            }

            return jsResources;
        },
        dumpSite: async function(options = {}) {
            const includeHtml = options.includeHtml !== false;
            const includeCss = options.includeCss !== false;
            const includeJavaScript = options.includeJavaScript !== false;

            let mainHtml = '';
            let htmlResources = [];
            let cssResources = [];
            let javascriptResources = [];

            if (includeHtml) {
                const htmlRes = await this.dumpHtmlResources(options);
                mainHtml = htmlRes.mainHtml;
                htmlResources = htmlRes.htmlResources;
            }

            if (includeCss) {
                cssResources = await this.dumpCssResources(options);
            }

            if (includeJavaScript) {
                javascriptResources = await this.dumpJsResources(options);
            }

            return {
                pageUrl: window.location.href,
                pageTitle: document.title || '',
                html: mainHtml,
                htmlResources: htmlResources,
                cssResources: cssResources,
                javascriptResources: javascriptResources
            };
        }
    };

    registerAction('DUMP_HTML', async (payload) => {
        const opts = Object.assign({}, payload || {}, { includeHtml: true, includeCss: false, includeJavaScript: false });
        return await ReverseEngineeringUtils.dumpSite(opts);
    });

    registerAction('DUMP_CSS', async (payload) => {
        const opts = Object.assign({}, payload || {}, { includeHtml: false, includeCss: true, includeJavaScript: false });
        return await ReverseEngineeringUtils.dumpSite(opts);
    });

    registerAction('DUMP_JS', async (payload) => {
        const opts = Object.assign({}, payload || {}, { includeHtml: false, includeCss: false, includeJavaScript: true });
        return await ReverseEngineeringUtils.dumpSite(opts);
    });

    registerAction('DUMP_SITE', async (payload) => {
        return await ReverseEngineeringUtils.dumpSite(payload || {});
    });


    // --- Unified IPC Engine (WebSocket + CSP-Safe GM_xmlhttpRequest Transport) ---
    const API_BASE = `http://127.0.0.1:${PORT}/api`;
    const PREFERRED_MODE = (EXT_CFG && EXT_CFG.mode) ? String(EXT_CFG.mode) : 'ws';
    const HTTP_POLL_INTERVAL_MS = 150;
    let activeTransport = PREFERRED_MODE;
    let ws = null;
    let wsBlockedByCsp = false;
    let httpPollingActive = false;
    let httpServerConnected = false;

    function tmFetch(url, options = {}) {
        return new Promise((resolve, reject) => {
            const reqFn = (typeof GM_xmlhttpRequest === 'function')
                ? GM_xmlhttpRequest
                : (typeof GM !== 'undefined' && typeof GM.xmlHttpRequest === 'function' ? GM.xmlHttpRequest.bind(GM) : null);

            if (!reqFn) {
                fetch(url, {
                    method: options.method || 'GET',
                    body: options.body,
                    headers: { 'Content-Type': 'application/json' }
                })
                .then(r => r.text())
                .then(t => {
                    try { resolve(JSON.parse(t || '{}')); }
                    catch (err) { resolve({}); }
                })
                .catch(reject);
                return;
            }

            reqFn({
                method: options.method || 'GET',
                url: url,
                data: options.body,
                headers: { 'Content-Type': 'application/json' },
                timeout: options.timeout || 10000,
                onload: (res) => {
                    if (res.status >= 200 && res.status < 300) {
                        try {
                            resolve(JSON.parse(res.responseText || '{}'));
                        } catch (err) {
                            resolve({});
                        }
                    } else {
                        reject(new Error('HTTP ' + res.status));
                    }
                },
                onerror: (err) => reject(err || new Error('Network error')),
                ontimeout: () => reject(new Error('Request timeout')),
                onabort: () => reject(new Error('Request aborted'))
            });
        });
    }

    async function sendHttpResult(taskId, status, outStr) {
        try {
            await tmFetch(`${API_BASE}/result/${INSTANCE_ID}`, {
                method: 'POST',
                body: JSON.stringify({ task_id: taskId, status: status, output: outStr }),
                timeout: 30000
            });
        } catch (e) {}
    }

    async function sendHttpEvent(eventName, payload) {
        try {
            await tmFetch(`${API_BASE}/event/${INSTANCE_ID}`, {
                method: 'POST',
                body: JSON.stringify({ event: eventName, payload: payload })
            });
        } catch (e) {}
    }

    async function sendResult(taskId, status, output) {
        if (isDisposed) return;
        const outStr = typeof output === 'object' ? JSON.stringify(output) : String(output);
        if (activeTransport === 'ws' && ws && ws.readyState === WebSocket.OPEN) {
            try {
                ws.send(JSON.stringify({ task_id: taskId, status: status, output: outStr }));
                return;
            } catch (e) {}
        }
        await sendHttpResult(taskId, status, outStr);
    }

    async function sendEvent(eventName, payload) {
        if (isDisposed) return;
        if (activeTransport === 'ws' && ws && ws.readyState === WebSocket.OPEN) {
            try {
                ws.send(JSON.stringify({ event: eventName, payload: payload }));
                return;
            } catch (e) {}
        }
        if (activeTransport === 'http') {
            await sendHttpEvent(eventName, payload);
        }
    }

    window.streamData = async function(taskId, data) {
        await sendResult(taskId, 'streaming', data);
    };

    async function announcePageReady() {
        const state = await waitForDocumentComplete(30000);
        await sendEvent('PAGE_LOADED', state);
        const currentHighlightedPath = (highlightedElement && highlightedElement.isConnected)
            ? getElementPath(highlightedElement)
            : null;
        await sendEvent('DOM_CHANGED', { highlightedPath: currentHighlightedPath });
    }

    function markWebSocketBlockedByCsp() {
        wsBlockedByCsp = true;
        try { sessionStorage.setItem('__baf_ws_csp_blocked', '1'); } catch (e) {}
        try { localStorage.setItem('__baf_ws_csp_blocked', '1'); } catch (e) {}
    }

    function isWebSocketLikelyBlockedByCsp() {
        if (wsBlockedByCsp) return true;
        try {
            if (sessionStorage.getItem('__baf_ws_csp_blocked') === '1' ||
                localStorage.getItem('__baf_ws_csp_blocked') === '1') {
                wsBlockedByCsp = true;
                return true;
            }
            const metaTags = document.querySelectorAll('meta[http-equiv="Content-Security-Policy" i]');
            for (const meta of metaTags) {
                const content = (meta.getAttribute('content') || '').toLowerCase();
                if (content.includes('connect-src') || content.includes('default-src')) {
                    if (!content.includes('127.0.0.1') && !content.includes('localhost') && !content.includes('ws:') && !content.includes('connect-src *')) {
                        markWebSocketBlockedByCsp();
                        return true;
                    }
                }
            }
        } catch (e) {}
        return false;
    }

    function startHttpPolling(reason) {
        if (isDisposed) return;
        activeTransport = 'http';
        if (httpPollingActive) return;
        httpPollingActive = true;
        if (reason) {
            console.log('[Automation Bridge] Using HTTP GM_xmlhttpRequest IPC (' + reason + ')');
        }
        pollServer();
    }

    async function pollServer() {
        if (isDisposed || activeTransport !== 'http') {
            httpPollingActive = false;
            return;
        }
        let hadCommand = false;
        try {
            const data = await tmFetch(`${API_BASE}/command/${INSTANCE_ID}`);
            if (!httpServerConnected) {
                httpServerConnected = true;
                announcePageReady();
            }
            if (data && data.command) {
                hadCommand = true;
                const { id, type, payload } = data.command;
                const handler = actionHandlers[type];

                if (handler) {
                    try {
                        const result = await handler(payload);
                        await sendResult(id, 'completed', result);
                    } catch (err) {
                        await sendResult(id, 'error', err && err.message ? err.message : String(err));
                    }
                } else {
                    await sendResult(id, 'error', 'Unknown command: ' + type);
                }
            }
        } catch (e) {
            httpServerConnected = false;
        } finally {
            if (!isDisposed && activeTransport === 'http') {
                setTimeout(pollServer, hadCommand ? 10 : HTTP_POLL_INTERVAL_MS);
            } else {
                httpPollingActive = false;
            }
        }
    }

    const onCspViolation = (e) => {
        const directive = ((e && (e.effectiveDirective || e.violatedDirective)) || '').toLowerCase();
        const blockedUri = ((e && e.blockedURI) || '').toLowerCase();
        if (directive.startsWith('connect-src') || directive.startsWith('default-src') ||
            blockedUri.includes('127.0.0.1') || blockedUri.includes('localhost') || blockedUri.startsWith('ws')) {
            if (PREFERRED_MODE === 'ws' && activeTransport !== 'http') {
                markWebSocketBlockedByCsp();
                if (ws) {
                    try { ws.onclose = null; ws.onerror = null; ws.close(); } catch (err) {}
                    ws = null;
                }
                startHttpPolling('WebSocket blocked by page Content-Security-Policy');
            }
        }
    };
    window.addEventListener('securitypolicyviolation', onCspViolation, true);
    document.addEventListener('securitypolicyviolation', onCspViolation, true);

    function connectWebSocket() {
        if (isDisposed) return;
        if (activeTransport === 'http' || isWebSocketLikelyBlockedByCsp()) {
            startHttpPolling('CSP fallback');
            return;
        }

        let wsOpened = false;
        try {
            ws = new WebSocket(`ws://127.0.0.1:${PORT}/?instanceId=${INSTANCE_ID}`);
        } catch (err) {
            markWebSocketBlockedByCsp();
            startHttpPolling('WebSocket constructor blocked: ' + (err.message || err));
            return;
        }

        ws.onopen = async () => {
            wsOpened = true;
            activeTransport = 'ws';
            console.log('[Automation Bridge] WebSocket connected');
            await announcePageReady();
        };

        ws.onmessage = async (event) => {
            const cmd = JSON.parse(event.data);
            const { id, type, payload } = cmd;
            const handler = actionHandlers[type];

            if (handler) {
                try {
                    const result = await handler(payload);
                    await sendResult(id, 'completed', result);
                } catch (err) {
                    await sendResult(id, 'error', err && err.message ? err.message : String(err));
                }
            } else {
                await sendResult(id, 'error', 'Unknown command: ' + type);
            }
        };

        ws.onclose = async () => {
            if (isDisposed) return;
            if (activeTransport === 'http' || isWebSocketLikelyBlockedByCsp()) {
                startHttpPolling('CSP fallback');
                return;
            }
            if (!wsOpened) {
                try {
                    await tmFetch(`${API_BASE}/event/${INSTANCE_ID}`, {
                        method: 'POST',
                        body: JSON.stringify({ event: 'PING', payload: null }),
                        timeout: 2500
                    });
                    markWebSocketBlockedByCsp();
                    startHttpPolling('WebSocket blocked while HTTP server is reachable');
                    return;
                } catch (probeErr) {
                    // Server offline; retry WebSocket
                }
            }
            if (!isDisposed) {
                console.log('[Automation Bridge] WebSocket disconnected, reconnecting...');
                setTimeout(connectWebSocket, 2000);
            }
        };
    }

    if (PREFERRED_MODE === 'ws' && !isWebSocketLikelyBlockedByCsp()) {
        connectWebSocket();
    } else {
        startHttpPolling(PREFERRED_MODE === 'ws' ? 'Cached CSP fallback' : null);
    }


    // --- Browser Event Sync ---
    let lastMiddleDownTime = 0;

    const handleMiddleMouseSelection = (e) => {
        if (typeof sendEvent === 'function') {
            const target = e.target && e.target.nodeType === Node.ELEMENT_NODE ? e.target : (e.target ? e.target.parentElement : null);
            const path = getElementPath(target);
            if (path) {
                highlightedElement = target;
                lastHighlightRect = { top: null, left: null, width: null, height: null, display: null };
                updateHighlightPosition();
                startHighlightTracking();
                sendEvent('ELEMENT_CLICKED', { path: path });
            }
        }
    };

    const onMiddleMouseDown = (e) => {
        if (e.isTrusted && e.button === 1) {
            e.preventDefault();
            e.stopPropagation();
            const now = Date.now();
            if (now - lastMiddleDownTime > 50) {
                lastMiddleDownTime = now;
                handleMiddleMouseSelection(e);
            }
        }
    };

    const onMiddleMouseUp = (e) => {
        if (e.isTrusted && e.button === 1) {
            e.preventDefault();
            e.stopPropagation();
        }
    };

    const onMiddleAuxClick = (e) => {
        if (e.isTrusted && e.button === 1) {
            e.preventDefault();
            e.stopPropagation();
            const now = Date.now();
            if (now - lastMiddleDownTime > 1000) {
                lastMiddleDownTime = now;
                handleMiddleMouseSelection(e);
            }
        }
    };

    const onMiddleClick = (e) => {
        if (e.isTrusted && e.button === 1) {
            e.preventDefault();
            e.stopPropagation();
        }
    };

    [window, document].forEach(target => {
        target.addEventListener('mousedown', onMiddleMouseDown, { capture: true, passive: false });
        target.addEventListener('mouseup', onMiddleMouseUp, { capture: true, passive: false });
        target.addEventListener('auxclick', onMiddleAuxClick, { capture: true, passive: false });
        target.addEventListener('click', onMiddleClick, { capture: true, passive: false });
    });

    window.__baf_bridge = {
        active: true,
        instanceId: INSTANCE_ID,
        port: PORT,
        reannounce: () => {
            if (!isDisposed) announcePageReady();
        },
        dispose: () => {
            if (isDisposed) return;
            isDisposed = true;
            window.__baf_bridge.active = false;
            activeTransport = 'disposed';
            clearTimeout(domTimeout);
            try { observer.disconnect(); } catch (e) {}
            try { clearHighlight(false); } catch (e) {}
            try { clearRateLimitBanner(); } catch (e) {}
            try {
                if (highlightOverlay && highlightOverlay.isConnected) highlightOverlay.remove();
                if (multiHighlightContainer && multiHighlightContainer.isConnected) multiHighlightContainer.remove();
            } catch (e) {}
            window.removeEventListener('scroll', updateHighlightPosition, { capture: true });
            window.removeEventListener('resize', updateHighlightPosition, { capture: true });
            window.removeEventListener('securitypolicyviolation', onCspViolation, true);
            document.removeEventListener('securitypolicyviolation', onCspViolation, true);
            [window, document].forEach(target => {
                target.removeEventListener('mousedown', onMiddleMouseDown, { capture: true });
                target.removeEventListener('mouseup', onMiddleMouseUp, { capture: true });
                target.removeEventListener('auxclick', onMiddleAuxClick, { capture: true });
                target.removeEventListener('click', onMiddleClick, { capture: true });
            });
            if (ws) {
                try {
                    ws.onclose = null;
                    ws.onerror = null;
                    ws.onmessage = null;
                    ws.close();
                } catch (e) {}
                ws = null;
            }
        }
    };

})();
