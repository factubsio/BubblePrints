
namespace BlueprintExplorer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.panel1 = new System.Windows.Forms.Panel();
            this.controlBar = new System.Windows.Forms.TableLayoutPanel();
            this.helpButton = new System.Windows.Forms.Button();
            this.settingsButton = new System.Windows.Forms.Button();
            this.omniSearch = new System.Windows.Forms.TextBox();
            this.SearchLabel = new System.Windows.Forms.Label();
            this.availableVersions = new System.Windows.Forms.ComboBox();
            this.notifications = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.resultsGrid = new System.Windows.Forms.DataGridView();
            this.BPName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPNamespace = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Score = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPGuid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.blueprintDock = new Krypton.Docking.KryptonDockableWorkspace();
            this.kDockManager = new Krypton.Docking.KryptonDockingManager();
            this.kGlobalManager = new Krypton.Toolkit.KryptonManager(this.components);
            this.panel1.SuspendLayout();
            this.controlBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.blueprintDock)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.controlBar);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(8);
            this.panel1.Size = new System.Drawing.Size(2442, 82);
            this.panel1.TabIndex = 1;
            // 
            // controlBar
            // 
            this.controlBar.ColumnCount = 6;
            this.controlBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.controlBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.controlBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.controlBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.controlBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.controlBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.controlBar.Controls.Add(this.helpButton, 4, 0);
            this.controlBar.Controls.Add(this.settingsButton, 3, 0);
            this.controlBar.Controls.Add(this.omniSearch, 1, 0);
            this.controlBar.Controls.Add(this.SearchLabel, 0, 0);
            this.controlBar.Controls.Add(this.availableVersions, 2, 0);
            this.controlBar.Controls.Add(this.notifications, 5, 0);
            this.controlBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.controlBar.Location = new System.Drawing.Point(8, 8);
            this.controlBar.Name = "controlBar";
            this.controlBar.RowCount = 1;
            this.controlBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.controlBar.Size = new System.Drawing.Size(2426, 66);
            this.controlBar.TabIndex = 2;
            // 
            // helpButton
            // 
            this.helpButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.helpButton.Location = new System.Drawing.Point(2265, 3);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(94, 60);
            this.helpButton.TabIndex = 4;
            this.helpButton.Text = "Help";
            this.helpButton.UseVisualStyleBackColor = true;
            this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
            // 
            // settingsButton
            // 
            this.settingsButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsButton.Location = new System.Drawing.Point(2165, 3);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(94, 60);
            this.settingsButton.TabIndex = 2;
            this.settingsButton.Text = "Settings";
            this.settingsButton.UseVisualStyleBackColor = true;
            // 
            // omniSearch
            // 
            this.omniSearch.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.omniSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.omniSearch.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.omniSearch.Location = new System.Drawing.Point(191, 8);
            this.omniSearch.Margin = new System.Windows.Forms.Padding(8);
            this.omniSearch.Name = "omniSearch";
            this.omniSearch.PlaceholderText = "enter search text...";
            this.omniSearch.Size = new System.Drawing.Size(1763, 54);
            this.omniSearch.TabIndex = 0;
            this.omniSearch.TextChanged += new System.EventHandler(this.omniSearch_TextChanged_1);
            this.omniSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.omniSearch_KeyDown);
            // 
            // SearchLabel
            // 
            this.SearchLabel.AutoSize = true;
            this.SearchLabel.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.SearchLabel.Location = new System.Drawing.Point(2, 0);
            this.SearchLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.SearchLabel.Name = "SearchLabel";
            this.SearchLabel.Size = new System.Drawing.Size(179, 65);
            this.SearchLabel.TabIndex = 1;
            this.SearchLabel.Text = "Search:";
            this.SearchLabel.Click += new System.EventHandler(this.label1_Click);
            // 
            // availableVersions
            // 
            this.availableVersions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.availableVersions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.availableVersions.FormattingEnabled = true;
            this.availableVersions.ItemHeight = 25;
            this.availableVersions.Location = new System.Drawing.Point(1965, 3);
            this.availableVersions.Name = "availableVersions";
            this.availableVersions.Size = new System.Drawing.Size(194, 33);
            this.availableVersions.TabIndex = 3;
            // 
            // notifications
            // 
            this.notifications.BackgroundImage = global::BlueprintExplorer.Properties.Resources.notification;
            this.notifications.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.notifications.Dock = System.Windows.Forms.DockStyle.Fill;
            this.notifications.FlatAppearance.BorderSize = 0;
            this.notifications.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.notifications.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.notifications.ForeColor = System.Drawing.SystemColors.ControlText;
            this.notifications.Location = new System.Drawing.Point(2365, 3);
            this.notifications.Name = "notifications";
            this.notifications.Size = new System.Drawing.Size(58, 60);
            this.notifications.TabIndex = 5;
            this.notifications.Text = "1";
            this.notifications.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 82);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.resultsGrid);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.blueprintDock);
            this.splitContainer1.Panel2.Cursor = System.Windows.Forms.Cursors.Default;
            this.splitContainer1.Size = new System.Drawing.Size(2442, 1167);
            this.splitContainer1.SplitterDistance = 299;
            this.splitContainer1.SplitterWidth = 16;
            this.splitContainer1.TabIndex = 3;
            // 
            // resultsGrid
            // 
            this.resultsGrid.AllowUserToAddRows = false;
            this.resultsGrid.AllowUserToDeleteRows = false;
            this.resultsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.resultsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.BPName,
            this.BPType,
            this.BPNamespace,
            this.Score,
            this.BPGuid});
            this.resultsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultsGrid.Location = new System.Drawing.Point(0, 0);
            this.resultsGrid.MultiSelect = false;
            this.resultsGrid.Name = "resultsGrid";
            this.resultsGrid.ReadOnly = true;
            this.resultsGrid.RowHeadersWidth = 62;
            this.resultsGrid.RowTemplate.Height = 33;
            this.resultsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.resultsGrid.Size = new System.Drawing.Size(2442, 299);
            this.resultsGrid.TabIndex = 2;
            this.resultsGrid.VirtualMode = true;
            this.resultsGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.resultsGrid_CellContentClick);
            this.resultsGrid.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dataGridView1_CellValueNeeded);
            // 
            // BPName
            // 
            this.BPName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.BPName.HeaderText = "Name";
            this.BPName.MinimumWidth = 8;
            this.BPName.Name = "BPName";
            this.BPName.ReadOnly = true;
            // 
            // BPType
            // 
            this.BPType.HeaderText = "Type";
            this.BPType.MinimumWidth = 8;
            this.BPType.Name = "BPType";
            this.BPType.ReadOnly = true;
            this.BPType.Width = 600;
            // 
            // BPNamespace
            // 
            this.BPNamespace.HeaderText = "Namespace";
            this.BPNamespace.MinimumWidth = 500;
            this.BPNamespace.Name = "BPNamespace";
            this.BPNamespace.ReadOnly = true;
            this.BPNamespace.Width = 500;
            // 
            // Score
            // 
            this.Score.HeaderText = "Score";
            this.Score.MinimumWidth = 11;
            this.Score.Name = "Score";
            this.Score.ReadOnly = true;
            this.Score.Width = 225;
            // 
            // BPGuid
            // 
            this.BPGuid.HeaderText = "Guid";
            this.BPGuid.MinimumWidth = 240;
            this.BPGuid.Name = "BPGuid";
            this.BPGuid.ReadOnly = true;
            this.BPGuid.Width = 240;
            // 
            // blueprintDock
            // 
            this.blueprintDock.ActivePage = null;
            this.blueprintDock.AutoHiddenHost = false;
            this.blueprintDock.CompactFlags = ((Krypton.Workspace.CompactFlags)(((Krypton.Workspace.CompactFlags.RemoveEmptyCells | Krypton.Workspace.CompactFlags.RemoveEmptySequences) 
            | Krypton.Workspace.CompactFlags.PromoteLeafs)));
            this.blueprintDock.ContainerBackStyle = Krypton.Toolkit.PaletteBackStyle.FormCustom1;
            this.blueprintDock.ContextMenus.ShowContextMenu = false;
            this.blueprintDock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.blueprintDock.Location = new System.Drawing.Point(0, 0);
            this.blueprintDock.Name = "blueprintDock";
            // 
            // 
            // 
            this.blueprintDock.Root.UniqueName = "ab97069bd5da4900883796bd4f6a8e33";
            this.blueprintDock.Root.WorkspaceControl = this.blueprintDock;
            this.blueprintDock.SeparatorStyle = Krypton.Toolkit.SeparatorStyle.LowProfile;
            this.blueprintDock.ShowMaximizeButton = false;
            this.blueprintDock.Size = new System.Drawing.Size(2442, 852);
            this.blueprintDock.SplitterWidth = 5;
            this.blueprintDock.TabIndex = 0;
            this.blueprintDock.TabStop = true;
            // 
            // kDockManager
            // 
            this.kDockManager.DefaultCloseRequest = Krypton.Docking.DockingCloseRequest.RemovePageAndDispose;
            // 
            // kGlobalManager
            // 
            this.kGlobalManager.GlobalAllowFormChrome = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BlueprintFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ClientSize = new System.Drawing.Size(2442, 1249);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.LinkFont = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.Name = "Form1";
            this.Text = "BlueprintDB";
            this.panel1.ResumeLayout(false);
            this.controlBar.ResumeLayout(false);
            this.controlBar.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.blueprintDock)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView resultsGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPName;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPType;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPNamespace;
        private System.Windows.Forms.DataGridViewTextBoxColumn Score;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPGuid;
        private System.Windows.Forms.TableLayoutPanel controlBar;
        private System.Windows.Forms.TextBox omniSearch;
        private System.Windows.Forms.Label SearchLabel;
        private System.Windows.Forms.Button settingsButton;
        private System.Windows.Forms.ComboBox availableVersions;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Button notifications;
        private Krypton.Docking.KryptonDockableWorkspace blueprintDock;
        private Krypton.Docking.KryptonDockingManager kDockManager;
        private Krypton.Toolkit.KryptonManager kGlobalManager;
    }
}

