namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Specifies the IPC transport protocol used between the browser automation script and <see cref="Server.AutomationServer"/>.
    /// </summary>
    public enum IpcMode
    {
        /// <summary>
        /// Communicates via HTTP polling (<c>GM_xmlhttpRequest</c> / <c>fetch</c>), bypassing strict page Content-Security-Policy rules.
        /// </summary>
        HttpPolling,

        /// <summary>
        /// Communicates via full-duplex WebSockets with automatic fallback to <see cref="HttpPolling"/> if blocked by CSP.
        /// </summary>
        WebSocket
    }
}