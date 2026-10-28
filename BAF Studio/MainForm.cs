using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrowserAutomationFramework.Automation;
using BrowserAutomationFramework.Models;
using BrowserAutomationFramework.Server;
using BAFStudio.Utils;

namespace BAFStudio
{
    public partial class MainForm : Form
    {
        private readonly AutomationStudioSession _session = new();
        private bool _isUpdatingScrapeUi;

        // Data Scraper Tab Controls
        private SplitContainer splitScraperMain = null!;
        private SplitContainer splitScraperRight = null!;
        private SplitContainer splitFields = null!;
        private CheckedListBox chkLstScrapes = null!;
        private TextBox txtContainerSelector = null!;
        private ComboBox cmbPreScrapeSeq = null!;
        private ComboBox cmbPostScrapeSeq = null!;
        private CheckBox chkEnablePagination = null!;
        private NumericUpDown numMaxPages = null!;
        private NumericUpDown numStartPage = null!;
        private NumericUpDown numPageDelayMs = null!;
        private TextBox txtNextPageSelector = null!;
        private TextBox txtPaginationUrlPattern = null!;
        private ListBox lstScrapeFields = null!;
        private PropertyGrid pgScrapeFieldDetails = null!;
        private Button btnPreviewScrape = null!;
        private Button btnRunAllScrapes = null!;
        private Label lblScrapeStatus = null!;
        private DataGridView dgvScrapePreview = null!;
        private TextBox txtScrapeJsonPreview = null!;

        // Reverse Engineering Tab Controls
        private SplitContainer splitRevEngMain = null!;
        private SplitContainer splitRevEngLeft = null!;
        private Button btnDumpHtml = null!;
        private Button btnDumpCss = null!;
        private Button btnDumpJs = null!;
        private Button btnDumpAll = null!;
        private CheckBox chkDumpInline = null!;
        private CheckBox chkDumpExternal = null!;
        private CheckBox chkDumpDynamicJs = null!;
        private TextBox txtFilterResources = null!;
        private ListBox lstDumpedResources = null!;
        private TextBox txtDumpedResourceDetails = null!;
        private Label lblDumpStatus = null!;
        private TabControl tabDumpViewers = null!;
        private TabPage tabSelectedResourceView = null!;
        private TabPage tabCombinedHtmlView = null!;
        private TabPage tabCombinedCssView = null!;
        private TabPage tabCombinedJsView = null!;
        private TextBox txtSelectedResourceSource = null!;
        private TextBox txtCombinedHtmlSource = null!;
        private TextBox txtCombinedCssSource = null!;
        private TextBox txtCombinedJsSource = null!;

        // Chrome Extension Sidebar Controls
        private SplitContainer splitLeftSidebar = null!;
        private GroupBox grpExtensionTabs = null!;
        private Label lblExtensionStatus = null!;
        private Button btnSwitchTab = null!;
        private Button btnDetachTab = null!;
        private Button btnFocusTab = null!;
        private Button btnRefreshTabs = null!;
        private CheckBox chkExclusiveTab = null!;
        private TextBox txtFilterTabs = null!;
        private ListBox lstBrowserTabs = null!;
        private bool _isUpdatingBrowserTabsList;
        private bool _isUpdatingClientsList;

        private bool _leftSidebarSplitterInitialized;
        private bool _domSplittersInitialized;
        private bool _editorSplittersInitialized;
        private bool _scraperSplittersInitialized;
        private bool _revEngSplittersInitialized;
        private bool _isShuttingDownOnClose;
        private bool _isClosingComplete;

        public MainForm() : this(null)
        {
        }

        public MainForm(string[]? startupArgs)
        {
            InitializeComponent();
            DarkModeTitleBar.Enable(this);
            InitializeExtensionTabsSidebarUi();
            InitializeScraperTabUi();
            InitializeReverseEngineeringTabUi();
            ApplyFixedSingleBorders(this);

            _session.TrySyncDefaultChromeExtensionQuietly((int)numPort.Value, AutomationStudioSession.DefaultIpcMode);

            tabMain.SelectedIndexChanged += (s, e) => EnsureTabSplittersConfigured();
            Shown += (s, e) => EnsureTabSplittersConfigured();

            cmbMode.DataSource = Enum.GetValues(typeof(IpcMode));
            cmbMode.SelectedItem = AutomationStudioSession.DefaultIpcMode;

            _session.OnLog += (s, e) => AppendLogLine(e.FormattedMessage, ToColor(e.ColorRgb));
            _session.OnScrapeStatusChanged += (s, msg) => InvokeOnUi(() => lblScrapeStatus.Text = msg);
            _session.OnDumpStatusChanged += (s, msg) => InvokeOnUi(() => lblDumpStatus.Text = msg);
            _session.OnRateLimitDetected += (s, e) => InvokeOnUi(() =>
            {
                if (e.ShouldUpdatePageDelayFor(chkLstScrapes.SelectedItem as ScrapeDefinition))
                {
                    _isUpdatingScrapeUi = true;
                    try
                    {
                        numPageDelayMs.Value = ScrapeDefinition.ClampPageDelayMs(e.NewPageDelayMs);
                    }
                    finally
                    {
                        _isUpdatingScrapeUi = false;
                    }
                }
            });
            _session.OnScrapeResultAvailable += (s, result) => InvokeOnUi(() => DisplayScrapeResult(result));
            _session.OnSiteDumpResultAvailable += (s, result) => InvokeOnUi(() => DisplaySiteDumpResult(result));

            _session.OnClientConnected += Server_OnClientConnected;
            _session.OnClientDisconnected += Server_OnClientDisconnected;
            _session.OnClientMetadataUpdated += Server_OnClientMetadataUpdated;
            _session.OnActiveClientChanged += Server_OnActiveClientChanged;
            _session.OnActiveClientPageLoaded += Server_OnActiveClientPageLoaded;
            _session.OnActiveClientDomChanged += Server_OnActiveClientDomChanged;
            _session.OnActiveClientHighlightedElementRemoved += Server_OnActiveClientHighlightedElementRemoved;
            _session.OnActiveClientElementClicked += Server_OnActiveClientElementClicked;
            _session.OnExtensionStateChanged += (s, e) => InvokeOnUi(UpdateExtensionStatusLabel);
            _session.OnExtensionTabsUpdated += Server_OnExtensionTabsUpdated;

            var startupProject = _session.TryLoadProjectFromCommandLineArgs(startupArgs);
            if (startupProject != null)
            {
                ApplyProjectToUi(startupProject);
            }
        }

        private void InvokeOnUi(Action action)
        {
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }

        private static Color ToColor((byte R, byte G, byte B) rgb) => Color.FromArgb(rgb.R, rgb.G, rgb.B);

        private void AppendLogLine(string formattedLine, Color color)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLogLine(formattedLine, color)));
                return;
            }

            rtbLogs.SelectionStart = rtbLogs.TextLength;
            rtbLogs.SelectionLength = 0;
            rtbLogs.SelectionColor = color;
            rtbLogs.AppendText($"{formattedLine}{Environment.NewLine}");
            rtbLogs.ScrollToCaret();
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            int port = (int)numPort.Value;
            var selectedMode = cmbMode.SelectedItem is IpcMode m ? m : AutomationStudioSession.DefaultIpcMode;
            _session.ExclusiveTabMode = chkExclusiveTab.Checked;

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            numPort.Enabled = false;

            if (!await _session.TryStartServerAsync(port, selectedMode))
            {
                ResetServerStoppedUi();
            }
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            _session.StopServer();
            ResetServerStoppedUi();
        }

        private void ResetServerStoppedUi()
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            numPort.Enabled = true;
            lstClients.Items.Clear();
            lstBrowserTabs.Items.Clear();
            UpdateExtensionStatusLabel();
        }

        private void ChkMatchAnySite_CheckedChanged(object? sender, EventArgs e)
        {
            _session.SetMatchAnySite(chkMatchAnySite.Checked);
            lblMatchUrls.Visible = !_session.Project.MatchAnySite;
            pnlMatchUrls.Visible = !_session.Project.MatchAnySite;
        }

        private void BtnAddMatchUrl_Click(object? sender, EventArgs e)
        {
            string url = PromptForValue("Enter Match URL (e.g., https://*.example.com/*):", "Add Match URL");
            string? added = _session.AddMatchUrl(url);
            if (added != null)
            {
                lstMatchUrls.Items.Add(added);
            }
        }

        private void BtnEditMatchUrl_Click(object? sender, EventArgs e)
        {
            if (lstMatchUrls.SelectedItem is string currentUrl)
            {
                int idx = lstMatchUrls.SelectedIndex;
                string url = PromptForValue("Edit Match URL:", "Edit Match URL", currentUrl);
                if (_session.UpdateMatchUrl(idx, url))
                {
                    lstMatchUrls.Items[idx] = _session.Project.MatchUrls[idx];
                }
            }
        }

        private void BtnRemoveMatchUrl_Click(object? sender, EventArgs e)
        {
            if (lstMatchUrls.SelectedItem is string selectedUrl)
            {
                _session.RemoveMatchUrl(selectedUrl);
                lstMatchUrls.Items.Remove(selectedUrl);
            }
        }

        private void ApplyProjectToUi(AutomationProject project)
        {
            chkMatchAnySite.Checked = project.MatchAnySite;
            lstMatchUrls.Items.Clear();
            foreach (var url in project.MatchUrls)
            {
                lstMatchUrls.Items.Add(url);
            }

            chkLstSequences.Items.Clear();
            lstSteps.Items.Clear();
            foreach (var seq in project.Sequences)
            {
                chkLstSequences.Items.Add(seq, seq.IsActive);
            }

            if (chkLstSequences.Items.Count > 0)
            {
                chkLstSequences.SelectedIndex = 0;
            }

            RefreshAutomationSequenceDropdowns();

            chkLstScrapes.Items.Clear();
            lstScrapeFields.Items.Clear();
            pgScrapeFieldDetails.SelectedObject = null;
            foreach (var scrape in project.Scrapes)
            {
                chkLstScrapes.Items.Add(scrape, scrape.IsActive);
            }

            if (chkLstScrapes.Items.Count > 0)
            {
                chkLstScrapes.SelectedIndex = 0;
            }
            else
            {
                LoadSelectedScrapeToUi();
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            var mode = (IpcMode)cmbMode.SelectedItem!;
            string script = _session.GenerateUserscript((int)numPort.Value, mode);
            Clipboard.SetText(script);
        }

        private void BtnExportExtension_Click(object? sender, EventArgs e)
        {
            string defaultDir = _session.GetExistingDefaultExtensionDirectory();
            using var fbd = new FolderBrowserDialog
            {
                Description = "Select a folder to export the unpacked BAF Chrome Extension",
                UseDescriptionForTitle = true,
                SelectedPath = defaultDir
            };

            if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                int port = (int)numPort.Value;
                var mode = cmbMode.SelectedItem is IpcMode m ? m : AutomationStudioSession.DefaultIpcMode;
                string? instructions = _session.TryExportChromeExtension(fbd.SelectedPath, port, mode);
                if (instructions != null)
                {
                    DarkMessageBox.Show(
                        this,
                        instructions,
                        "Chrome Extension Exported",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        private void Server_OnClientConnected(object? sender, BrowserClient client)
        {
            BeginInvoke(new Action(() => UpsertClientInListUi(client)));
        }

        private void Server_OnActiveClientPageLoaded(object? sender, PageLoadedEventArgs e)
        {
            if (sender is BrowserClient client)
            {
                BeginInvoke(new Action(async () => await _session.RefreshActiveClientDomAsync(
                    client,
                    getCurrentSelectedPath: () => tvDom.SelectedPath,
                    onSnapshotReady: ApplyDomSnapshotResultToUi)));
            }
        }

        private void Server_OnActiveClientDomChanged(object? sender, DomChangedEventArgs e)
        {
            if (sender is BrowserClient client)
            {
                BeginInvoke(new Action(async () => await _session.RefreshActiveClientDomAsync(
                    client,
                    targetSelectedPath: e.HighlightedPath,
                    hasTargetPath: e.HasHighlightedPath,
                    getCurrentSelectedPath: () => tvDom.SelectedPath,
                    onSnapshotReady: ApplyDomSnapshotResultToUi)));
            }
        }

        private void Server_OnActiveClientHighlightedElementRemoved(object? sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                tvDom.ClearSelection();
                txtElementDetails.Text = string.Empty;
            }));
        }

        private void Server_OnActiveClientElementClicked(object? sender, ElementClickedEventArgs e)
        {
            if (sender is BrowserClient client)
            {
                BeginInvoke(new Action(async () => await _session.HandleActiveClientElementClickedAsync(
                    e,
                    client,
                    trySelectExistingPath: path => tvDom.SelectNodeByPath(path, ensureVisible: true, raiseAfterSelect: false),
                    onSelectedInExistingTree: _ => txtElementDetails.Text = tvDom.SelectedDomNode?.GetFormattedDetails() ?? string.Empty,
                    getCurrentSelectedPath: () => tvDom.SelectedPath,
                    onSnapshotReady: ApplyDomSnapshotResultToUi)));
            }
        }

        private void Server_OnClientMetadataUpdated(object? sender, BrowserClient client)
        {
            InvokeOnUi(() => RefreshClientListItemDisplay(client));
        }

        private void Server_OnActiveClientChanged(object? sender, BrowserClient? activeClient)
        {
            InvokeOnUi(() => SyncActiveClientSelectionToUi(activeClient));
        }

        private void UpsertClientInListUi(BrowserClient client)
        {
            _isUpdatingClientsList = true;
            try
            {
                _session.UpsertClientInList(lstClients.Items, client);
            }
            finally
            {
                _isUpdatingClientsList = false;
            }
        }

        private async void SyncActiveClientSelectionToUi(BrowserClient? activeClient)
        {
            if (activeClient != null)
            {
                UpsertClientInListUi(activeClient);
            }

            _isUpdatingClientsList = true;
            try
            {
                lstClients.SelectedItem = activeClient;
            }
            finally
            {
                _isUpdatingClientsList = false;
            }

            if (activeClient != null)
            {
                await _session.RefreshActiveClientDomAsync(
                    activeClient,
                    getCurrentSelectedPath: () => tvDom.SelectedPath,
                    onSnapshotReady: ApplyDomSnapshotResultToUi);
            }
            else
            {
                tvDom.Clear();
                txtElementDetails.Text = string.Empty;
            }
        }

        private void Server_OnClientDisconnected(object? sender, BrowserClient client)
        {
            InvokeOnUi(() => HandleClientRemovedFromUi(client));
        }

        private void HandleClientRemovedFromUi(BrowserClient client)
        {
            _isUpdatingClientsList = true;
            try
            {
                _session.RemoveClientFromList(lstClients.Items, client);
            }
            finally
            {
                _isUpdatingClientsList = false;
            }
        }

        private void LstClients_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingClientsList) return;
            _session.ActiveClient = lstClients.SelectedItem as BrowserClient;
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            await _session.SendRawCommandAsync(txtCmdType.Text, txtPayload.Text);
        }

        private async void BtnReactClick_Click(object sender, EventArgs e)
        {
            await _session.TestReactClickAsync(txtPayload.Text);
        }

        private async void BtnEval_Click(object sender, EventArgs e)
        {
            await _session.TestEvalAsync(txtPayload.Text);
        }

        private void ApplyDomSnapshotResultToUi(BrowserClient client, DomSnapshotRefreshResult result)
        {
            tvDom.ApplySnapshotResult(result);

            string details = result.SelectedNodeDetails;
            if (!string.Equals(txtElementDetails.Text, details, StringComparison.Ordinal))
            {
                txtElementDetails.Text = details;
            }
        }

        private void TvDom_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is DomNode domNode)
            {
                txtElementDetails.Text = domNode.GetFormattedDetails();
                _session.SelectDomNode(domNode);
            }
        }

        private void TvDom_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Node?.Tag is DomNode domNode && domNode.IsElement)
            {
                ctxDomNode.Show(tvDom, e.Location);
            }
        }

        private async void MnuClickElement_Click(object? sender, EventArgs e)
        {
            await _session.ClickDomNodeAsync(tvDom.SelectedDomNode);
        }

        private static void ApplyFixedSingleBorders(Control control)
        {
            switch (control)
            {
                case TextBoxBase textBoxBase:
                    textBoxBase.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ListBox listBox:
                    listBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case TreeView treeView:
                    treeView.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case BAFStudio.Controls.DomTreeView domTreeView:
                    domTreeView.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case UpDownBase upDownBase:
                    upDownBase.BorderStyle = BorderStyle.FixedSingle;
                    break;
            }

            foreach (Control child in control.Controls)
            {
                ApplyFixedSingleBorders(child);
            }
        }

        private string PromptForValue(string text, string caption, string defaultValue = "")
        {
            using Form prompt = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                BackColor = ToColor(StudioTheme.DarkBackground),
                ForeColor = ToColor(StudioTheme.Foreground),
                Font = Font,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            float scaleFactor = DeviceDpi / 96f;
            int Scale(int value) => (int)Math.Ceiling(value * scaleFactor);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(Scale(16)),
                MinimumSize = new Size(Scale(380), 0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label textLabel = new Label
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, Scale(8))
            };

            TextBox textBox = new TextBox
            {
                Text = defaultValue,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ToColor(StudioTheme.DarkBackground),
                ForeColor = ToColor(StudioTheme.Foreground),
                Margin = new Padding(0, 0, 0, Scale(14))
            };

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            Button confirmation = new Button
            {
                Text = "Ok",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                MinimumSize = new Size(Scale(90), Scale(32)),
                Padding = new Padding(Scale(10), Scale(2), Scale(10), Scale(2)),
                Margin = new Padding(0),
                UseVisualStyleBackColor = true
            };

            buttonPanel.Controls.Add(confirmation);
            layout.Controls.Add(textLabel, 0, 0);
            layout.Controls.Add(textBox, 0, 1);
            layout.Controls.Add(buttonPanel, 0, 2);

            prompt.Controls.Add(layout);
            prompt.AcceptButton = confirmation;

            ApplyFixedSingleBorders(prompt);

            return prompt.ShowDialog(this) == DialogResult.OK ? textBox.Text : string.Empty;
        }

        private async void MnuSetValue_Click(object? sender, EventArgs e)
        {
            DomNode? domNode = tvDom.SelectedDomNode;
            if (!_session.CanTargetDomNode(domNode))
                return;

            string value = PromptForValue("Enter value to set:", "Set Value");
            await _session.SetDomNodeValueAsync(domNode, value);
        }

        private void CtxDomNode_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            mnuAddClickTo.DropDownItems.Clear();
            mnuAddSetValueTo.DropDownItems.Clear();

            if (!_session.HasSequences)
            {
                mnuAddClickTo.DropDownItems.Add(new ToolStripMenuItem("(No lists available)") { Enabled = false });
                mnuAddSetValueTo.DropDownItems.Add(new ToolStripMenuItem("(No lists available)") { Enabled = false });
            }
            else
            {
                foreach (AutomationSequence seq in _session.Project.Sequences)
                {
                    var clickItem = new ToolStripMenuItem(seq.Name);
                    clickItem.Click += (s, args) => AddStepToSequence(seq, AutomationActionType.Click);
                    mnuAddClickTo.DropDownItems.Add(clickItem);

                    var setValItem = new ToolStripMenuItem(seq.Name);
                    setValItem.Click += (s, args) => AddStepToSequence(seq, AutomationActionType.SetValue);
                    mnuAddSetValueTo.DropDownItems.Add(setValItem);
                }
            }

            PopulateScrapeSubMenu(
                mnuAddScrapeFieldTo,
                scrape => AddFieldFromSelectedDomNode(scrape, matchAllElements: false),
                name => _session.CreateScrapeWithFieldFromDomNode(name, tvDom.SelectedDomNode, matchAllElements: false, tvDom.RootDomNode));
            PopulateScrapeSubMenu(
                mnuAddRepeatingFieldTo,
                scrape => AddFieldFromSelectedDomNode(scrape, matchAllElements: true),
                name => _session.CreateScrapeWithFieldFromDomNode(name, tvDom.SelectedDomNode, matchAllElements: true, tvDom.RootDomNode));
            PopulateScrapeSubMenu(
                mnuSetScrapeContainerFor,
                SetContainerFromSelectedDomNode,
                name => _session.CreateScrapeWithContainerFromDomNode(name, tvDom.SelectedDomNode, tvDom.RootDomNode));
            PopulateScrapeSubMenu(
                mnuSetPaginationNextFor,
                SetPaginationNextFromSelectedDomNode,
                name => _session.CreateScrapeWithPaginationFromDomNode(name, tvDom.SelectedDomNode, tvDom.RootDomNode));
        }

        private void PopulateScrapeSubMenu(
            ToolStripMenuItem parentMenu,
            Action<ScrapeDefinition> onSelect,
            Func<string, ScrapeDefinition?> createScrape)
        {
            parentMenu.DropDownItems.Clear();

            foreach (ScrapeDefinition scrape in _session.Project.Scrapes)
            {
                var item = new ToolStripMenuItem(scrape.Name);
                item.Click += (s, e) => onSelect(scrape);
                parentMenu.DropDownItems.Add(item);
            }

            if (_session.HasScrapes)
            {
                parentMenu.DropDownItems.Add(new ToolStripSeparator());
            }

            var createNewItem = new ToolStripMenuItem("(+ New Scrape List...)");
            createNewItem.Click += (s, e) =>
            {
                string name = PromptForValue("Enter new scrape list name:", "New Scrape List", _session.Project.GetSuggestedScrapeName());
                var newScrape = createScrape(name);
                if (newScrape != null)
                {
                    chkLstScrapes.Items.Add(newScrape, newScrape.IsActive);
                    chkLstScrapes.SelectedItem = newScrape;
                }
            };
            parentMenu.DropDownItems.Add(createNewItem);
        }

        private void AddStepToSequence(AutomationSequence seq, AutomationActionType actionType)
        {
            var step = _session.AddStepFromDomNode(seq, tvDom.SelectedDomNode, actionType);
            if (step != null)
            {
                if (chkLstSequences.SelectedItem == seq)
                {
                    lstSteps.Items.Add(step);
                    lstSteps.SelectedItem = step;
                }
                tabMain.SelectedTab = tabEditor;
            }
        }

        private void BtnAddStep_Click(object? sender, EventArgs e)
        {
            var step = _session.AddStepToSequence(chkLstSequences.SelectedItem as AutomationSequence);
            if (step != null)
            {
                lstSteps.Items.Add(step);
                lstSteps.SelectedItem = step;
            }
        }

        private void BtnRemoveStep_Click(object? sender, EventArgs e)
        {
            if (chkLstSequences.SelectedItem is AutomationSequence seq && lstSteps.SelectedItem is AutomationStep step)
            {
                _session.RemoveStep(seq, step);
                lstSteps.Items.Remove(step);
            }
        }

        private void LstSteps_SelectedIndexChanged(object? sender, EventArgs e)
        {
            pgStepDetails.SelectedObject = lstSteps.SelectedItem;
        }

        private void PgStepDetails_PropertyValueChanged(object? s, PropertyValueChangedEventArgs e)
        {
            _session.RefreshItemInList(lstSteps.Items, lstSteps.SelectedIndex);
        }

        private void MnuFileNew_Click(object? sender, EventArgs e)
        {
            ApplyProjectToUi(_session.ResetProject());
        }

        private void MnuFileOpen_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = AutomationStudioSession.ProjectFileDialogFilter,
                Title = "Open Automation Project"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var project = _session.TryLoadProjectFromFile(ofd.FileName);
                if (project != null)
                {
                    ApplyProjectToUi(project);
                }
            }
        }

        private void MnuFileSave_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = AutomationStudioSession.ProjectFileDialogFilter,
                Title = "Save Automation Project",
                DefaultExt = AutomationStudioSession.ProjectFileExtension
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _session.TrySaveProjectToFile(sfd.FileName);
            }
        }

        private void MnuFileExit_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void MnuHelpAbout_Click(object? sender, EventArgs e)
        {
            DarkMessageBox.Show(this, AutomationStudioSession.StudioAboutMessage, $"About {AutomationStudioSession.StudioShortTitle}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isClosingComplete && _session.IsServerRunning)
            {
                if (e.CloseReason == CloseReason.WindowsShutDown ||
                    e.CloseReason == CloseReason.TaskManagerClosing)
                {
                    _session.StopServer();
                    base.OnFormClosing(e);
                    return;
                }

                e.Cancel = true;
                if (_isShuttingDownOnClose)
                {
                    base.OnFormClosing(e);
                    return;
                }

                _isShuttingDownOnClose = true;
                try
                {
                    await _session.ShutdownAsync();
                    ResetServerStoppedUi();
                }
                finally
                {
                    _isClosingComplete = true;
                    _isShuttingDownOnClose = false;
                }

                Close();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            DarkModeTitleBar.WndProc(ref m);
        }

        private void BtnMoveUp_Click(object? sender, EventArgs e)
        {
            if (chkLstSequences.SelectedItem is AutomationSequence seq && lstSteps.SelectedItem is AutomationStep)
            {
                int idx = lstSteps.SelectedIndex;
                if (_session.MoveStepUp(seq, lstSteps.Items, idx, out int newIndex))
                {
                    lstSteps.SelectedIndex = newIndex;
                }
            }
        }

        private void BtnMoveDown_Click(object? sender, EventArgs e)
        {
            if (chkLstSequences.SelectedItem is AutomationSequence seq && lstSteps.SelectedItem is AutomationStep)
            {
                int idx = lstSteps.SelectedIndex;
                if (_session.MoveStepDown(seq, lstSteps.Items, idx, out int newIndex))
                {
                    lstSteps.SelectedIndex = newIndex;
                }
            }
        }

        private void ChkLstSequences_SelectedIndexChanged(object? sender, EventArgs e)
        {
            lstSteps.Items.Clear();
            if (chkLstSequences.SelectedItem is AutomationSequence seq)
            {
                foreach (var step in seq.Steps)
                {
                    lstSteps.Items.Add(step);
                }
            }
        }

        private void ChkLstSequences_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (chkLstSequences.Items[e.Index] is AutomationSequence seq)
            {
                _session.SetSequenceActive(seq, e.NewValue == CheckState.Checked);
            }
        }

        private void BtnAddSequence_Click(object? sender, EventArgs e)
        {
            string name = PromptForValue("Enter new list name:", "Add Automation List", _session.Project.GetSuggestedSequenceName());
            var seq = _session.TryAddSequence(name);
            if (seq != null)
            {
                chkLstSequences.Items.Add(seq, seq.IsActive);
                chkLstSequences.SelectedItem = seq;
                RefreshAutomationSequenceDropdowns();
            }
        }

        private void BtnRemoveSequence_Click(object? sender, EventArgs e)
        {
            if (chkLstSequences.SelectedItem is AutomationSequence seq)
            {
                _session.RemoveSequence(seq);
                chkLstSequences.Items.Remove(seq);
                RefreshAutomationSequenceDropdowns();
            }
        }

        private void BtnMoveSeqUp_Click(object? sender, EventArgs e)
        {
            int idx = chkLstSequences.SelectedIndex;
            if (idx < 0) return;
            bool isChecked = chkLstSequences.GetItemChecked(idx);
            if (_session.MoveSequenceUp(chkLstSequences.Items, idx, out int newIndex))
            {
                chkLstSequences.SetItemChecked(newIndex, isChecked);
                chkLstSequences.SelectedIndex = newIndex;
            }
        }

        private void BtnMoveSeqDown_Click(object? sender, EventArgs e)
        {
            int idx = chkLstSequences.SelectedIndex;
            if (idx < 0) return;
            bool isChecked = chkLstSequences.GetItemChecked(idx);
            if (_session.MoveSequenceDown(chkLstSequences.Items, idx, out int newIndex))
            {
                chkLstSequences.SetItemChecked(newIndex, isChecked);
                chkLstSequences.SelectedIndex = newIndex;
            }
        }

        private void BtnRenameSeq_Click(object? sender, EventArgs e)
        {
            if (chkLstSequences.SelectedItem is AutomationSequence seq)
            {
                string name = PromptForValue("Enter new list name:", "Rename Automation List", seq.Name);
                if (_session.RenameSequence(seq, name))
                {
                    int idx = chkLstSequences.SelectedIndex;
                    bool isChecked = chkLstSequences.GetItemChecked(idx);
                    _session.RefreshItemInList(chkLstSequences.Items, idx);
                    chkLstSequences.SetItemChecked(idx, isChecked);
                    RefreshAutomationSequenceDropdowns();
                }
            }
        }

        private async void BtnRunAutomation_Click(object? sender, EventArgs e)
        {
            btnRunAutomation.Enabled = false;
            try
            {
                await _session.RunActiveSequencesAsync();
            }
            finally
            {
                btnRunAutomation.Enabled = true;
            }
        }

        // --- Data Scraper UI & Point-and-Click Handlers ---

        private int ScaleDpi(int value) => (int)Math.Ceiling(value * (DeviceDpi / 96f));

        private static Button CreateToolbarButton(string text, int padX, int padY, int margin)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(padX, padY, padX, padY),
                Margin = new Padding(margin),
                UseVisualStyleBackColor = true
            };
        }

        private void InitializeExtensionTabsSidebarUi()
        {
            var darkBg = ToColor(StudioTheme.DarkBackground);
            int padX = ScaleDpi(5);
            int padY = ScaleDpi(2);
            int btnMargin = ScaleDpi(2);

            splitLeftSidebar = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Size = new Size(300, 440)
            };

            grpExtensionTabs = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "Chrome Extension - Open Tabs",
                Padding = new Padding(ScaleDpi(8), ScaleDpi(6), ScaleDpi(8), ScaleDpi(8)),
                ForeColor = ToColor(StudioTheme.Foreground)
            };

            var pnlExtTop = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(0, 0, 0, ScaleDpi(4))
            };
            pnlExtTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 4; i++)
            {
                pnlExtTop.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            lblExtensionStatus = new Label
            {
                Text = "Extension: Waiting for connection...",
                AutoSize = true,
                ForeColor = ToColor(StudioTheme.WarningStatus),
                Margin = new Padding(0, 0, 0, ScaleDpi(4))
            };

            var flpExtBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, ScaleDpi(2))
            };

            btnSwitchTab = CreateToolbarButton("Inject / Switch", padX, padY, btnMargin);
            btnDetachTab = CreateToolbarButton("Detach", padX, padY, btnMargin);
            btnFocusTab = CreateToolbarButton("Focus Tab", padX, padY, btnMargin);
            btnRefreshTabs = CreateToolbarButton("Refresh", padX, padY, btnMargin);

            btnSwitchTab.Click += BtnSwitchTab_Click;
            btnDetachTab.Click += BtnDetachTab_Click;
            btnFocusTab.Click += BtnFocusTab_Click;
            btnRefreshTabs.Click += BtnRefreshTabs_Click;

            flpExtBtns.Controls.AddRange(new Control[]
            {
                btnSwitchTab, btnDetachTab, btnFocusTab, btnRefreshTabs
            });

            chkExclusiveTab = new CheckBox
            {
                Text = "Auto-switch (single active tab)",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, ScaleDpi(2), 0, ScaleDpi(4))
            };
            chkExclusiveTab.CheckedChanged += (s, e) =>
            {
                _session.ExclusiveTabMode = chkExclusiveTab.Checked;
            };

            txtFilterTabs = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                PlaceholderText = "Filter open tabs by title or URL...",
                Margin = new Padding(0)
            };
            txtFilterTabs.TextChanged += (s, e) => RefreshBrowserTabsListUi();

            pnlExtTop.Controls.Add(lblExtensionStatus, 0, 0);
            pnlExtTop.Controls.Add(flpExtBtns, 0, 1);
            pnlExtTop.Controls.Add(chkExclusiveTab, 0, 2);
            pnlExtTop.Controls.Add(txtFilterTabs, 0, 3);

            lstBrowserTabs = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            lstBrowserTabs.SelectedIndexChanged += LstBrowserTabs_SelectedIndexChanged;

            grpExtensionTabs.Controls.Add(lstBrowserTabs);
            grpExtensionTabs.Controls.Add(pnlExtTop);

            pnlClients.Text = "Connected Clients (Active && Legacy TM)";

            splitTop.Panel1.Controls.Remove(pnlClients);
            splitLeftSidebar.Panel1.Controls.Add(grpExtensionTabs);
            splitLeftSidebar.Panel2.Controls.Add(pnlClients);
            splitTop.Panel1.Controls.Add(splitLeftSidebar);
        }

        private void RefreshClientListItemDisplay(BrowserClient client)
        {
            _isUpdatingClientsList = true;
            try
            {
                int idx = _session.RefreshClientInList(
                    lstClients.Items,
                    client,
                    lstClients.SelectedIndex,
                    out bool wasSelected);

                if (idx >= 0 && wasSelected)
                {
                    lstClients.SelectedIndex = idx;
                }
            }
            finally
            {
                _isUpdatingClientsList = false;
            }
        }

        private void UpdateExtensionStatusLabel()
        {
            lblExtensionStatus.Text = _session.GetExtensionStatusMessage();
            lblExtensionStatus.ForeColor = ToColor(_session.GetExtensionStatusColorRgb());
        }

        private void Server_OnExtensionTabsUpdated(object? sender, ExtensionTabsEventArgs e)
        {
            InvokeOnUi(RefreshBrowserTabsListUi);
        }

        private void RefreshBrowserTabsListUi()
        {
            _isUpdatingBrowserTabsList = true;
            lstBrowserTabs.BeginUpdate();
            try
            {
                int? prevSelectedTabId = (lstBrowserTabs.SelectedItem as BrowserTabInfo)?.TabId;
                var filteredTabs = _session.GetExtensionTabs(txtFilterTabs.Text, prevSelectedTabId, out var itemToSelect);

                lstBrowserTabs.Items.Clear();
                foreach (var tab in filteredTabs)
                {
                    lstBrowserTabs.Items.Add(tab);
                }

                if (itemToSelect != null)
                {
                    lstBrowserTabs.SelectedItem = itemToSelect;
                }
            }
            finally
            {
                lstBrowserTabs.EndUpdate();
                _isUpdatingBrowserTabsList = false;
            }
        }

        private async void LstBrowserTabs_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingBrowserTabsList) return;
            if (lstBrowserTabs.SelectedItem is not BrowserTabInfo selectedTab) return;

            await SwitchToBrowserTabAsync(selectedTab, focusBrowserTab: false, skipIfAlreadyActive: true);
        }

        private async void BtnSwitchTab_Click(object? sender, EventArgs e)
        {
            await SwitchToBrowserTabAsync(lstBrowserTabs.SelectedItem as BrowserTabInfo, focusBrowserTab: false, skipIfAlreadyActive: false);
        }

        private async Task SwitchToBrowserTabAsync(BrowserTabInfo? tab, bool focusBrowserTab, bool skipIfAlreadyActive)
        {
            var mode = cmbMode.SelectedItem is IpcMode m ? m : AutomationStudioSession.DefaultIpcMode;
            await _session.SwitchToExtensionTabAsync(tab, mode, focusBrowserTab, skipIfAlreadyActive);
        }

        private async void BtnDetachTab_Click(object? sender, EventArgs e)
        {
            var targetTab = lstBrowserTabs.SelectedItem as BrowserTabInfo
                ?? (_session.GetHookedExtensionTabs().Count > 0 ? _session.GetHookedExtensionTabs()[0] : null);
            await _session.DetachExtensionTabAsync(targetTab);
        }

        private async void BtnFocusTab_Click(object? sender, EventArgs e)
        {
            await _session.FocusExtensionTabAsync(lstBrowserTabs.SelectedItem as BrowserTabInfo);
        }

        private async void BtnRefreshTabs_Click(object? sender, EventArgs e)
        {
            await _session.RefreshExtensionTabsAsync();
        }

        private static bool TrySetSplitterDistance(SplitContainer? splitter, int desiredDistance)
        {
            if (splitter == null)
                return false;

            int totalSize = splitter.Orientation == Orientation.Horizontal
                ? splitter.Height
                : splitter.Width;

            int borderOffset = splitter.BorderStyle == BorderStyle.None ? 0 : 4;
            int minDistance = Math.Max(1, splitter.Panel1MinSize);
            int maxDistance = totalSize - splitter.Panel2MinSize - Math.Max(1, splitter.SplitterWidth) - borderOffset;
            if (maxDistance < minDistance)
                return false;

            splitter.SplitterDistance = Math.Clamp(desiredDistance, minDistance, maxDistance);
            return true;
        }

        private void EnsureTabSplittersConfigured()
        {
            try
            {
                float dpiScale = DeviceDpi / 96f;

                if (!_leftSidebarSplitterInitialized && splitLeftSidebar != null && splitLeftSidebar.Height > 220)
                {
                    _leftSidebarSplitterInitialized = TrySetSplitterDistance(
                        splitLeftSidebar,
                        StudioLayoutMetrics.CalculateLeftSidebarSplitterDistance(splitLeftSidebar.Height, dpiScale));
                }

                if (tabMain.SelectedTab == tabDom && !_domSplittersInitialized && splitDom.Width > 200)
                {
                    _domSplittersInitialized = TrySetSplitterDistance(
                        splitDom,
                        StudioLayoutMetrics.CalculateDomSplitterDistance(splitDom.Width));
                }
                else if (tabMain.SelectedTab == tabEditor && !_editorSplittersInitialized && splitSequences.Width > 300)
                {
                    if (TrySetSplitterDistance(
                            splitSequences,
                            StudioLayoutMetrics.CalculateSequencesSplitterDistance(splitSequences.Width, dpiScale)) &&
                        splitEditor.Width > 250 &&
                        TrySetSplitterDistance(
                            splitEditor,
                            StudioLayoutMetrics.CalculateEditorSplitterDistance(splitEditor.Width, dpiScale)))
                    {
                        _editorSplittersInitialized = true;
                    }
                }
                else if (tabMain.SelectedTab == tabScraper && !_scraperSplittersInitialized && splitScraperMain != null && splitScraperMain.Width > 300)
                {
                    if (TrySetSplitterDistance(
                            splitScraperMain,
                            StudioLayoutMetrics.CalculateScraperMainSplitterDistance(splitScraperMain.Width, dpiScale)) &&
                        splitScraperRight.Height > 200 &&
                        TrySetSplitterDistance(
                            splitScraperRight,
                            StudioLayoutMetrics.CalculateScraperRightSplitterDistance(splitScraperRight.Height, dpiScale)) &&
                        splitFields.Width > 250 &&
                        TrySetSplitterDistance(
                            splitFields,
                            StudioLayoutMetrics.CalculateFieldsSplitterDistance(splitFields.Width, dpiScale)))
                    {
                        _scraperSplittersInitialized = true;
                    }
                }
                else if (tabMain.SelectedTab == tabReverseEngineering && !_revEngSplittersInitialized && splitRevEngMain != null && splitRevEngMain.Width > 300)
                {
                    if (TrySetSplitterDistance(
                            splitRevEngMain,
                            StudioLayoutMetrics.CalculateReverseEngineeringMainSplitterDistance(splitRevEngMain.Width, dpiScale)) &&
                        splitRevEngLeft.Height > 200 &&
                        TrySetSplitterDistance(
                            splitRevEngLeft,
                            StudioLayoutMetrics.CalculateReverseEngineeringLeftSplitterDistance(splitRevEngLeft.Height, dpiScale)))
                    {
                        _revEngSplittersInitialized = true;
                    }
                }
            }
            catch
            {
                // Ignore transient layout bounds exceptions during window initialization
            }
        }

        private void InitializeScraperTabUi()
        {
            var darkBg = ToColor(StudioTheme.DarkBackground);
            var darkLine = ToColor(StudioTheme.DarkSurface);

            int padX = ScaleDpi(6);
            int padY = ScaleDpi(3);
            int btnMargin = ScaleDpi(3);

            splitScraperMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Size = new Size(920, 440)
            };

            // Left Panel: Scrape Lists & Scrape-Level Options
            var pnlScrapesTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(ScaleDpi(4))
            };

            var btnAddScrape = CreateToolbarButton("Add Scrape", padX, padY, btnMargin);
            var btnRemoveScrape = CreateToolbarButton("Remove", padX, padY, btnMargin);
            var btnMoveScrapeUp = CreateToolbarButton("Move Up", padX, padY, btnMargin);
            var btnMoveScrapeDown = CreateToolbarButton("Move Down", padX, padY, btnMargin);
            var btnRenameScrape = CreateToolbarButton("Rename", padX, padY, btnMargin);

            btnAddScrape.Click += BtnAddScrape_Click;
            btnRemoveScrape.Click += BtnRemoveScrape_Click;
            btnMoveScrapeUp.Click += BtnMoveScrapeUp_Click;
            btnMoveScrapeDown.Click += BtnMoveScrapeDown_Click;
            btnRenameScrape.Click += BtnRenameScrape_Click;

            pnlScrapesTop.Controls.AddRange(new Control[]
            {
                btnAddScrape, btnRemoveScrape, btnMoveScrapeUp, btnMoveScrapeDown, btnRenameScrape
            });

            chkLstScrapes = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            chkLstScrapes.SelectedIndexChanged += ChkLstScrapes_SelectedIndexChanged;
            chkLstScrapes.ItemCheck += ChkLstScrapes_ItemCheck;

            var grpScrapeConfig = new GroupBox
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Text = "Container && Automation Hooks",
                Padding = new Padding(ScaleDpi(8), ScaleDpi(6), ScaleDpi(8), ScaleDpi(8)),
                ForeColor = ToColor(StudioTheme.Foreground)
            };

            var tlpConfig = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 8
            };
            tlpConfig.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 8; i++)
            {
                tlpConfig.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            Padding lblMargin = new Padding(0, ScaleDpi(4), ScaleDpi(6), ScaleDpi(4));
            Padding inputMargin = new Padding(0, ScaleDpi(3), 0, ScaleDpi(3));

            var lblContainer = new Label { Text = "Container CSS:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = lblMargin };
            txtContainerSelector = new TextBox { Dock = DockStyle.Fill, BackColor = darkBg, ForeColor = ToColor(StudioTheme.Foreground), Margin = inputMargin };
            txtContainerSelector.TextChanged += ScrapeConfigControl_Changed;

            var pnlContainerBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Margin = new Padding(0, ScaleDpi(2), 0, ScaleDpi(4))
            };
            var btnGenContainer = CreateToolbarButton("Generalize", ScaleDpi(5), ScaleDpi(2), ScaleDpi(2));
            var btnHlContainer = CreateToolbarButton("Highlight", ScaleDpi(5), ScaleDpi(2), ScaleDpi(2));
            var btnClrContainer = CreateToolbarButton("Clear", ScaleDpi(5), ScaleDpi(2), ScaleDpi(2));
            btnGenContainer.Click += BtnGeneralizeContainer_Click;
            btnHlContainer.Click += BtnHighlightContainer_Click;
            btnClrContainer.Click += (s, e) => txtContainerSelector.Text = string.Empty;
            pnlContainerBtns.Controls.AddRange(new Control[] { btnGenContainer, btnHlContainer, btnClrContainer });

            var lblPreSeq = new Label { Text = "Pre-Automation:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = lblMargin };
            cmbPreScrapeSeq = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                Margin = inputMargin
            };
            cmbPreScrapeSeq.SelectedIndexChanged += ScrapeConfigControl_Changed;

            var lblPostSeq = new Label { Text = "Post-Automation:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = lblMargin };
            cmbPostScrapeSeq = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                Margin = inputMargin
            };
            cmbPostScrapeSeq.SelectedIndexChanged += ScrapeConfigControl_Changed;

            chkEnablePagination = new CheckBox
            {
                Text = "Paginate (Max):",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = lblMargin,
                Checked = ScrapeDefinition.Default.EnablePagination
            };
            chkEnablePagination.CheckedChanged += ScrapeConfigControl_Changed;

            var pnlPageNums = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = inputMargin
            };

            numMaxPages = new NumericUpDown
            {
                Minimum = ScrapeDefinition.MinMaxPages,
                Maximum = ScrapeDefinition.MaxMaxPages,
                Value = ScrapeDefinition.Default.MaxPages,
                Width = ScaleDpi(75),
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                Margin = new Padding(0, 0, ScaleDpi(8), 0)
            };
            numMaxPages.ValueChanged += ScrapeConfigControl_Changed;

            var lblStartPage = new Label
            {
                Text = "Start:",
                AutoSize = true,
                Margin = new Padding(0, ScaleDpi(4), ScaleDpi(4), 0)
            };

            numStartPage = new NumericUpDown
            {
                Minimum = ScrapeDefinition.MinPaginationStartPage,
                Maximum = ScrapeDefinition.MaxPaginationStartPage,
                Value = ScrapeDefinition.Default.PaginationStartPage,
                Width = ScaleDpi(70),
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                Margin = new Padding(0)
            };
            numStartPage.ValueChanged += ScrapeConfigControl_Changed;

            pnlPageNums.Controls.AddRange(new Control[] { numMaxPages, lblStartPage, numStartPage });

            var lblPageDelay = new Label { Text = "Page Delay (ms):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = lblMargin };
            numPageDelayMs = new NumericUpDown
            {
                Minimum = ScrapeDefinition.MinPageDelayMs,
                Maximum = ScrapeDefinition.MaxPageDelayMs,
                Increment = 250,
                Value = ScrapeDefinition.Default.PageDelayMs,
                Width = ScaleDpi(100),
                Anchor = AnchorStyles.Left,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                Margin = inputMargin
            };
            numPageDelayMs.ValueChanged += ScrapeConfigControl_Changed;

            var lblNextBtn = new Label { Text = "Next Button CSS:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = lblMargin };
            txtNextPageSelector = new TextBox { Dock = DockStyle.Fill, BackColor = darkBg, ForeColor = ToColor(StudioTheme.Foreground), Margin = inputMargin };
            txtNextPageSelector.TextChanged += ScrapeConfigControl_Changed;

            var lblPageUrlPattern = new Label { Text = "Page URL Pattern:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = lblMargin };
            txtPaginationUrlPattern = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                PlaceholderText = "https://site.com/items?page={page}",
                Margin = inputMargin
            };
            txtPaginationUrlPattern.TextChanged += ScrapeConfigControl_Changed;

            tlpConfig.Controls.Add(lblContainer, 0, 0);
            tlpConfig.Controls.Add(txtContainerSelector, 1, 0);
            tlpConfig.Controls.Add(pnlContainerBtns, 1, 1);
            tlpConfig.Controls.Add(lblPreSeq, 0, 2);
            tlpConfig.Controls.Add(cmbPreScrapeSeq, 1, 2);
            tlpConfig.Controls.Add(lblPostSeq, 0, 3);
            tlpConfig.Controls.Add(cmbPostScrapeSeq, 1, 3);
            tlpConfig.Controls.Add(chkEnablePagination, 0, 4);
            tlpConfig.Controls.Add(pnlPageNums, 1, 4);
            tlpConfig.Controls.Add(lblPageDelay, 0, 5);
            tlpConfig.Controls.Add(numPageDelayMs, 1, 5);
            tlpConfig.Controls.Add(lblNextBtn, 0, 6);
            tlpConfig.Controls.Add(txtNextPageSelector, 1, 6);
            tlpConfig.Controls.Add(lblPageUrlPattern, 0, 7);
            tlpConfig.Controls.Add(txtPaginationUrlPattern, 1, 7);

            grpScrapeConfig.Controls.Add(tlpConfig);

            splitScraperMain.Panel1.Controls.Add(chkLstScrapes);
            splitScraperMain.Panel1.Controls.Add(pnlScrapesTop);
            splitScraperMain.Panel1.Controls.Add(grpScrapeConfig);

            // Right Panel: Top = Fields & PropertyGrid, Bottom = Results & Preview
            splitScraperRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Size = new Size(600, 440)
            };

            var pnlFieldsToolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(ScaleDpi(4))
            };
            pnlFieldsToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlFieldsToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlFieldsToolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var flpFieldActionsLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Margin = new Padding(0)
            };

            var flpFieldActionsRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var btnAddField = CreateToolbarButton("Add Field", padX, padY, btnMargin);
            var btnRemoveField = CreateToolbarButton("Remove", padX, padY, btnMargin);
            var btnFieldUp = CreateToolbarButton("Move Up", padX, padY, btnMargin);
            var btnFieldDown = CreateToolbarButton("Move Down", padX, padY, btnMargin);
            var btnGenField = CreateToolbarButton("Generalize", padX, padY, btnMargin);
            var btnHlField = CreateToolbarButton("Highlight", padX, padY, btnMargin);

            btnPreviewScrape = CreateToolbarButton("Preview Scrape", padX, padY, btnMargin);
            btnRunAllScrapes = CreateToolbarButton("Run Active Scrapes", padX, padY, btnMargin);

            btnAddField.Click += BtnAddScrapeField_Click;
            btnRemoveField.Click += BtnRemoveScrapeField_Click;
            btnFieldUp.Click += BtnMoveScrapeFieldUp_Click;
            btnFieldDown.Click += BtnMoveScrapeFieldDown_Click;
            btnGenField.Click += BtnGeneralizeScrapeField_Click;
            btnHlField.Click += BtnHighlightScrapeField_Click;
            btnPreviewScrape.Click += BtnPreviewScrape_Click;
            btnRunAllScrapes.Click += BtnRunAllScrapes_Click;

            flpFieldActionsLeft.Controls.AddRange(new Control[]
            {
                btnAddField, btnRemoveField, btnFieldUp, btnFieldDown, btnGenField, btnHlField
            });
            flpFieldActionsRight.Controls.AddRange(new Control[]
            {
                btnPreviewScrape, btnRunAllScrapes
            });

            pnlFieldsToolbar.Controls.Add(flpFieldActionsLeft, 0, 0);
            pnlFieldsToolbar.Controls.Add(flpFieldActionsRight, 1, 0);

            splitFields = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Size = new Size(600, 220)
            };

            lstScrapeFields = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            lstScrapeFields.SelectedIndexChanged += LstScrapeFields_SelectedIndexChanged;

            pgScrapeFieldDetails = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                BackColor = darkBg,
                CategoryForeColor = ToColor(StudioTheme.Foreground),
                CategorySplitterColor = darkLine,
                CommandsBorderColor = darkLine,
                CommandsForeColor = ToColor(StudioTheme.Foreground),
                DisabledItemForeColor = ToColor(StudioTheme.DisabledText),
                HelpBackColor = darkBg,
                HelpBorderColor = darkLine,
                HelpForeColor = ToColor(StudioTheme.Foreground),
                LineColor = darkLine,
                ViewBackColor = darkBg,
                ViewBorderColor = darkLine,
                ViewForeColor = ToColor(StudioTheme.Foreground)
            };
            pgScrapeFieldDetails.PropertyValueChanged += PgScrapeFieldDetails_PropertyValueChanged;

            splitFields.Panel1.Controls.Add(lstScrapeFields);
            splitFields.Panel2.Controls.Add(pgScrapeFieldDetails);

            splitScraperRight.Panel1.Controls.Add(splitFields);
            splitScraperRight.Panel1.Controls.Add(pnlFieldsToolbar);

            // Bottom Panel: Results Preview & Export
            var pnlResultsToolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(ScaleDpi(4))
            };
            pnlResultsToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlResultsToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlResultsToolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblScrapeStatus = new Label
            {
                Text = "Scrape Results Preview (0 records)",
                AutoSize = false,
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(ScaleDpi(4), 0, ScaleDpi(8), 0)
            };

            var flpExportBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var btnCopyJson = CreateToolbarButton("Copy JSON", padX, padY, btnMargin);
            var btnExportCsv = CreateToolbarButton("Export CSV...", padX, padY, btnMargin);
            var btnExportJson = CreateToolbarButton("Export JSON...", padX, padY, btnMargin);

            btnExportJson.Click += BtnExportScrapeJson_Click;
            btnExportCsv.Click += BtnExportScrapeCsv_Click;
            btnCopyJson.Click += BtnCopyScrapeJson_Click;

            flpExportBtns.Controls.AddRange(new Control[] { btnCopyJson, btnExportCsv, btnExportJson });

            pnlResultsToolbar.Controls.Add(lblScrapeStatus, 0, 0);
            pnlResultsToolbar.Controls.Add(flpExportBtns, 1, 0);

            var tabPreview = new TabControl { Dock = DockStyle.Fill };
            var tabTable = new TabPage("Table View") { BackColor = darkBg };
            var tabJson = new TabPage("JSON View") { BackColor = darkBg };

            dgvScrapePreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                GridColor = darkLine,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvScrapePreview.ColumnHeadersDefaultCellStyle.BackColor = ToColor(StudioTheme.DarkSurface);
            dgvScrapePreview.ColumnHeadersDefaultCellStyle.ForeColor = ToColor(StudioTheme.BrightForeground);
            dgvScrapePreview.DefaultCellStyle.BackColor = darkBg;
            dgvScrapePreview.DefaultCellStyle.ForeColor = ToColor(StudioTheme.Foreground);
            dgvScrapePreview.DefaultCellStyle.SelectionBackColor = ToColor(StudioTheme.SelectionBackground);
            dgvScrapePreview.DefaultCellStyle.SelectionForeColor = ToColor(StudioTheme.BrightForeground);

            txtScrapeJsonPreview = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                Font = new Font("Consolas", 10F)
            };

            tabTable.Controls.Add(dgvScrapePreview);
            tabJson.Controls.Add(txtScrapeJsonPreview);
            tabPreview.Controls.Add(tabTable);
            tabPreview.Controls.Add(tabJson);

            splitScraperRight.Panel2.Controls.Add(tabPreview);
            splitScraperRight.Panel2.Controls.Add(pnlResultsToolbar);

            splitScraperMain.Panel2.Controls.Add(splitScraperRight);
            tabScraper.Controls.Add(splitScraperMain);

            TrySetSplitterDistance(splitScraperMain, 300);
            TrySetSplitterDistance(splitScraperRight, 220);
            TrySetSplitterDistance(splitFields, 210);

            RefreshAutomationSequenceDropdowns();
        }

        private void RefreshAutomationSequenceDropdowns()
        {
            if (cmbPreScrapeSeq == null || cmbPostScrapeSeq == null) return;

            _isUpdatingScrapeUi = true;
            try
            {
                string prevPre = cmbPreScrapeSeq.SelectedItem as string ?? string.Empty;
                string prevPost = cmbPostScrapeSeq.SelectedItem as string ?? string.Empty;

                cmbPreScrapeSeq.Items.Clear();
                cmbPostScrapeSeq.Items.Clear();

                foreach (string option in _session.Project.GetSequenceDropdownOptions())
                {
                    cmbPreScrapeSeq.Items.Add(option);
                    cmbPostScrapeSeq.Items.Add(option);
                }

                _session.Project.GetSequenceDropdownState(
                    chkLstScrapes?.SelectedItem as ScrapeDefinition,
                    prevPre,
                    prevPost,
                    out string resolvedPre,
                    out string resolvedPost);

                cmbPreScrapeSeq.SelectedItem = resolvedPre;
                cmbPostScrapeSeq.SelectedItem = resolvedPost;
            }
            finally
            {
                _isUpdatingScrapeUi = false;
            }
        }

        private void LoadSelectedScrapeToUi()
        {
            _isUpdatingScrapeUi = true;
            try
            {
                lstScrapeFields.Items.Clear();
                pgScrapeFieldDetails.SelectedObject = null;

                ScrapeDefinition scrape = chkLstScrapes.SelectedItem as ScrapeDefinition ?? ScrapeDefinition.Default;
                txtContainerSelector.Text = scrape.ContainerSelector;
                cmbPreScrapeSeq.SelectedItem = _session.Project.ResolveSequenceDropdownSelection(scrape.PreScrapeSequenceName);
                cmbPostScrapeSeq.SelectedItem = _session.Project.ResolveSequenceDropdownSelection(scrape.PostScrapeSequenceName);
                chkEnablePagination.Checked = scrape.EnablePagination;
                numMaxPages.Value = ScrapeDefinition.ClampMaxPages(scrape.MaxPages);
                numStartPage.Value = ScrapeDefinition.ClampPaginationStartPage(scrape.PaginationStartPage);
                numPageDelayMs.Value = ScrapeDefinition.ClampPageDelayMs(scrape.PageDelayMs);
                txtNextPageSelector.Text = scrape.NextPageSelector;
                txtPaginationUrlPattern.Text = scrape.PaginationUrlPattern;

                foreach (var field in scrape.Fields)
                {
                    lstScrapeFields.Items.Add(field);
                }

                if (lstScrapeFields.Items.Count > 0)
                {
                    lstScrapeFields.SelectedIndex = 0;
                }
            }
            finally
            {
                _isUpdatingScrapeUi = false;
            }
        }

        private void ScrapeConfigControl_Changed(object? sender, EventArgs e)
        {
            if (_isUpdatingScrapeUi) return;
            if (chkLstScrapes.SelectedItem is not ScrapeDefinition scrape) return;

            _session.UpdateScrapeSettings(
                scrape,
                txtContainerSelector.Text,
                cmbPreScrapeSeq.SelectedItem as string,
                cmbPostScrapeSeq.SelectedItem as string,
                chkEnablePagination.Checked,
                (int)numMaxPages.Value,
                (int)numStartPage.Value,
                (int)numPageDelayMs.Value,
                txtNextPageSelector.Text,
                txtPaginationUrlPattern.Text);
        }

        private void ChkLstScrapes_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadSelectedScrapeToUi();
        }

        private void ChkLstScrapes_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (chkLstScrapes.Items[e.Index] is ScrapeDefinition scrape)
            {
                _session.SetScrapeActive(scrape, e.NewValue == CheckState.Checked);
            }
        }

        private void BtnAddScrape_Click(object? sender, EventArgs e)
        {
            string name = PromptForValue("Enter new scrape list name:", "Add Scrape List", _session.Project.GetSuggestedScrapeName());
            var scrape = _session.TryAddScrape(name);
            if (scrape != null)
            {
                chkLstScrapes.Items.Add(scrape, scrape.IsActive);
                chkLstScrapes.SelectedItem = scrape;
            }
        }

        private void BtnRemoveScrape_Click(object? sender, EventArgs e)
        {
            if (chkLstScrapes.SelectedItem is ScrapeDefinition scrape)
            {
                _session.RemoveScrape(scrape);
                chkLstScrapes.Items.Remove(scrape);
                LoadSelectedScrapeToUi();
            }
        }

        private void BtnMoveScrapeUp_Click(object? sender, EventArgs e)
        {
            int idx = chkLstScrapes.SelectedIndex;
            if (idx < 0) return;
            bool isChecked = chkLstScrapes.GetItemChecked(idx);
            if (_session.MoveScrapeUp(chkLstScrapes.Items, idx, out int newIndex))
            {
                chkLstScrapes.SetItemChecked(newIndex, isChecked);
                chkLstScrapes.SelectedIndex = newIndex;
            }
        }

        private void BtnMoveScrapeDown_Click(object? sender, EventArgs e)
        {
            int idx = chkLstScrapes.SelectedIndex;
            if (idx < 0) return;
            bool isChecked = chkLstScrapes.GetItemChecked(idx);
            if (_session.MoveScrapeDown(chkLstScrapes.Items, idx, out int newIndex))
            {
                chkLstScrapes.SetItemChecked(newIndex, isChecked);
                chkLstScrapes.SelectedIndex = newIndex;
            }
        }

        private void BtnRenameScrape_Click(object? sender, EventArgs e)
        {
            if (chkLstScrapes.SelectedItem is ScrapeDefinition scrape)
            {
                string name = PromptForValue("Enter new scrape list name:", "Rename Scrape List", scrape.Name);
                if (_session.RenameScrape(scrape, name))
                {
                    int idx = chkLstScrapes.SelectedIndex;
                    bool isChecked = chkLstScrapes.GetItemChecked(idx);
                    _session.RefreshItemInList(chkLstScrapes.Items, idx);
                    chkLstScrapes.SetItemChecked(idx, isChecked);
                }
            }
        }

        private void BtnGeneralizeContainer_Click(object? sender, EventArgs e)
        {
            if (_session.TryGeneralizeContainerSelector(
                    txtContainerSelector.Text,
                    chkLstScrapes.SelectedItem as ScrapeDefinition,
                    tvDom.RootDomNode,
                    out string generalizedSelector))
            {
                txtContainerSelector.Text = generalizedSelector;
                pgScrapeFieldDetails.Refresh();
            }
        }

        private async void BtnHighlightContainer_Click(object? sender, EventArgs e)
        {
            await _session.HighlightContainerAsync(
                txtContainerSelector.Text,
                chkLstScrapes.SelectedItem as ScrapeDefinition);
        }

        private void BtnAddScrapeField_Click(object? sender, EventArgs e)
        {
            var field = _session.AddFieldToScrape(chkLstScrapes.SelectedItem as ScrapeDefinition);
            if (field != null)
            {
                lstScrapeFields.Items.Add(field);
                lstScrapeFields.SelectedItem = field;
            }
        }

        private void BtnRemoveScrapeField_Click(object? sender, EventArgs e)
        {
            if (chkLstScrapes.SelectedItem is ScrapeDefinition scrape && lstScrapeFields.SelectedItem is ScrapeField field)
            {
                _session.RemoveFieldFromScrape(scrape, field);
                lstScrapeFields.Items.Remove(field);
                pgScrapeFieldDetails.SelectedObject = lstScrapeFields.SelectedItem;
            }
        }

        private void BtnMoveScrapeFieldUp_Click(object? sender, EventArgs e)
        {
            if (chkLstScrapes.SelectedItem is ScrapeDefinition scrape && lstScrapeFields.SelectedItem is ScrapeField)
            {
                int idx = lstScrapeFields.SelectedIndex;
                if (_session.MoveFieldUp(scrape, lstScrapeFields.Items, idx, out int newIndex))
                {
                    lstScrapeFields.SelectedIndex = newIndex;
                }
            }
        }

        private void BtnMoveScrapeFieldDown_Click(object? sender, EventArgs e)
        {
            if (chkLstScrapes.SelectedItem is ScrapeDefinition scrape && lstScrapeFields.SelectedItem is ScrapeField)
            {
                int idx = lstScrapeFields.SelectedIndex;
                if (_session.MoveFieldDown(scrape, lstScrapeFields.Items, idx, out int newIndex))
                {
                    lstScrapeFields.SelectedIndex = newIndex;
                }
            }
        }

        private void BtnGeneralizeScrapeField_Click(object? sender, EventArgs e)
        {
            if (_session.GeneralizeScrapeFieldSelector(lstScrapeFields.SelectedItem as ScrapeField, tvDom.RootDomNode))
            {
                pgScrapeFieldDetails.Refresh();
                RefreshSelectedFieldItemText();
            }
        }

        private async void BtnHighlightScrapeField_Click(object? sender, EventArgs e)
        {
            await _session.HighlightScrapeFieldAsync(
                lstScrapeFields.SelectedItem as ScrapeField,
                chkLstScrapes.SelectedItem as ScrapeDefinition);
        }

        private void LstScrapeFields_SelectedIndexChanged(object? sender, EventArgs e)
        {
            pgScrapeFieldDetails.SelectedObject = lstScrapeFields.SelectedItem;
        }

        private void PgScrapeFieldDetails_PropertyValueChanged(object? s, PropertyValueChangedEventArgs e)
        {
            RefreshSelectedFieldItemText();
        }

        private void RefreshSelectedFieldItemText()
        {
            _session.RefreshItemInList(lstScrapeFields.Items, lstScrapeFields.SelectedIndex);
        }

        private async void MnuQuickScrapeText_Click(object? sender, EventArgs e)
        {
            await _session.QuickScrapeDomNodeAsync(tvDom.SelectedDomNode);
        }

        private void AddFieldFromSelectedDomNode(ScrapeDefinition scrape, bool matchAllElements)
        {
            var field = _session.AddFieldFromDomNode(scrape, tvDom.SelectedDomNode, matchAllElements, tvDom.RootDomNode);
            if (field == null) return;

            if (chkLstScrapes.SelectedItem != scrape)
            {
                chkLstScrapes.SelectedItem = scrape;
            }
            else
            {
                lstScrapeFields.Items.Add(field);
                lstScrapeFields.SelectedItem = field;
            }
        }

        private void SetContainerFromSelectedDomNode(ScrapeDefinition scrape)
        {
            if (_session.SetContainerFromDomNode(scrape, tvDom.SelectedDomNode, tvDom.RootDomNode) == null)
                return;

            if (chkLstScrapes.SelectedItem != scrape)
            {
                chkLstScrapes.SelectedItem = scrape;
            }
            else
            {
                LoadSelectedScrapeToUi();
            }
        }

        private void SetPaginationNextFromSelectedDomNode(ScrapeDefinition scrape)
        {
            if (_session.SetPaginationNextFromDomNode(scrape, tvDom.SelectedDomNode, tvDom.RootDomNode) == null)
                return;

            if (chkLstScrapes.SelectedItem != scrape)
            {
                chkLstScrapes.SelectedItem = scrape;
            }
            else
            {
                LoadSelectedScrapeToUi();
            }
        }

        private async void BtnPreviewScrape_Click(object? sender, EventArgs e)
        {
            btnPreviewScrape.Enabled = false;
            try
            {
                await _session.PreviewScrapeAsync(
                    chkLstScrapes.SelectedItem as ScrapeDefinition);
            }
            finally
            {
                btnPreviewScrape.Enabled = true;
            }
        }

        private async void BtnRunAllScrapes_Click(object? sender, EventArgs e)
        {
            btnRunAllScrapes.Enabled = false;
            try
            {
                await _session.RunActiveScrapesAsync();
            }
            finally
            {
                btnRunAllScrapes.Enabled = true;
            }
        }

        private void DisplayScrapeResult(ScrapeResult result)
        {
            lblScrapeStatus.Text = result.GetStatusSummary();
            dgvScrapePreview.DataSource = result.ToDataTable();
            txtScrapeJsonPreview.Text = result.ToJson(true, true);
        }

        private void BtnCopyScrapeJson_Click(object? sender, EventArgs e)
        {
            string? json = _session.GetLastScrapeJsonForCopy();
            if (json != null)
            {
                Clipboard.SetText(json);
            }
        }

        private void BtnExportScrapeJson_Click(object? sender, EventArgs e)
        {
            if (!_session.TryGetLastScrapeSuggestedFileName(ScrapeResult.JsonFileExtension, out string suggestedFileName))
                return;

            using var sfd = new SaveFileDialog
            {
                Filter = AutomationStudioSession.ScrapeJsonFileDialogFilter,
                Title = "Export Scrape Results as JSON",
                DefaultExt = ScrapeResult.JsonFileExtension,
                FileName = suggestedFileName
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _session.ExportLastScrapeToJsonFile(sfd.FileName);
            }
        }

        private void BtnExportScrapeCsv_Click(object? sender, EventArgs e)
        {
            if (!_session.TryGetLastScrapeSuggestedFileName(ScrapeResult.CsvFileExtension, out string suggestedFileName))
                return;

            using var sfd = new SaveFileDialog
            {
                Filter = AutomationStudioSession.ScrapeCsvFileDialogFilter,
                Title = "Export Scrape Results as CSV",
                DefaultExt = ScrapeResult.CsvFileExtension,
                FileName = suggestedFileName
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _session.ExportLastScrapeToCsvFile(sfd.FileName);
            }
        }

        // --- Reverse Engineering Tab UI & API Wrappers ---

        private void InitializeReverseEngineeringTabUi()
        {
            var darkBg = ToColor(StudioTheme.DarkBackground);

            int padX = ScaleDpi(8);
            int padY = ScaleDpi(4);
            int btnMargin = ScaleDpi(4);

            // Top Action Bar containing the 4 Dump Buttons + Options + Export Buttons
            var pnlTopBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(ScaleDpi(4))
            };
            pnlTopBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlTopBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlTopBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlTopBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var flpDumpButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Margin = new Padding(0)
            };

            btnDumpHtml = CreateToolbarButton("Dump HTML", padX, padY, btnMargin);
            btnDumpCss = CreateToolbarButton("Dump CSS", padX, padY, btnMargin);
            btnDumpJs = CreateToolbarButton("Dump JavaScript", padX, padY, btnMargin);
            btnDumpAll = CreateToolbarButton("Dump Everything", padX, padY, btnMargin);

            btnDumpHtml.Click += async (s, e) => await ExecuteSiteDumpFromUiAsync(SiteDumpType.Html);
            btnDumpCss.Click += async (s, e) => await ExecuteSiteDumpFromUiAsync(SiteDumpType.Css);
            btnDumpJs.Click += async (s, e) => await ExecuteSiteDumpFromUiAsync(SiteDumpType.JavaScript);
            btnDumpAll.Click += async (s, e) => await ExecuteSiteDumpFromUiAsync(SiteDumpType.All);

            flpDumpButtons.Controls.AddRange(new Control[]
            {
                btnDumpHtml, btnDumpCss, btnDumpJs, btnDumpAll
            });

            var flpExportActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var btnCopyCurrentView = CreateToolbarButton("Copy Active View", ScaleDpi(6), ScaleDpi(3), btnMargin);
            var btnSaveSelectedFile = CreateToolbarButton("Save Active File...", ScaleDpi(6), ScaleDpi(3), btnMargin);
            var btnSaveDumpFolder = CreateToolbarButton("Save Dump to Folder...", ScaleDpi(6), ScaleDpi(3), btnMargin);

            btnCopyCurrentView.Click += BtnCopyCurrentDumpView_Click;
            btnSaveSelectedFile.Click += BtnSaveActiveDumpFile_Click;
            btnSaveDumpFolder.Click += BtnSaveDumpToFolder_Click;

            flpExportActions.Controls.AddRange(new Control[]
            {
                btnCopyCurrentView, btnSaveSelectedFile, btnSaveDumpFolder
            });

            var flpOptionsRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Margin = new Padding(ScaleDpi(2), ScaleDpi(2), 0, 0)
            };

            chkDumpExternal = new CheckBox
            {
                Text = "Include External Assets",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, ScaleDpi(2), ScaleDpi(12), ScaleDpi(2))
            };
            chkDumpInline = new CheckBox
            {
                Text = "Include Inline <style> / <script>",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, ScaleDpi(2), ScaleDpi(12), ScaleDpi(2))
            };
            chkDumpDynamicJs = new CheckBox
            {
                Text = "Include Dynamic JS Resources",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, ScaleDpi(2), ScaleDpi(12), ScaleDpi(2))
            };

            flpOptionsRow.Controls.AddRange(new Control[]
            {
                chkDumpExternal, chkDumpInline, chkDumpDynamicJs
            });

            lblDumpStatus = new Label
            {
                Text = "Ready — Connect a browser client and click one of the 4 dump buttons.",
                AutoSize = false,
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = ToColor(StudioTheme.Foreground),
                Margin = new Padding(ScaleDpi(4), 0, ScaleDpi(4), 0)
            };

            pnlTopBar.Controls.Add(flpDumpButtons, 0, 0);
            pnlTopBar.Controls.Add(flpExportActions, 1, 0);
            pnlTopBar.Controls.Add(flpOptionsRow, 0, 1);
            pnlTopBar.Controls.Add(lblDumpStatus, 1, 1);

            // Main Split Container: Left = Resource Explorer & Metadata, Right = Source Viewers
            splitRevEngMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Size = new Size(920, 380)
            };

            splitRevEngLeft = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Size = new Size(300, 380)
            };

            var pnlFilterTop = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, ScaleDpi(4))
            };
            pnlFilterTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlFilterTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlFilterTop.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblFilter = new Label
            {
                Text = "Filter:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, ScaleDpi(6), 0)
            };
            txtFilterResources = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                PlaceholderText = "Filter by name, type, URL, or code...",
                Margin = new Padding(0)
            };
            txtFilterResources.TextChanged += (s, e) => RefreshDumpedResourcesList();

            pnlFilterTop.Controls.Add(lblFilter, 0, 0);
            pnlFilterTop.Controls.Add(txtFilterResources, 1, 0);

            lstDumpedResources = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            lstDumpedResources.SelectedIndexChanged += LstDumpedResources_SelectedIndexChanged;

            txtDumpedResourceDetails = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                Font = new Font("Consolas", 9.5F)
            };

            splitRevEngLeft.Panel1.Controls.Add(lstDumpedResources);
            splitRevEngLeft.Panel1.Controls.Add(pnlFilterTop);
            splitRevEngLeft.Panel2.Controls.Add(txtDumpedResourceDetails);

            tabDumpViewers = new TabControl { Dock = DockStyle.Fill };
            tabDumpViewers.SelectedIndexChanged += (s, e) => _session.ActiveDumpViewMode = GetActiveDumpViewMode();
            tabSelectedResourceView = new TabPage("Selected Resource") { BackColor = darkBg };
            tabCombinedHtmlView = new TabPage("Combined HTML") { BackColor = darkBg };
            tabCombinedCssView = new TabPage("Combined CSS") { BackColor = darkBg };
            tabCombinedJsView = new TabPage("Combined JavaScript") { BackColor = darkBg };

            txtSelectedResourceSource = CreateCodeViewerTextBox(darkBg);
            txtCombinedHtmlSource = CreateCodeViewerTextBox(darkBg);
            txtCombinedCssSource = CreateCodeViewerTextBox(darkBg);
            txtCombinedJsSource = CreateCodeViewerTextBox(darkBg);

            tabSelectedResourceView.Controls.Add(txtSelectedResourceSource);
            tabCombinedHtmlView.Controls.Add(txtCombinedHtmlSource);
            tabCombinedCssView.Controls.Add(txtCombinedCssSource);
            tabCombinedJsView.Controls.Add(txtCombinedJsSource);

            tabDumpViewers.Controls.Add(tabSelectedResourceView);
            tabDumpViewers.Controls.Add(tabCombinedHtmlView);
            tabDumpViewers.Controls.Add(tabCombinedCssView);
            tabDumpViewers.Controls.Add(tabCombinedJsView);

            splitRevEngMain.Panel1.Controls.Add(splitRevEngLeft);
            splitRevEngMain.Panel2.Controls.Add(tabDumpViewers);

            tabReverseEngineering.Controls.Add(splitRevEngMain);
            tabReverseEngineering.Controls.Add(pnlTopBar);

            TrySetSplitterDistance(splitRevEngMain, 300);
            TrySetSplitterDistance(splitRevEngLeft, 240);
        }

        private static TextBox CreateCodeViewerTextBox(Color darkBg)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                BackColor = darkBg,
                ForeColor = ToColor(StudioTheme.Foreground),
                Font = new Font("Consolas", 10F),
                MaxLength = 0
            };
        }

        private void SetDumpButtonsEnabled(bool enabled)
        {
            btnDumpHtml.Enabled = enabled;
            btnDumpCss.Enabled = enabled;
            btnDumpJs.Enabled = enabled;
            btnDumpAll.Enabled = enabled;
        }

        private async Task ExecuteSiteDumpFromUiAsync(SiteDumpType dumpType)
        {
            SetDumpButtonsEnabled(false);
            try
            {
                _session.SetDumpOptions(
                    includeExternal: chkDumpExternal.Checked,
                    includeInline: chkDumpInline.Checked,
                    includeDynamicScripts: chkDumpDynamicJs.Checked);

                await _session.ExecuteSiteDumpAsync(dumpType);
            }
            finally
            {
                SetDumpButtonsEnabled(true);
            }
        }

        private void DisplaySiteDumpResult(SiteDumpResult result)
        {
            lblDumpStatus.Text = result.GetStatusSummary();

            txtCombinedHtmlSource.Text = result.GetCombinedHtml();
            txtCombinedCssSource.Text = result.GetCombinedCss();
            txtCombinedJsSource.Text = result.GetCombinedJavaScript();

            RefreshDumpedResourcesList();

            tabDumpViewers.SelectedTab = _session.ActiveDumpViewMode switch
            {
                SiteDumpViewMode.CombinedHtml => tabCombinedHtmlView,
                SiteDumpViewMode.CombinedCss => tabCombinedCssView,
                SiteDumpViewMode.CombinedJavaScript => tabCombinedJsView,
                _ => tabSelectedResourceView
            };
        }

        private void RefreshDumpedResourcesList()
        {
            lstDumpedResources.BeginUpdate();
            try
            {
                lstDumpedResources.Items.Clear();
                var resources = _session.FilterLastSiteDumpResources(
                    txtFilterResources.Text,
                    lstDumpedResources.SelectedItem as SiteResourceItem,
                    out var resolvedSelection);

                foreach (var res in resources)
                {
                    lstDumpedResources.Items.Add(res);
                }

                if (resolvedSelection != null)
                {
                    lstDumpedResources.SelectedItem = resolvedSelection;
                }
                else
                {
                    txtDumpedResourceDetails.Text = string.Empty;
                    txtSelectedResourceSource.Text = string.Empty;
                }
            }
            finally
            {
                lstDumpedResources.EndUpdate();
            }
        }

        private void LstDumpedResources_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstDumpedResources.SelectedItem is SiteResourceItem resource)
            {
                _session.SelectedDumpResource = resource;
                txtDumpedResourceDetails.Text = resource.GetFormattedDetails();
                txtSelectedResourceSource.Text = resource.Content;
            }
        }

        private SiteDumpViewMode GetActiveDumpViewMode()
        {
            if (tabDumpViewers.SelectedTab == tabCombinedHtmlView) return SiteDumpViewMode.CombinedHtml;
            if (tabDumpViewers.SelectedTab == tabCombinedCssView) return SiteDumpViewMode.CombinedCss;
            if (tabDumpViewers.SelectedTab == tabCombinedJsView) return SiteDumpViewMode.CombinedJavaScript;
            return SiteDumpViewMode.SelectedResource;
        }

        private void BtnCopyCurrentDumpView_Click(object? sender, EventArgs e)
        {
            string? content = _session.GetActiveDumpViewTextForCopy();
            if (content != null)
            {
                Clipboard.SetText(content);
            }
        }

        private void BtnSaveActiveDumpFile_Click(object? sender, EventArgs e)
        {
            if (!_session.TryGetActiveDumpViewForExport(out string defaultFileName))
                return;

            using var sfd = new SaveFileDialog
            {
                Filter = AutomationStudioSession.DumpFileDialogFilter,
                Title = _session.ActiveDumpViewMode == SiteDumpViewMode.SelectedResource
                    ? "Save Selected Resource"
                    : "Save Combined Dump Output",
                FileName = defaultFileName
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _session.SaveActiveDumpViewToFile(sfd.FileName, defaultFileName);
            }
        }

        private void BtnSaveDumpToFolder_Click(object? sender, EventArgs e)
        {
            if (!_session.EnsureHasSiteDumpResultForExport())
                return;

            using var fbd = new FolderBrowserDialog
            {
                Description = "Select a destination folder to save all dumped HTML, CSS, and JavaScript files",
                UseDescriptionForTitle = true
            };

            if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                _session.SaveLastDumpToFolder(fbd.SelectedPath);
            }
        }
    }
}