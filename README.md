<p align="center">
  <img src="BAF Studio/baf-studio-icon.svg" alt="BAF Studio Icon" width="180" height="180" />
</p>

<h1 align="center">Browser Automation Framework &amp; BAF Studio</h1>

<p align="center">
  <strong>A stealth-first, .NET 10 browser automation, web scraping, and site reverse-engineering framework powered by real browser sessions.</strong>
</p>

---

## Why BAF? The Stealth Advantage Over Patchright, Playwright & Puppeteer

Traditional browser automation frameworks—such as **Playwright**, **Puppeteer**, **Selenium**, and even patched forks like **Patchright** or `puppeteer-extra-plugin-stealth`—operate from the outside in. They spawn an instrumented browser binary controlled over the **Chrome DevTools Protocol (CDP)** or **WebDriver**. Modern anti-bot systems (Cloudflare Turnstile, DataDome, Akamai, Kasada, Imperva) actively fingerprint and flag these sessions through:

* **CDP & Runtime Leaks:** Detection of `Runtime.enable`, `navigator.webdriver`, CDP serialization side effects, and patched binary signatures.
* **Synthetic Browser Profiles:** Fresh or isolated automation profiles lacking organic browsing history, established cookies, hardware-backed trust tokens, and real extension ecosystems.
* **Network & TLS Fingerprinting:** Discrepancies in JA3/JA4 TLS handshakes, HTTP/2 frame ordering, and OS process-tree anomalies when a browser is spawned as a child process of an automation driver.

### How BAF Achieves True Native Stealth

**Browser Automation Framework (BAF)** flips the architecture completely: **it does not launch a fake or patched browser.** Instead, BAF runs a lightweight local C# IPC server (`AutomationServer`) that communicates with a bridge script running **inside your actual, everyday, trusted browser instance**.

| Capability / Vector | Playwright / Patchright / Selenium | Browser Automation Framework (BAF) |
| :--- | :--- | :--- |
| **Browser Instance** | Spawns an automated/patched browser binary | Uses your **real, daily-driver browser** |
| **Control Protocol** | Chrome DevTools Protocol (CDP) / WebDriver | Local WebSocket / `GM_xmlhttpRequest` IPC Bridge |
| **TLS / JA3 / HTTP2 Fingerprint** | Automation binary fingerprint | **100% authentic native browser fingerprint** |
| **Cookies, Sessions & Trust Tokens** | Isolated / synthetic profile | **Real user profile, existing logins & trust score** |
| **React / SPA Event Handling** | External synthetic CDP input dispatch | In-context `__reactProps$` & native DOM event simulation |
| **Strict Page CSP (`connect-src`)** | Often breaks injected scripts | **Automatic fallback to privileged `GM_xmlhttpRequest` polling** |

Because all navigation, DOM inspection, clicking, input entry, and scraping happen inside a genuine human browser session with real cookies, extensions, GPU/WebGL state, and network stack, **most anti-bot systems cannot distinguish BAF activity from normal user interaction.**

---

## Browser Compatibility & Firefox Support Note

| Browser | Unpacked MV3 Extension (Recommended) | Legacy Tampermonkey Userscript |
| :--- | :---: | :---: |
| **Google Chrome** | Supported | Supported |
| **Microsoft Edge** | Supported | Supported |
| **Brave / Vivaldi / Opera** | Supported | Supported |
| **Mozilla Firefox** | *Not Yet Available* | **Required (Supported)** |

> **Important Note for Firefox Users:**  
> There is **no native Firefox extension yet** for the BAF Browser Extension. If you are automating or scraping in **Firefox**, you must use the **legacy Tampermonkey userscript** (`Generate & Copy Script (TM)` in BAF Studio or `ScriptGenerator.Generate(...)` in C#) until dedicated Firefox extension support is built.

---

## Key Features

### 1. Dual-Transport IPC Engine (WebSocket + CSP-Safe HTTP Fallback)
* **Full-Duplex WebSockets (`IpcMode.WebSocket`):** Instantaneous command execution, live DOM mutation streaming, and real-time navigation tracking over `ws://127.0.0.1:{port}`.
* **Automatic CSP Bypass (`IpcMode.HttpPolling`):** If a target website enforces a strict `Content-Security-Policy` (`connect-src`) that blocks local WebSocket connections, the BAF bridge automatically detects the violation and seamlessly switches to privileged `GM_xmlhttpRequest` HTTP polling without dropping the session.

### 2. Unpacked Manifest V3 Chrome Extension & Live Tab Manager
* **Zero-Config Generation:** Automatically exports and synchronizes the unpacked Manifest V3 Chrome Extension (`manifest.json`, `background.js`, `content_relay.js`, `bridge.js`, `popup.html`, `popup.js`) to match your configured server port.
* **Live Open-Tab Discovery:** View and filter every open tab in your browser directly from C# or the BAF Studio sidebar.
* **One-Click Injection & Exclusive Tab Switching:** Inject the automation bridge into any open tab on demand, automatically detach previous tabs in exclusive mode, or bring a target tab to the foreground in Chrome.

### 3. High-Performance Virtualized DOM Explorer & Middle-Click Picker
* **O(1) Indexed `DomTreeSnapshot`:** Serializes deep DOM hierarchies (up to 300 levels) and builds a pre-indexed, syntax-tokenized tree on a background thread.
* **Flicker-Free Virtualized `DomTreeView`:** Renders only visible rows at 60+ FPS with full keyboard navigation, expand/collapse glyphs, and dark-mode syntax highlighting.
* **Two-Way Live Highlighting:**
  * Selecting any node in the DOM Explorer highlights the element in the live browser tab.
  * **Middle-clicking any element in the browser** highlights it and immediately selects and scrolls to that exact node in the Studio DOM tree.

### 4. React-Compatible Automation Engine (`AutomationRunner`)
* **React Fiber & Native Value Bypass:**
  * `ClickReactElementAsync`: Invokes React's internal `__reactProps$` / `__reactEventHandlers$` `onClick` handlers with a proxied `isTrusted: true` event, or executes SPA-aware link navigation and pointer/mouse event sequences.
  * `SetReactInputValueAsync`: Bypasses React's overridden value setter tracker using prototype descriptor setters and dispatches `input` and `change` events so React state updates reliably.
* **Modular Workflows:** Organize automation into `AutomationProject` -> `AutomationSequence` -> `AutomationStep` lists (`Click`, `SetValue`, `Navigate`, `EvaluateScript`, `Scrape`, `DumpSite`, `Custom`).
* **Custom Step & Action Extensibility:** Register custom C# step delegates (`RegisterStepHandler`, `RegisterCustomActionHandler`) or custom browser-side JS actions (`registerAction`) with real-time progress streaming (`window.streamData`).

### 5. Intelligent Data Scraper (`DataScraper` & Fluent `ScrapeBuilder`)
* **Point-and-Click Selector Generalization (`SelectorHelper`):** Right-click any DOM node to add a single field, generalize repeating sibling fields across lists/tables, set a repeating item container (automatically relativizing child selectors to `:scope`), or configure a pagination Next button.
* **12 Built-In Extraction Modes:** `InnerText`, `TextContent`, `InnerHtml`, `OuterHtml`, `Value`, `Attribute`, `Href`, `Src`, `DataAttribute`, `ComputedStyle`, `Exists`, and `Count`.
* **Regex & Post-Processing:** Built-in regular expression matching with capture group selection, trimming, fallback default values, and cross-page row deduplication.
* **Flexible Multi-Page Pagination:**
  * **Next-Button Clicking:** Clicks a CSS selector between pages and awaits SPA or full-page transitions.
  * **URL Pattern Templates:** Navigates across URL patterns (`https://site.com/list?page={page}`, `?p=1`, etc.) with configurable start page and increment.
  * **Custom Pagination Sequences:** Runs a custom `AutomationSequence` (e.g., scrolling or clicking "Load More") between pages.
* **Automatic HTTP 429 Rate-Limit Recovery:**
  * Automatically detects HTTP 429 responses via Navigation Timing headers, status probing, and document inspection.
  * Parses `Retry-After`, `X-Retry-After`, and `RateLimit-Reset` headers (supporting both relative seconds and HTTP-date timestamps).
  * Displays a non-intrusive status banner in the browser, waits out the exact cooldown period, dynamically increases `PageDelayMs` for subsequent pages, and retries automatically.
* **Rich Exports & POCO Binding:** Export results to **JSON**, **CSV**, `System.Data.DataTable`, or bind directly to strongly-typed C# classes via `result.BindTo<T>()`.

### 6. Site Reverse Engineering & Asset Dumper (`SiteDumper`)
* **4-Mode Asset Extraction:** Dump **HTML**, **CSS**, **JavaScript**, or **Everything** (`SiteDumpType.All`) from any live page:
  * **HTML:** Clean main document HTML (stripped of bridge artifacts) plus all accessible same-origin and cross-origin `<iframe>` documents.
  * **CSS:** External `<link rel="stylesheet">` files, inline `<style>` blocks, CSSOM constructed stylesheets, and `document.adoptedStyleSheets`.
  * **JavaScript:** External `<script src>` files, inline scripts/JSON blocks, and dynamically loaded scripts discovered via the browser's `performance.getEntriesByType('resource')` API.
* **Instant Search & Disk Bundling:** Filter captured resources by filename, URL, source type, or code content, view combined bundles, or export the entire site dump to disk (`index.html`, `css/`, `js/`, `combined.css`, `combined.js`, and `manifest.json`).

### 7. 100% Decoupled Core Library (`BrowserAutomationFramework.dll`) & BAF Studio IDE
* Every feature of the framework lives inside `BrowserAutomationFramework.dll` with **zero dependency on Windows Forms**.
* Use the included **BAF Studio** desktop IDE to visually inspect DOM trees, build projects, and test scrapes, or reference `BrowserAutomationFramework.dll` directly in your own console apps, background workers, web APIs, or custom GUIs.

---

## Quick Start

### Option A: Using the BAF Studio GUI

1. **Build and Run BAF Studio:**
   ~~~bash
   dotnet build
   dotnet run --project "BAF Studio/BAF Studio.csproj"
   ~~~
2. **Start the Server:** Set your desired port (default `9999`) and IPC mode (`WebSocket`), then click **Start Server**.
3. **Connect Your Browser:**
   * **Chrome / Edge / Brave (Recommended):**
     1. Click **Export Chrome Extension...** (or use the auto-synced `BrowserAutomationFramework\ChromeExtension` folder).
     2. Open `chrome://extensions`, enable **Developer mode**, click **Load unpacked**, and select the folder.
     3. Select any open browser tab in the **Chrome Extension - Open Tabs** panel to inject and switch to it immediately.
   * **Firefox (or Legacy Userscript Mode):**
     1. Install the [Tampermonkey](https://www.tampermonkey.net/) extension in Firefox.
     2. In BAF Studio, configure your target **Match URLs** (or leave **Match Any Site** checked) and click **Generate & Copy Script (TM)**.
     3. Create a new userscript in Tampermonkey, paste the clipboard contents, save, and reload your target webpage.

---

### Option B: Programmatic C# Usage (`BrowserAutomationFramework.dll`)

#### 1. React-Safe Automation & Navigation

~~~csharp
using BrowserAutomationFramework.Extensions;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

using var server = new AutomationServer(port: 9999);

server.OnClientConnected += async (sender, client) =>
{
    Console.WriteLine($"Client connected: {client.DisplayName}");

    // Navigate and wait for page load
    await client.NavigateToUrlAsync("https://example.com/search");

    // Set React-controlled input and click search
    await client.SetReactInputValueAsync("input[name='q']", "stealth automation", overwrite: true);
    await client.ClickReactElementAsync("button[type='submit']");

    // Extract a single element's text
    string? heading = await client.ScrapeElementAsync("h1", ScrapeExtractionType.InnerText);
    Console.WriteLine($"Heading: {heading}");
};

await server.StartAsync();
~~~

#### 2. Multi-Page Scraping with Fluent `ScrapeBuilder`

~~~csharp
using BrowserAutomationFramework.Extensions;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

public record ListingItem
{
    public string Name { get; set; } = "";
    public string Price { get; set; } = "";
    public string Link { get; set; } = "";
}

public async Task<List<ListingItem>> ScrapeListingsAsync(BrowserClient client)
{
    ScrapeResult result = await client.CreateScrape("Listings")
        .WithContainer("article.listing-card")
        .Field("Name", "h3.title", ScrapeExtractionType.InnerText)
        .Field("Price", "span.price", ScrapeExtractionType.InnerText)
        .Field("Link", "a.details", ScrapeExtractionType.Href)
        .WithPagination(nextPageSelector: "a[rel='next']", maxPages: 10, pageDelayMs: 1500, deduplicateRows: true)
        .ExecuteAsync();

    // Save to CSV/JSON or bind to strongly-typed objects
    await result.SaveToCsvFileAsync("listings.csv");
    return result.BindTo<ListingItem>();
}
~~~

#### 3. Reverse-Engineering & Dumping a Site

~~~csharp
using BrowserAutomationFramework.Extensions;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;

public async Task DumpSiteAssetsAsync(BrowserClient client)
{
    SiteDumpResult dump = await client.DumpAllAsync();
    await dump.SaveToDirectoryAsync(@"C:\ExportedSiteDump");
}
~~~

---

## Repository Structure

~~~text
BrowserAutomationFramework/
├── BrowserAutomationFramework/          # Core .NET 10 Class Library (BrowserAutomationFramework.dll)
│   ├── Automation/                      # AutomationRunner & AutomationStudioSession facade
│   ├── ChromeExtension/                 # Synced Manifest V3 Chrome Extension files
│   ├── Extensions/                      # ReactExtensions for BrowserClient
│   ├── Generators/                      # ScriptGenerator (TM) & ChromeExtensionGenerator
│   ├── Models/                          # Project, Scrape, DOM Snapshot, Dump & Event models
│   ├── ReverseEngineering/              # SiteDumper engine
│   ├── Scraping/                        # DataScraper, ScrapeBuilder & SelectorHelper
│   ├── Server/                          # AutomationServer & BrowserClient IPC engine
│   ├── Utils/                           # Windows file-association utilities
│   ├── BrowserAutomationFramework.csproj# Core library project file
│   └── API_REFERENCE.md                 # Full public API & IPC protocol reference
└── BAF Studio/                          # BAF Studio — Dark-mode WinForms IDE (BAF Studio.exe)
    ├── Controls/                        # Virtualized DomTreeView control
    ├── utils/                           # Dark mode theming utilities
    ├── BAF Studio.csproj                # BAF Studio WinForms project file
    ├── baf-studio-icon.svg              # BAF Studio 512x512 vector application icon
    └── MainForm.cs                      # BAF Studio UI wrapper
~~~

---

## Documentation

For detailed type-by-type documentation of every namespace, class, method signature, event, and browser IPC command, see **[API_REFERENCE.md](BrowserAutomationFramework/API_REFERENCE.md)**.