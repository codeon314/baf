using BrowserAutomationFramework.Automation;
using BrowserAutomationFramework.Models;

namespace BAFStudio
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _session?.Dispose();
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            pnlTop = new FlowLayoutPanel();
            lblPort = new Label();
            numPort = new NumericUpDown();
            lblMode = new Label();
            cmbMode = new ComboBox();
            chkMatchAnySite = new CheckBox();
            lblMatchUrls = new Label();
            pnlMatchUrls = new Panel();
            lstMatchUrls = new ListBox();
            pnlMatchUrlBtns = new Panel();
            btnRemoveMatchUrl = new Button();
            btnEditMatchUrl = new Button();
            btnAddMatchUrl = new Button();
            btnStart = new Button();
            btnStop = new Button();
            btnGenerate = new Button();
            btnExportExtension = new Button();
            splitMain = new SplitContainer();
            splitTop = new SplitContainer();
            pnlClients = new GroupBox();
            lstClients = new ListBox();
            pnlCommands = new GroupBox();
            tabMain = new TabControl();
            tabRaw = new TabPage();
            tlpCommands = new TableLayoutPanel();
            lblCmdType = new Label();
            txtCmdType = new TextBox();
            lblPayload = new Label();
            txtPayload = new TextBox();
            pnlCmdButtons = new FlowLayoutPanel();
            btnSend = new Button();
            btnReactClick = new Button();
            btnEval = new Button();
            tabDom = new TabPage();
            splitDom = new SplitContainer();
            tvDom = new BAFStudio.Controls.DomTreeView();
            txtElementDetails = new TextBox();
            tabEditor = new TabPage();
            splitSequences = new SplitContainer();
            chkLstSequences = new CheckedListBox();
            pnlSequencesTop = new FlowLayoutPanel();
            btnAddSequence = new Button();
            btnRemoveSequence = new Button();
            btnMoveSeqUp = new Button();
            btnMoveSeqDown = new Button();
            btnRenameSeq = new Button();
            splitEditor = new SplitContainer();
            lstSteps = new ListBox();
            pgStepDetails = new PropertyGrid();
            pnlEditorTop = new TableLayoutPanel();
            flpStepButtons = new FlowLayoutPanel();
            btnAddStep = new Button();
            btnRemoveStep = new Button();
            btnMoveUp = new Button();
            btnMoveDown = new Button();
            btnRunAutomation = new Button();
            tabScraper = new TabPage();
            tabReverseEngineering = new TabPage();
            pnlLogs = new GroupBox();
            rtbLogs = new RichTextBox();
            ctxDomNode = new ContextMenuStrip(components);
            mnuClickElement = new ToolStripMenuItem();
            mnuSetValue = new ToolStripMenuItem();
            mnuAddClickTo = new ToolStripMenuItem();
            mnuAddSetValueTo = new ToolStripMenuItem();
            mnuDomSeparator1 = new ToolStripSeparator();
            mnuQuickScrapeText = new ToolStripMenuItem();
            mnuAddScrapeFieldTo = new ToolStripMenuItem();
            mnuAddRepeatingFieldTo = new ToolStripMenuItem();
            mnuSetScrapeContainerFor = new ToolStripMenuItem();
            mnuSetPaginationNextFor = new ToolStripMenuItem();
            menuStripMain = new MenuStrip();
            mnuFile = new ToolStripMenuItem();
            mnuFileNew = new ToolStripMenuItem();
            mnuFileOpen = new ToolStripMenuItem();
            mnuFileSave = new ToolStripMenuItem();
            mnuFileExportExtension = new ToolStripMenuItem();
            mnuFileSeparator = new ToolStripSeparator();
            mnuFileExit = new ToolStripMenuItem();
            mnuHelp = new ToolStripMenuItem();
            mnuHelpAbout = new ToolStripMenuItem();
            pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numPort).BeginInit();
            pnlMatchUrls.SuspendLayout();
            pnlMatchUrlBtns.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitTop).BeginInit();
            splitTop.Panel1.SuspendLayout();
            splitTop.Panel2.SuspendLayout();
            splitTop.SuspendLayout();
            pnlClients.SuspendLayout();
            pnlCommands.SuspendLayout();
            tabMain.SuspendLayout();
            tabRaw.SuspendLayout();
            tlpCommands.SuspendLayout();
            pnlCmdButtons.SuspendLayout();
            tabDom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitDom).BeginInit();
            splitDom.Panel1.SuspendLayout();
            splitDom.Panel2.SuspendLayout();
            splitDom.SuspendLayout();
            tabEditor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitSequences).BeginInit();
            splitSequences.Panel1.SuspendLayout();
            splitSequences.Panel2.SuspendLayout();
            splitSequences.SuspendLayout();
            pnlSequencesTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitEditor).BeginInit();
            splitEditor.Panel1.SuspendLayout();
            splitEditor.Panel2.SuspendLayout();
            splitEditor.SuspendLayout();
            pnlEditorTop.SuspendLayout();
            flpStepButtons.SuspendLayout();
            pnlLogs.SuspendLayout();
            ctxDomNode.SuspendLayout();
            menuStripMain.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTop
            // 
            pnlTop.AutoSize = true;
            pnlTop.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlTop.Controls.Add(lblPort);
            pnlTop.Controls.Add(numPort);
            pnlTop.Controls.Add(lblMode);
            pnlTop.Controls.Add(cmbMode);
            pnlTop.Controls.Add(chkMatchAnySite);
            pnlTop.Controls.Add(lblMatchUrls);
            pnlTop.Controls.Add(pnlMatchUrls);
            pnlTop.Controls.Add(btnStart);
            pnlTop.Controls.Add(btnStop);
            pnlTop.Controls.Add(btnGenerate);
            pnlTop.Controls.Add(btnExportExtension);
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Location = new Point(0, 36);
            pnlTop.Name = "pnlTop";
            pnlTop.Padding = new Padding(10);
            pnlTop.Size = new Size(1256, 172);
            pnlTop.TabIndex = 1;
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(13, 16);
            lblPort.Margin = new Padding(3, 6, 3, 3);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(52, 28);
            lblPort.TabIndex = 0;
            lblPort.Text = "Port:";
            // 
            // numPort
            // 
            numPort.BorderStyle = BorderStyle.FixedSingle;
            numPort.Location = new Point(71, 14);
            numPort.Margin = new Padding(3, 4, 15, 3);
            numPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numPort.Minimum = new decimal(new int[] { 1024, 0, 0, 0 });
            numPort.Name = "numPort";
            numPort.Size = new Size(95, 34);
            numPort.TabIndex = 1;
            numPort.Value = new decimal(new int[] { 9999, 0, 0, 0 });
            // 
            // lblMode
            // 
            lblMode.AutoSize = true;
            lblMode.Location = new Point(184, 16);
            lblMode.Margin = new Padding(3, 6, 3, 3);
            lblMode.Name = "lblMode";
            lblMode.Size = new Size(101, 28);
            lblMode.TabIndex = 2;
            lblMode.Text = "IPC Mode:";
            // 
            // cmbMode
            // 
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.Location = new Point(291, 14);
            cmbMode.Margin = new Padding(3, 4, 15, 3);
            cmbMode.Name = "cmbMode";
            cmbMode.Size = new Size(180, 36);
            cmbMode.TabIndex = 3;
            // 
            // chkMatchAnySite
            // 
            chkMatchAnySite.AutoSize = true;
            chkMatchAnySite.Checked = true;
            chkMatchAnySite.CheckState = CheckState.Checked;
            chkMatchAnySite.Location = new Point(489, 16);
            chkMatchAnySite.Margin = new Padding(3, 6, 15, 3);
            chkMatchAnySite.Name = "chkMatchAnySite";
            chkMatchAnySite.Size = new Size(170, 32);
            chkMatchAnySite.TabIndex = 4;
            chkMatchAnySite.Text = "Match Any Site";
            chkMatchAnySite.UseVisualStyleBackColor = true;
            chkMatchAnySite.CheckedChanged += ChkMatchAnySite_CheckedChanged;
            // 
            // lblMatchUrls
            // 
            lblMatchUrls.AutoSize = true;
            lblMatchUrls.Location = new Point(677, 16);
            lblMatchUrls.Margin = new Padding(3, 6, 3, 3);
            lblMatchUrls.Name = "lblMatchUrls";
            lblMatchUrls.Size = new Size(119, 28);
            lblMatchUrls.TabIndex = 5;
            lblMatchUrls.Text = "Match URLs:";
            lblMatchUrls.Visible = false;
            // 
            // pnlMatchUrls
            // 
            pnlMatchUrls.Controls.Add(lstMatchUrls);
            pnlMatchUrls.Controls.Add(pnlMatchUrlBtns);
            pnlMatchUrls.Location = new Point(802, 14);
            pnlMatchUrls.Margin = new Padding(3, 4, 15, 3);
            pnlMatchUrls.Name = "pnlMatchUrls";
            pnlMatchUrls.Size = new Size(320, 96);
            pnlMatchUrls.TabIndex = 6;
            pnlMatchUrls.Visible = false;
            // 
            // lstMatchUrls
            // 
            lstMatchUrls.BorderStyle = BorderStyle.FixedSingle;
            lstMatchUrls.Dock = DockStyle.Fill;
            lstMatchUrls.IntegralHeight = false;
            lstMatchUrls.Location = new Point(0, 0);
            lstMatchUrls.Name = "lstMatchUrls";
            lstMatchUrls.Size = new Size(230, 96);
            lstMatchUrls.TabIndex = 0;
            // 
            // pnlMatchUrlBtns
            // 
            pnlMatchUrlBtns.Controls.Add(btnRemoveMatchUrl);
            pnlMatchUrlBtns.Controls.Add(btnEditMatchUrl);
            pnlMatchUrlBtns.Controls.Add(btnAddMatchUrl);
            pnlMatchUrlBtns.Dock = DockStyle.Right;
            pnlMatchUrlBtns.Location = new Point(230, 0);
            pnlMatchUrlBtns.Name = "pnlMatchUrlBtns";
            pnlMatchUrlBtns.Size = new Size(90, 96);
            pnlMatchUrlBtns.TabIndex = 1;
            // 
            // btnRemoveMatchUrl
            // 
            btnRemoveMatchUrl.Dock = DockStyle.Top;
            btnRemoveMatchUrl.Location = new Point(0, 64);
            btnRemoveMatchUrl.Name = "btnRemoveMatchUrl";
            btnRemoveMatchUrl.Size = new Size(90, 32);
            btnRemoveMatchUrl.TabIndex = 2;
            btnRemoveMatchUrl.Text = "Remove";
            btnRemoveMatchUrl.UseVisualStyleBackColor = true;
            btnRemoveMatchUrl.Click += BtnRemoveMatchUrl_Click;
            // 
            // btnEditMatchUrl
            // 
            btnEditMatchUrl.Dock = DockStyle.Top;
            btnEditMatchUrl.Location = new Point(0, 32);
            btnEditMatchUrl.Name = "btnEditMatchUrl";
            btnEditMatchUrl.Size = new Size(90, 32);
            btnEditMatchUrl.TabIndex = 1;
            btnEditMatchUrl.Text = "Edit";
            btnEditMatchUrl.UseVisualStyleBackColor = true;
            btnEditMatchUrl.Click += BtnEditMatchUrl_Click;
            // 
            // btnAddMatchUrl
            // 
            btnAddMatchUrl.Dock = DockStyle.Top;
            btnAddMatchUrl.Location = new Point(0, 0);
            btnAddMatchUrl.Name = "btnAddMatchUrl";
            btnAddMatchUrl.Size = new Size(90, 32);
            btnAddMatchUrl.TabIndex = 0;
            btnAddMatchUrl.Text = "Add";
            btnAddMatchUrl.UseVisualStyleBackColor = true;
            btnAddMatchUrl.Click += BtnAddMatchUrl_Click;
            // 
            // btnStart
            // 
            btnStart.AutoSize = true;
            btnStart.Location = new Point(13, 113);
            btnStart.Margin = new Padding(3, 0, 10, 3);
            btnStart.Name = "btnStart";
            btnStart.Padding = new Padding(8, 4, 8, 4);
            btnStart.Size = new Size(139, 46);
            btnStart.TabIndex = 7;
            btnStart.Text = "Start Server";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += BtnStart_Click;
            // 
            // btnStop
            // 
            btnStop.AutoSize = true;
            btnStop.Enabled = false;
            btnStop.Location = new Point(165, 113);
            btnStop.Margin = new Padding(3, 0, 10, 3);
            btnStop.Name = "btnStop";
            btnStop.Padding = new Padding(8, 4, 8, 4);
            btnStop.Size = new Size(139, 46);
            btnStop.TabIndex = 8;
            btnStop.Text = "Stop Server";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += BtnStop_Click;
            // 
            // btnGenerate
            // 
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(317, 113);
            btnGenerate.Margin = new Padding(3, 0, 10, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Padding = new Padding(8, 4, 8, 4);
            btnGenerate.Size = new Size(290, 46);
            btnGenerate.TabIndex = 9;
            btnGenerate.Text = "Generate && Copy Script (TM)";
            btnGenerate.UseVisualStyleBackColor = true;
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // btnExportExtension
            // 
            btnExportExtension.AutoSize = true;
            btnExportExtension.Location = new Point(620, 113);
            btnExportExtension.Margin = new Padding(3, 0, 10, 3);
            btnExportExtension.Name = "btnExportExtension";
            btnExportExtension.Padding = new Padding(8, 4, 8, 4);
            btnExportExtension.Size = new Size(269, 46);
            btnExportExtension.TabIndex = 10;
            btnExportExtension.Text = "Export Chrome Extension...";
            btnExportExtension.UseVisualStyleBackColor = true;
            btnExportExtension.Click += BtnExportExtension_Click;
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(0, 208);
            splitMain.Name = "splitMain";
            splitMain.Orientation = Orientation.Horizontal;
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(splitTop);
            splitMain.Panel1MinSize = 300;
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(pnlLogs);
            splitMain.Panel2MinSize = 150;
            splitMain.Size = new Size(1256, 609);
            splitMain.SplitterDistance = 450;
            splitMain.TabIndex = 0;
            // 
            // splitTop
            // 
            splitTop.Dock = DockStyle.Fill;
            splitTop.Location = new Point(0, 0);
            splitTop.Name = "splitTop";
            // 
            // splitTop.Panel1
            // 
            splitTop.Panel1.Controls.Add(pnlClients);
            splitTop.Panel1MinSize = 250;
            // 
            // splitTop.Panel2
            // 
            splitTop.Panel2.Controls.Add(pnlCommands);
            splitTop.Panel2MinSize = 400;
            splitTop.Size = new Size(1256, 450);
            splitTop.SplitterDistance = 301;
            splitTop.TabIndex = 0;
            // 
            // pnlClients
            // 
            pnlClients.Controls.Add(lstClients);
            pnlClients.Dock = DockStyle.Fill;
            pnlClients.Location = new Point(0, 0);
            pnlClients.Name = "pnlClients";
            pnlClients.Padding = new Padding(10);
            pnlClients.Size = new Size(301, 450);
            pnlClients.TabIndex = 0;
            pnlClients.TabStop = false;
            pnlClients.Text = "Connected Clients";
            // 
            // lstClients
            // 
            lstClients.BorderStyle = BorderStyle.FixedSingle;
            lstClients.DisplayMember = "DisplayName";
            lstClients.Dock = DockStyle.Fill;
            lstClients.IntegralHeight = false;
            lstClients.Location = new Point(10, 37);
            lstClients.Name = "lstClients";
            lstClients.Size = new Size(281, 403);
            lstClients.TabIndex = 0;
            lstClients.SelectedIndexChanged += LstClients_SelectedIndexChanged;
            // 
            // pnlCommands
            // 
            pnlCommands.Controls.Add(tabMain);
            pnlCommands.Dock = DockStyle.Fill;
            pnlCommands.Location = new Point(0, 0);
            pnlCommands.Name = "pnlCommands";
            pnlCommands.Padding = new Padding(10);
            pnlCommands.Size = new Size(951, 450);
            pnlCommands.TabIndex = 0;
            pnlCommands.TabStop = false;
            pnlCommands.Text = "Command Center";
            // 
            // tabMain
            // 
            tabMain.Controls.Add(tabRaw);
            tabMain.Controls.Add(tabDom);
            tabMain.Controls.Add(tabEditor);
            tabMain.Controls.Add(tabScraper);
            tabMain.Controls.Add(tabReverseEngineering);
            tabMain.Dock = DockStyle.Fill;
            tabMain.Location = new Point(10, 37);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(931, 403);
            tabMain.TabIndex = 1;
            // 
            // tabRaw
            // 
            tabRaw.Controls.Add(tlpCommands);
            tabRaw.Location = new Point(4, 37);
            tabRaw.Name = "tabRaw";
            tabRaw.Padding = new Padding(3);
            tabRaw.Size = new Size(923, 362);
            tabRaw.TabIndex = 0;
            tabRaw.Text = "Raw Commands";
            // 
            // tlpCommands
            // 
            tlpCommands.ColumnCount = 1;
            tlpCommands.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpCommands.Controls.Add(lblCmdType, 0, 0);
            tlpCommands.Controls.Add(txtCmdType, 0, 1);
            tlpCommands.Controls.Add(lblPayload, 0, 2);
            tlpCommands.Controls.Add(txtPayload, 0, 3);
            tlpCommands.Controls.Add(pnlCmdButtons, 0, 4);
            tlpCommands.Dock = DockStyle.Fill;
            tlpCommands.Location = new Point(3, 3);
            tlpCommands.Name = "tlpCommands";
            tlpCommands.RowCount = 5;
            tlpCommands.RowStyles.Add(new RowStyle());
            tlpCommands.RowStyles.Add(new RowStyle());
            tlpCommands.RowStyles.Add(new RowStyle());
            tlpCommands.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCommands.RowStyles.Add(new RowStyle());
            tlpCommands.Size = new Size(917, 356);
            tlpCommands.TabIndex = 0;
            // 
            // lblCmdType
            // 
            lblCmdType.AutoSize = true;
            lblCmdType.Location = new Point(0, 0);
            lblCmdType.Margin = new Padding(0);
            lblCmdType.Name = "lblCmdType";
            lblCmdType.Size = new Size(153, 28);
            lblCmdType.TabIndex = 0;
            lblCmdType.Text = "Command Type:";
            // 
            // txtCmdType
            // 
            txtCmdType.BorderStyle = BorderStyle.FixedSingle;
            txtCmdType.Dock = DockStyle.Fill;
            txtCmdType.Font = new Font("Consolas", 10F);
            txtCmdType.Location = new Point(0, 33);
            txtCmdType.Margin = new Padding(0, 5, 0, 10);
            txtCmdType.Name = "txtCmdType";
            txtCmdType.Size = new Size(917, 31);
            txtCmdType.TabIndex = 1;
            txtCmdType.Text = "EVAL";
            // 
            // lblPayload
            // 
            lblPayload.AutoSize = true;
            lblPayload.Location = new Point(0, 74);
            lblPayload.Margin = new Padding(0);
            lblPayload.Name = "lblPayload";
            lblPayload.Size = new Size(231, 28);
            lblPayload.TabIndex = 2;
            lblPayload.Text = "Payload (JSON or String):";
            // 
            // txtPayload
            // 
            txtPayload.BorderStyle = BorderStyle.FixedSingle;
            txtPayload.Dock = DockStyle.Fill;
            txtPayload.Font = new Font("Consolas", 10F);
            txtPayload.Location = new Point(0, 107);
            txtPayload.Margin = new Padding(0, 5, 0, 0);
            txtPayload.Multiline = true;
            txtPayload.Name = "txtPayload";
            txtPayload.ScrollBars = ScrollBars.Vertical;
            txtPayload.Size = new Size(917, 187);
            txtPayload.TabIndex = 3;
            txtPayload.Text = "console.log('Hello from C#'); return 'Success';";
            // 
            // pnlCmdButtons
            // 
            pnlCmdButtons.AutoSize = true;
            pnlCmdButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlCmdButtons.Controls.Add(btnSend);
            pnlCmdButtons.Controls.Add(btnReactClick);
            pnlCmdButtons.Controls.Add(btnEval);
            pnlCmdButtons.Dock = DockStyle.Fill;
            pnlCmdButtons.Location = new Point(0, 304);
            pnlCmdButtons.Margin = new Padding(0, 10, 0, 0);
            pnlCmdButtons.Name = "pnlCmdButtons";
            pnlCmdButtons.Size = new Size(917, 52);
            pnlCmdButtons.TabIndex = 4;
            // 
            // btnSend
            // 
            btnSend.AutoSize = true;
            btnSend.Location = new Point(0, 0);
            btnSend.Margin = new Padding(0, 0, 10, 10);
            btnSend.Name = "btnSend";
            btnSend.Padding = new Padding(5, 2, 5, 2);
            btnSend.Size = new Size(213, 42);
            btnSend.TabIndex = 0;
            btnSend.Text = "Send Raw Command";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += BtnSend_Click;
            // 
            // btnReactClick
            // 
            btnReactClick.AutoSize = true;
            btnReactClick.Location = new Point(223, 0);
            btnReactClick.Margin = new Padding(0, 0, 10, 10);
            btnReactClick.Name = "btnReactClick";
            btnReactClick.Padding = new Padding(5, 2, 5, 2);
            btnReactClick.Size = new Size(251, 42);
            btnReactClick.TabIndex = 1;
            btnReactClick.Text = "Test React Click (Selector)";
            btnReactClick.UseVisualStyleBackColor = true;
            btnReactClick.Click += BtnReactClick_Click;
            // 
            // btnEval
            // 
            btnEval.AutoSize = true;
            btnEval.Location = new Point(484, 0);
            btnEval.Margin = new Padding(0, 0, 10, 10);
            btnEval.Name = "btnEval";
            btnEval.Padding = new Padding(5, 2, 5, 2);
            btnEval.Size = new Size(140, 42);
            btnEval.TabIndex = 2;
            btnEval.Text = "Test Eval (JS)";
            btnEval.UseVisualStyleBackColor = true;
            btnEval.Click += BtnEval_Click;
            // 
            // tabDom
            // 
            tabDom.Controls.Add(splitDom);
            tabDom.Location = new Point(4, 34);
            tabDom.Name = "tabDom";
            tabDom.Padding = new Padding(3);
            tabDom.Size = new Size(923, 367);
            tabDom.TabIndex = 1;
            tabDom.Text = "DOM Explorer";
            // 
            // splitDom
            // 
            splitDom.Dock = DockStyle.Fill;
            splitDom.Location = new Point(3, 3);
            splitDom.Name = "splitDom";
            // 
            // splitDom.Panel1
            // 
            splitDom.Panel1.Controls.Add(tvDom);
            // 
            // splitDom.Panel2
            // 
            splitDom.Panel2.Controls.Add(txtElementDetails);
            splitDom.Size = new Size(917, 361);
            splitDom.SplitterDistance = 402;
            splitDom.TabIndex = 1;
            // 
            // tvDom
            // 
            tvDom.BackColor = Color.FromArgb(30, 30, 30);
            tvDom.Dock = DockStyle.Fill;
            tvDom.ForeColor = Color.FromArgb(211, 211, 211);
            tvDom.ItemHeight = 36;
            tvDom.Location = new Point(0, 0);
            tvDom.Name = "tvDom";
            tvDom.Size = new Size(402, 361);
            tvDom.TabIndex = 0;
            tvDom.AfterSelect += TvDom_AfterSelect;
            tvDom.NodeMouseClick += TvDom_NodeMouseClick;
            // 
            // txtElementDetails
            // 
            txtElementDetails.BorderStyle = BorderStyle.FixedSingle;
            txtElementDetails.Dock = DockStyle.Fill;
            txtElementDetails.Font = new Font("Consolas", 10F);
            txtElementDetails.Location = new Point(0, 0);
            txtElementDetails.Multiline = true;
            txtElementDetails.Name = "txtElementDetails";
            txtElementDetails.ReadOnly = true;
            txtElementDetails.ScrollBars = ScrollBars.Vertical;
            txtElementDetails.Size = new Size(511, 361);
            txtElementDetails.TabIndex = 0;
            // 
            // tabEditor
            // 
            tabEditor.Controls.Add(splitSequences);
            tabEditor.Location = new Point(4, 34);
            tabEditor.Name = "tabEditor";
            tabEditor.Padding = new Padding(3);
            tabEditor.Size = new Size(923, 367);
            tabEditor.TabIndex = 2;
            tabEditor.Text = "Automation Editor";
            // 
            // splitSequences
            // 
            splitSequences.Dock = DockStyle.Fill;
            splitSequences.Location = new Point(3, 3);
            splitSequences.Name = "splitSequences";
            // 
            // splitSequences.Panel1
            // 
            splitSequences.Panel1.Controls.Add(chkLstSequences);
            splitSequences.Panel1.Controls.Add(pnlSequencesTop);
            // 
            // splitSequences.Panel2
            // 
            splitSequences.Panel2.Controls.Add(splitEditor);
            splitSequences.Panel2.Controls.Add(pnlEditorTop);
            splitSequences.Size = new Size(917, 361);
            splitSequences.SplitterDistance = 285;
            splitSequences.TabIndex = 3;
            // 
            // chkLstSequences
            // 
            chkLstSequences.BorderStyle = BorderStyle.FixedSingle;
            chkLstSequences.Dock = DockStyle.Fill;
            chkLstSequences.FormattingEnabled = true;
            chkLstSequences.IntegralHeight = false;
            chkLstSequences.Location = new Point(0, 158);
            chkLstSequences.Name = "chkLstSequences";
            chkLstSequences.Size = new Size(285, 203);
            chkLstSequences.TabIndex = 1;
            chkLstSequences.ItemCheck += ChkLstSequences_ItemCheck;
            chkLstSequences.SelectedIndexChanged += ChkLstSequences_SelectedIndexChanged;
            // 
            // pnlSequencesTop
            // 
            pnlSequencesTop.AutoSize = true;
            pnlSequencesTop.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlSequencesTop.Controls.Add(btnAddSequence);
            pnlSequencesTop.Controls.Add(btnRemoveSequence);
            pnlSequencesTop.Controls.Add(btnMoveSeqUp);
            pnlSequencesTop.Controls.Add(btnMoveSeqDown);
            pnlSequencesTop.Controls.Add(btnRenameSeq);
            pnlSequencesTop.Dock = DockStyle.Top;
            pnlSequencesTop.Location = new Point(0, 0);
            pnlSequencesTop.Name = "pnlSequencesTop";
            pnlSequencesTop.Padding = new Padding(4);
            pnlSequencesTop.Size = new Size(285, 158);
            pnlSequencesTop.TabIndex = 0;
            // 
            // btnAddSequence
            // 
            btnAddSequence.AutoSize = true;
            btnAddSequence.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnAddSequence.Location = new Point(7, 7);
            btnAddSequence.Name = "btnAddSequence";
            btnAddSequence.Padding = new Padding(6, 3, 6, 3);
            btnAddSequence.Size = new Size(105, 44);
            btnAddSequence.TabIndex = 0;
            btnAddSequence.Text = "Add List";
            btnAddSequence.UseVisualStyleBackColor = true;
            btnAddSequence.Click += BtnAddSequence_Click;
            // 
            // btnRemoveSequence
            // 
            btnRemoveSequence.AutoSize = true;
            btnRemoveSequence.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRemoveSequence.Location = new Point(118, 7);
            btnRemoveSequence.Name = "btnRemoveSequence";
            btnRemoveSequence.Padding = new Padding(6, 3, 6, 3);
            btnRemoveSequence.Size = new Size(138, 44);
            btnRemoveSequence.TabIndex = 1;
            btnRemoveSequence.Text = "Remove List";
            btnRemoveSequence.UseVisualStyleBackColor = true;
            btnRemoveSequence.Click += BtnRemoveSequence_Click;
            // 
            // btnMoveSeqUp
            // 
            btnMoveSeqUp.AutoSize = true;
            btnMoveSeqUp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnMoveSeqUp.Location = new Point(7, 57);
            btnMoveSeqUp.Name = "btnMoveSeqUp";
            btnMoveSeqUp.Padding = new Padding(6, 3, 6, 3);
            btnMoveSeqUp.Size = new Size(115, 44);
            btnMoveSeqUp.TabIndex = 2;
            btnMoveSeqUp.Text = "Move Up";
            btnMoveSeqUp.UseVisualStyleBackColor = true;
            btnMoveSeqUp.Click += BtnMoveSeqUp_Click;
            // 
            // btnMoveSeqDown
            // 
            btnMoveSeqDown.AutoSize = true;
            btnMoveSeqDown.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnMoveSeqDown.Location = new Point(128, 57);
            btnMoveSeqDown.Name = "btnMoveSeqDown";
            btnMoveSeqDown.Padding = new Padding(6, 3, 6, 3);
            btnMoveSeqDown.Size = new Size(140, 44);
            btnMoveSeqDown.TabIndex = 3;
            btnMoveSeqDown.Text = "Move Down";
            btnMoveSeqDown.UseVisualStyleBackColor = true;
            btnMoveSeqDown.Click += BtnMoveSeqDown_Click;
            // 
            // btnRenameSeq
            // 
            btnRenameSeq.AutoSize = true;
            btnRenameSeq.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRenameSeq.Location = new Point(7, 107);
            btnRenameSeq.Name = "btnRenameSeq";
            btnRenameSeq.Padding = new Padding(6, 3, 6, 3);
            btnRenameSeq.Size = new Size(103, 44);
            btnRenameSeq.TabIndex = 4;
            btnRenameSeq.Text = "Rename";
            btnRenameSeq.UseVisualStyleBackColor = true;
            btnRenameSeq.Click += BtnRenameSeq_Click;
            // 
            // splitEditor
            // 
            splitEditor.Dock = DockStyle.Fill;
            splitEditor.Location = new Point(0, 108);
            splitEditor.Name = "splitEditor";
            // 
            // splitEditor.Panel1
            // 
            splitEditor.Panel1.Controls.Add(lstSteps);
            // 
            // splitEditor.Panel2
            // 
            splitEditor.Panel2.Controls.Add(pgStepDetails);
            splitEditor.Size = new Size(628, 253);
            splitEditor.SplitterDistance = 205;
            splitEditor.TabIndex = 1;
            // 
            // lstSteps
            // 
            lstSteps.BorderStyle = BorderStyle.FixedSingle;
            lstSteps.Dock = DockStyle.Fill;
            lstSteps.IntegralHeight = false;
            lstSteps.Location = new Point(0, 0);
            lstSteps.Name = "lstSteps";
            lstSteps.Size = new Size(205, 253);
            lstSteps.TabIndex = 0;
            lstSteps.SelectedIndexChanged += LstSteps_SelectedIndexChanged;
            // 
            // pgStepDetails
            // 
            pgStepDetails.BackColor = SystemColors.Control;
            pgStepDetails.Dock = DockStyle.Fill;
            pgStepDetails.Location = new Point(0, 0);
            pgStepDetails.Name = "pgStepDetails";
            pgStepDetails.Size = new Size(419, 253);
            pgStepDetails.TabIndex = 0;
            pgStepDetails.PropertyValueChanged += PgStepDetails_PropertyValueChanged;
            // 
            // pnlEditorTop
            // 
            pnlEditorTop.AutoSize = true;
            pnlEditorTop.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlEditorTop.ColumnCount = 2;
            pnlEditorTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlEditorTop.ColumnStyles.Add(new ColumnStyle());
            pnlEditorTop.Controls.Add(flpStepButtons, 0, 0);
            pnlEditorTop.Controls.Add(btnRunAutomation, 1, 0);
            pnlEditorTop.Dock = DockStyle.Top;
            pnlEditorTop.Location = new Point(0, 0);
            pnlEditorTop.Name = "pnlEditorTop";
            pnlEditorTop.Padding = new Padding(4);
            pnlEditorTop.RowCount = 1;
            pnlEditorTop.RowStyles.Add(new RowStyle());
            pnlEditorTop.Size = new Size(628, 108);
            pnlEditorTop.TabIndex = 0;
            // 
            // flpStepButtons
            // 
            flpStepButtons.AutoSize = true;
            flpStepButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flpStepButtons.Controls.Add(btnAddStep);
            flpStepButtons.Controls.Add(btnRemoveStep);
            flpStepButtons.Controls.Add(btnMoveUp);
            flpStepButtons.Controls.Add(btnMoveDown);
            flpStepButtons.Dock = DockStyle.Fill;
            flpStepButtons.Location = new Point(4, 4);
            flpStepButtons.Margin = new Padding(0);
            flpStepButtons.Name = "flpStepButtons";
            flpStepButtons.Size = new Size(432, 100);
            flpStepButtons.TabIndex = 0;
            // 
            // btnAddStep
            // 
            btnAddStep.AutoSize = true;
            btnAddStep.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnAddStep.Location = new Point(3, 3);
            btnAddStep.Name = "btnAddStep";
            btnAddStep.Padding = new Padding(6, 3, 6, 3);
            btnAddStep.Size = new Size(115, 44);
            btnAddStep.TabIndex = 0;
            btnAddStep.Text = "Add Step";
            btnAddStep.UseVisualStyleBackColor = true;
            btnAddStep.Click += BtnAddStep_Click;
            // 
            // btnRemoveStep
            // 
            btnRemoveStep.AutoSize = true;
            btnRemoveStep.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRemoveStep.Location = new Point(124, 3);
            btnRemoveStep.Name = "btnRemoveStep";
            btnRemoveStep.Padding = new Padding(6, 3, 6, 3);
            btnRemoveStep.Size = new Size(148, 44);
            btnRemoveStep.TabIndex = 1;
            btnRemoveStep.Text = "Remove Step";
            btnRemoveStep.UseVisualStyleBackColor = true;
            btnRemoveStep.Click += BtnRemoveStep_Click;
            // 
            // btnMoveUp
            // 
            btnMoveUp.AutoSize = true;
            btnMoveUp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnMoveUp.Location = new Point(278, 3);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.Padding = new Padding(6, 3, 6, 3);
            btnMoveUp.Size = new Size(115, 44);
            btnMoveUp.TabIndex = 3;
            btnMoveUp.Text = "Move Up";
            btnMoveUp.UseVisualStyleBackColor = true;
            btnMoveUp.Click += BtnMoveUp_Click;
            // 
            // btnMoveDown
            // 
            btnMoveDown.AutoSize = true;
            btnMoveDown.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnMoveDown.Location = new Point(3, 53);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.Padding = new Padding(6, 3, 6, 3);
            btnMoveDown.Size = new Size(140, 44);
            btnMoveDown.TabIndex = 4;
            btnMoveDown.Text = "Move Down";
            btnMoveDown.UseVisualStyleBackColor = true;
            btnMoveDown.Click += BtnMoveDown_Click;
            // 
            // btnRunAutomation
            // 
            btnRunAutomation.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRunAutomation.AutoSize = true;
            btnRunAutomation.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRunAutomation.Location = new Point(439, 7);
            btnRunAutomation.Name = "btnRunAutomation";
            btnRunAutomation.Padding = new Padding(8, 3, 8, 3);
            btnRunAutomation.Size = new Size(182, 44);
            btnRunAutomation.TabIndex = 2;
            btnRunAutomation.Text = "Run Automation";
            btnRunAutomation.UseVisualStyleBackColor = true;
            btnRunAutomation.Click += BtnRunAutomation_Click;
            // 
            // tabScraper
            // 
            tabScraper.Location = new Point(4, 34);
            tabScraper.Name = "tabScraper";
            tabScraper.Padding = new Padding(3);
            tabScraper.Size = new Size(923, 367);
            tabScraper.TabIndex = 3;
            tabScraper.Text = "Data Scraper";
            // 
            // tabReverseEngineering
            // 
            tabReverseEngineering.Location = new Point(4, 34);
            tabReverseEngineering.Name = "tabReverseEngineering";
            tabReverseEngineering.Padding = new Padding(3);
            tabReverseEngineering.Size = new Size(923, 367);
            tabReverseEngineering.TabIndex = 4;
            tabReverseEngineering.Text = "Reverse Engineering";
            // 
            // pnlLogs
            // 
            pnlLogs.Controls.Add(rtbLogs);
            pnlLogs.Dock = DockStyle.Fill;
            pnlLogs.Location = new Point(0, 0);
            pnlLogs.Name = "pnlLogs";
            pnlLogs.Padding = new Padding(10);
            pnlLogs.Size = new Size(1256, 155);
            pnlLogs.TabIndex = 0;
            pnlLogs.TabStop = false;
            pnlLogs.Text = "Real-time Logs";
            // 
            // rtbLogs
            // 
            rtbLogs.BorderStyle = BorderStyle.FixedSingle;
            rtbLogs.Dock = DockStyle.Fill;
            rtbLogs.Font = new Font("Consolas", 10F);
            rtbLogs.Location = new Point(10, 37);
            rtbLogs.Name = "rtbLogs";
            rtbLogs.ReadOnly = true;
            rtbLogs.Size = new Size(1236, 108);
            rtbLogs.TabIndex = 0;
            rtbLogs.Text = "";
            // 
            // ctxDomNode
            // 
            ctxDomNode.Font = new Font("Segoe UI", 10F);
            ctxDomNode.ImageScalingSize = new Size(24, 24);
            ctxDomNode.Items.AddRange(new ToolStripItem[] { mnuClickElement, mnuSetValue, mnuAddClickTo, mnuAddSetValueTo, mnuDomSeparator1, mnuQuickScrapeText, mnuAddScrapeFieldTo, mnuAddRepeatingFieldTo, mnuSetScrapeContainerFor, mnuSetPaginationNextFor });
            ctxDomNode.Name = "ctxDomNode";
            ctxDomNode.Size = new Size(404, 316);
            ctxDomNode.Opening += CtxDomNode_Opening;
            // 
            // mnuClickElement
            // 
            mnuClickElement.Name = "mnuClickElement";
            mnuClickElement.Size = new Size(403, 34);
            mnuClickElement.Text = "Test Click Element";
            mnuClickElement.Click += MnuClickElement_Click;
            // 
            // mnuSetValue
            // 
            mnuSetValue.Name = "mnuSetValue";
            mnuSetValue.Size = new Size(403, 34);
            mnuSetValue.Text = "Test Set Value";
            mnuSetValue.Click += MnuSetValue_Click;
            // 
            // mnuAddClickTo
            // 
            mnuAddClickTo.Name = "mnuAddClickTo";
            mnuAddClickTo.Size = new Size(403, 34);
            mnuAddClickTo.Text = "Add Click Step To...";
            // 
            // mnuAddSetValueTo
            // 
            mnuAddSetValueTo.Name = "mnuAddSetValueTo";
            mnuAddSetValueTo.Size = new Size(403, 34);
            mnuAddSetValueTo.Text = "Add Set Value Step To...";
            // 
            // mnuDomSeparator1
            // 
            mnuDomSeparator1.Name = "mnuDomSeparator1";
            mnuDomSeparator1.Size = new Size(400, 6);
            // 
            // mnuQuickScrapeText
            // 
            mnuQuickScrapeText.Name = "mnuQuickScrapeText";
            mnuQuickScrapeText.Size = new Size(403, 34);
            mnuQuickScrapeText.Text = "Test Scrape Element";
            mnuQuickScrapeText.Click += MnuQuickScrapeText_Click;
            // 
            // mnuAddScrapeFieldTo
            // 
            mnuAddScrapeFieldTo.Name = "mnuAddScrapeFieldTo";
            mnuAddScrapeFieldTo.Size = new Size(403, 34);
            mnuAddScrapeFieldTo.Text = "Add Scrape Field To...";
            // 
            // mnuAddRepeatingFieldTo
            // 
            mnuAddRepeatingFieldTo.Name = "mnuAddRepeatingFieldTo";
            mnuAddRepeatingFieldTo.Size = new Size(403, 34);
            mnuAddRepeatingFieldTo.Text = "Add Repeating Field (All Similar) To...";
            // 
            // mnuSetScrapeContainerFor
            // 
            mnuSetScrapeContainerFor.Name = "mnuSetScrapeContainerFor";
            mnuSetScrapeContainerFor.Size = new Size(403, 34);
            mnuSetScrapeContainerFor.Text = "Set as Repeating Container For...";
            // 
            // mnuSetPaginationNextFor
            // 
            mnuSetPaginationNextFor.Name = "mnuSetPaginationNextFor";
            mnuSetPaginationNextFor.Size = new Size(403, 34);
            mnuSetPaginationNextFor.Text = "Set as Pagination Next Button For...";
            // 
            // menuStripMain
            // 
            menuStripMain.Font = new Font("Segoe UI", 10F);
            menuStripMain.ImageScalingSize = new Size(24, 24);
            menuStripMain.Items.AddRange(new ToolStripItem[] { mnuFile, mnuHelp });
            menuStripMain.Location = new Point(0, 0);
            menuStripMain.Name = "menuStripMain";
            menuStripMain.Size = new Size(1256, 36);
            menuStripMain.TabIndex = 2;
            menuStripMain.Text = "menuStripMain";
            // 
            // mnuFile
            // 
            mnuFile.DropDownItems.AddRange(new ToolStripItem[] { mnuFileNew, mnuFileOpen, mnuFileSave, mnuFileExportExtension, mnuFileSeparator, mnuFileExit });
            mnuFile.Name = "mnuFile";
            mnuFile.Size = new Size(58, 32);
            mnuFile.Text = "&File";
            // 
            // mnuFileNew
            // 
            mnuFileNew.Name = "mnuFileNew";
            mnuFileNew.Size = new Size(345, 36);
            mnuFileNew.Text = "&New Project";
            mnuFileNew.Click += MnuFileNew_Click;
            // 
            // mnuFileOpen
            // 
            mnuFileOpen.Name = "mnuFileOpen";
            mnuFileOpen.Size = new Size(345, 36);
            mnuFileOpen.Text = "&Open Project...";
            mnuFileOpen.Click += MnuFileOpen_Click;
            // 
            // mnuFileSave
            // 
            mnuFileSave.Name = "mnuFileSave";
            mnuFileSave.Size = new Size(345, 36);
            mnuFileSave.Text = "&Save Project...";
            mnuFileSave.Click += MnuFileSave_Click;
            // 
            // mnuFileExportExtension
            // 
            mnuFileExportExtension.Name = "mnuFileExportExtension";
            mnuFileExportExtension.Size = new Size(345, 36);
            mnuFileExportExtension.Text = "Export &Chrome Extension...";
            mnuFileExportExtension.Click += BtnExportExtension_Click;
            // 
            // mnuFileSeparator
            // 
            mnuFileSeparator.Name = "mnuFileSeparator";
            mnuFileSeparator.Size = new Size(342, 6);
            // 
            // mnuFileExit
            // 
            mnuFileExit.Name = "mnuFileExit";
            mnuFileExit.Size = new Size(345, 36);
            mnuFileExit.Text = "E&xit";
            mnuFileExit.Click += MnuFileExit_Click;
            // 
            // mnuHelp
            // 
            mnuHelp.DropDownItems.AddRange(new ToolStripItem[] { mnuHelpAbout });
            mnuHelp.Name = "mnuHelp";
            mnuHelp.Size = new Size(69, 32);
            mnuHelp.Text = "&Help";
            // 
            // mnuHelpAbout
            // 
            mnuHelpAbout.Name = "mnuHelpAbout";
            mnuHelpAbout.Size = new Size(169, 36);
            mnuHelpAbout.Text = "&About";
            mnuHelpAbout.Click += MnuHelpAbout_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1256, 817);
            Controls.Add(splitMain);
            Controls.Add(pnlTop);
            Controls.Add(menuStripMain);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStripMain;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BAF Studio - Browser Automation Framework Studio";
            WindowState = FormWindowState.Maximized;
            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numPort).EndInit();
            pnlMatchUrls.ResumeLayout(false);
            pnlMatchUrlBtns.ResumeLayout(false);
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            splitTop.Panel1.ResumeLayout(false);
            splitTop.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitTop).EndInit();
            splitTop.ResumeLayout(false);
            pnlClients.ResumeLayout(false);
            pnlCommands.ResumeLayout(false);
            tabMain.ResumeLayout(false);
            tabRaw.ResumeLayout(false);
            tlpCommands.ResumeLayout(false);
            tlpCommands.PerformLayout();
            pnlCmdButtons.ResumeLayout(false);
            pnlCmdButtons.PerformLayout();
            tabDom.ResumeLayout(false);
            splitDom.Panel1.ResumeLayout(false);
            splitDom.Panel2.ResumeLayout(false);
            splitDom.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitDom).EndInit();
            splitDom.ResumeLayout(false);
            tabEditor.ResumeLayout(false);
            splitSequences.Panel1.ResumeLayout(false);
            splitSequences.Panel1.PerformLayout();
            splitSequences.Panel2.ResumeLayout(false);
            splitSequences.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitSequences).EndInit();
            splitSequences.ResumeLayout(false);
            pnlSequencesTop.ResumeLayout(false);
            pnlSequencesTop.PerformLayout();
            splitEditor.Panel1.ResumeLayout(false);
            splitEditor.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitEditor).EndInit();
            splitEditor.ResumeLayout(false);
            pnlEditorTop.ResumeLayout(false);
            pnlEditorTop.PerformLayout();
            flpStepButtons.ResumeLayout(false);
            flpStepButtons.PerformLayout();
            pnlLogs.ResumeLayout(false);
            ctxDomNode.ResumeLayout(false);
            menuStripMain.ResumeLayout(false);
            menuStripMain.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel pnlTop;
        private System.Windows.Forms.TableLayoutPanel tlpCommands;
        private System.Windows.Forms.FlowLayoutPanel pnlCmdButtons;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.Label lblMode;
        private System.Windows.Forms.ComboBox cmbMode;
        private System.Windows.Forms.CheckBox chkMatchAnySite;
        private System.Windows.Forms.Label lblMatchUrls;
        private System.Windows.Forms.Panel pnlMatchUrls;
        private System.Windows.Forms.ListBox lstMatchUrls;
        private System.Windows.Forms.Panel pnlMatchUrlBtns;
        private System.Windows.Forms.Button btnAddMatchUrl;
        private System.Windows.Forms.Button btnEditMatchUrl;
        private System.Windows.Forms.Button btnRemoveMatchUrl;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnExportExtension;
        private System.Windows.Forms.ToolStripMenuItem mnuFileExportExtension;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.SplitContainer splitTop;
        private System.Windows.Forms.GroupBox pnlClients;
        private System.Windows.Forms.ListBox lstClients;
        private System.Windows.Forms.GroupBox pnlCommands;
        private System.Windows.Forms.Label lblCmdType;
        private System.Windows.Forms.TextBox txtCmdType;
        private System.Windows.Forms.Label lblPayload;
        private System.Windows.Forms.TextBox txtPayload;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnReactClick;
        private System.Windows.Forms.Button btnEval;
        private System.Windows.Forms.GroupBox pnlLogs;
        private System.Windows.Forms.RichTextBox rtbLogs;

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabRaw;
        private System.Windows.Forms.TabPage tabDom;
        private System.Windows.Forms.TabPage tabEditor;
        private System.Windows.Forms.SplitContainer splitDom;
        private BAFStudio.Controls.DomTreeView tvDom;
        private System.Windows.Forms.TextBox txtElementDetails;
        private System.Windows.Forms.TableLayoutPanel pnlEditorTop;
        private System.Windows.Forms.FlowLayoutPanel flpStepButtons;
        private System.Windows.Forms.Button btnAddStep;
        private System.Windows.Forms.Button btnRemoveStep;
        private System.Windows.Forms.Button btnRunAutomation;
        private System.Windows.Forms.SplitContainer splitEditor;
        private System.Windows.Forms.ListBox lstSteps;
        private System.Windows.Forms.PropertyGrid pgStepDetails;
        private System.Windows.Forms.ContextMenuStrip ctxDomNode;
        private System.Windows.Forms.ToolStripMenuItem mnuClickElement;
        private System.Windows.Forms.ToolStripMenuItem mnuSetValue;
        private System.Windows.Forms.ToolStripMenuItem mnuAddClickTo;
        private System.Windows.Forms.ToolStripMenuItem mnuAddSetValueTo;
        private System.Windows.Forms.SplitContainer splitSequences;
        private System.Windows.Forms.CheckedListBox chkLstSequences;
        private System.Windows.Forms.FlowLayoutPanel pnlSequencesTop;
        private System.Windows.Forms.Button btnAddSequence;
        private System.Windows.Forms.Button btnRemoveSequence;
        private System.Windows.Forms.Button btnMoveSeqUp;
        private System.Windows.Forms.Button btnMoveSeqDown;
        private System.Windows.Forms.Button btnRenameSeq;
        private System.Windows.Forms.MenuStrip menuStripMain;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem mnuFileNew;
        private System.Windows.Forms.ToolStripMenuItem mnuFileOpen;
        private System.Windows.Forms.ToolStripMenuItem mnuFileSave;
        private System.Windows.Forms.ToolStripSeparator mnuFileSeparator;
        private System.Windows.Forms.ToolStripMenuItem mnuFileExit;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem mnuHelpAbout;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.TabPage tabScraper;
        private System.Windows.Forms.TabPage tabReverseEngineering;
        private System.Windows.Forms.ToolStripSeparator mnuDomSeparator1;
        private System.Windows.Forms.ToolStripMenuItem mnuQuickScrapeText;
        private System.Windows.Forms.ToolStripMenuItem mnuAddScrapeFieldTo;
        private System.Windows.Forms.ToolStripMenuItem mnuAddRepeatingFieldTo;
        private System.Windows.Forms.ToolStripMenuItem mnuSetScrapeContainerFor;
        private System.Windows.Forms.ToolStripMenuItem mnuSetPaginationNextFor;
    }
}