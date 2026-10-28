// BAF Content Script Relay (ISOLATED World)
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
