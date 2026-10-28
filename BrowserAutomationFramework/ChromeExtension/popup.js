'use strict';

const statusBadge = document.getElementById('statusBadge');
const portInput = document.getElementById('portInput');
const btnSavePort = document.getElementById('btnSavePort');
const currentTabTitle = document.getElementById('currentTabTitle');
const btnAttachCurrent = document.getElementById('btnAttachCurrent');
const btnDetachCurrent = document.getElementById('btnDetachCurrent');
const tabList = document.getElementById('tabList');

let activeFocusedTab = null;

function renderState(state) {
    if (!state) return;
    portInput.value = state.port || 9999;

    if (state.connected) {
        statusBadge.textContent = 'Connected';
        statusBadge.className = 'badge badge-connected';
    } else {
        statusBadge.textContent = 'Server Offline';
        statusBadge.className = 'badge badge-disconnected';
    }

    const tabs = Array.isArray(state.tabs) ? state.tabs : [];
    activeFocusedTab = tabs.find(t => t.active) || tabs[0] || null;

    if (activeFocusedTab) {
        currentTabTitle.textContent = `[#${activeFocusedTab.tabId}] ${activeFocusedTab.title}`;
        btnAttachCurrent.disabled = !activeFocusedTab.canInject;
        btnAttachCurrent.textContent = activeFocusedTab.isInjected ? 'Re-Sync This Tab' : 'Inject & Switch Here';
        btnDetachCurrent.style.display = activeFocusedTab.isInjected ? 'inline-block' : 'none';
    } else {
        currentTabTitle.textContent = '(No active tab)';
        btnAttachCurrent.disabled = true;
    }

    tabList.innerHTML = '';
    tabs.forEach(tab => {
        const row = document.createElement('div');
        row.className = 'tab-item' + (tab.isInjected ? ' injected' : '');

        const info = document.createElement('div');
        info.className = 'tab-info';

        const titleEl = document.createElement('div');
        titleEl.className = 'tab-title';
        titleEl.textContent = (tab.isInjected ? '[HOOKED] ' : '') + `[#${tab.tabId}] ${tab.title}`;

        const urlEl = document.createElement('div');
        urlEl.className = 'tab-url';
        urlEl.textContent = tab.url;

        info.appendChild(titleEl);
        info.appendChild(urlEl);

        const actionBtn = document.createElement('button');
        if (!tab.canInject) {
            actionBtn.textContent = 'Restricted';
            actionBtn.disabled = true;
            actionBtn.className = 'btn-secondary';
        } else if (tab.isInjected) {
            actionBtn.textContent = 'Detach';
            actionBtn.className = 'btn-danger';
            actionBtn.onclick = () => {
                chrome.runtime.sendMessage({ type: 'POPUP_DETACH_TAB', tabId: tab.tabId }, () => refreshPopup());
            };
        } else {
            actionBtn.textContent = 'Switch';
            actionBtn.onclick = () => {
                chrome.runtime.sendMessage({ type: 'POPUP_ATTACH_TAB', tabId: tab.tabId, exclusive: true }, () => refreshPopup());
            };
        }

        row.appendChild(info);
        row.appendChild(actionBtn);
        tabList.appendChild(row);
    });
}

function refreshPopup() {
    chrome.runtime.sendMessage({ type: 'POPUP_GET_STATE' }, (state) => {
        renderState(state);
    });
}

btnSavePort.addEventListener('click', () => {
    const p = Number(portInput.value);
    chrome.runtime.sendMessage({ type: 'POPUP_SET_PORT', port: p }, () => {
        setTimeout(refreshPopup, 250);
    });
});

btnAttachCurrent.addEventListener('click', () => {
    if (!activeFocusedTab) return;
    chrome.runtime.sendMessage({ type: 'POPUP_ATTACH_TAB', tabId: activeFocusedTab.tabId, exclusive: true }, () => {
        refreshPopup();
    });
});

btnDetachCurrent.addEventListener('click', () => {
    if (!activeFocusedTab) return;
    chrome.runtime.sendMessage({ type: 'POPUP_DETACH_TAB', tabId: activeFocusedTab.tabId }, () => {
        refreshPopup();
    });
});

refreshPopup();
