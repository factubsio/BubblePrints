
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
            this.topBarContainer = new System.Windows.Forms.Panel();
            this.controlBar = new System.Windows.Forms.TableLayoutPanel();
            this.helpButton = new System.Windows.Forms.Button();
            this.settingsButton = new System.Windows.Forms.Button();
            this.availableVersions = new System.Windows.Forms.ComboBox();
            this.notifications = new System.Windows.Forms.Button();
            this.header = new BlueprintExplorer.BubbleLabel();
            this.blueprintDock = new Krypton.Docking.KryptonDockableWorkspace();
            this.kDockManager = new Krypton.Docking.KryptonDockingManager();
            this.kGlobalManager = new Krypton.Toolkit.KryptonManager(this.components);
            this.blueprintDockContainer = new System.Windows.Forms.Panel();
            this.topBarContainer.SuspendLayout();
            this.controlBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.blueprintDock)).BeginInit();
            this.blueprintDockContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // topBarContainer
            // 
            this.topBarContainer.Controls.Add(this.controlBar);
            this.topBarContainer.Dock = System.Windows.Forms.DockStyle.Top;
            this.topBarContainer.Location = new System.Drawing.Point(0, 0);
            this.topBarContainer.Name = "topBarContainer";
            this.topBarContainer.Padding = new System.Windows.Forms.Padding(9, 8, 9, 8);
            this.topBarContainer.Size = new System.Drawing.Size(2440, 54);
            this.topBarContainer.TabIndex = 1;
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
            this.controlBar.Controls.Add(this.availableVersions, 2, 0);
            this.controlBar.Controls.Add(this.notifications, 5, 0);
            this.controlBar.Controls.Add(this.header, 1, 0);
            this.controlBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.controlBar.Location = new System.Drawing.Point(9, 8);
            this.controlBar.Name = "controlBar";
            this.controlBar.RowCount = 1;
            this.controlBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.controlBar.Size = new System.Drawing.Size(2422, 38);
            this.controlBar.TabIndex = 2;
            // 
            // helpButton
            // 
            this.helpButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.helpButton.Location = new System.Drawing.Point(2261, 3);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(94, 32);
            this.helpButton.TabIndex = 4;
            this.helpButton.TabStop = false;
            this.helpButton.Text = "Help";
            this.helpButton.UseVisualStyleBackColor = true;
            this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
            // 
            // settingsButton
            // 
            this.settingsButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsButton.Location = new System.Drawing.Point(2161, 3);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(94, 32);
            this.settingsButton.TabIndex = 2;
            this.settingsButton.TabStop = false;
            this.settingsButton.Text = "Settings";
            this.settingsButton.UseVisualStyleBackColor = true;
            // 
            // availableVersions
            // 
            this.availableVersions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.availableVersions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.availableVersions.FormattingEnabled = true;
            this.availableVersions.ItemHeight = 25;
            this.availableVersions.Location = new System.Drawing.Point(1961, 3);
            this.availableVersions.Name = "availableVersions";
            this.availableVersions.Size = new System.Drawing.Size(194, 33);
            this.availableVersions.TabIndex = 3;
            // 
            // notifications
            // 
            this.notifications.BackgroundImage = global::BlueprintExplorer.Properties.Resources.notification;
            this.notifications.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.notifications.Dock = System.Windows.Forms.DockStyle.Fill;
            this.notifications.FlatAppearance.BorderSize = 0;
            this.notifications.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.notifications.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.notifications.ForeColor = System.Drawing.SystemColors.ControlText;
            this.notifications.Location = new System.Drawing.Point(2361, 3);
            this.notifications.Name = "notifications";
            this.notifications.Size = new System.Drawing.Size(58, 32);
            this.notifications.TabIndex = 5;
            this.notifications.TabStop = false;
            this.notifications.Text = "1";
            this.notifications.UseVisualStyleBackColor = true;
            // 
            // header
            // 
            this.header.Dock = System.Windows.Forms.DockStyle.Fill;
            this.header.Location = new System.Drawing.Point(3, 3);
            this.header.Marquee = false;
            this.header.Name = "header";
            this.header.OverrideText = null;
            this.header.Size = new System.Drawing.Size(1952, 32);
            this.header.TabIndex = 6;
            this.header.Text = "LOADING...";
            this.header.Text2 = null;
            // 
            // blueprintDock
            // 
            this.blueprintDock.ActivePage = null;
            this.blueprintDock.AutoHiddenHost = false;
            this.blueprintDock.CompactFlags = ((Krypton.Workspace.CompactFlags)(((Krypton.Workspace.CompactFlags.RemoveEmptyCells | Krypton.Workspace.CompactFlags.RemoveEmptySequences) 
            | Krypton.Workspace.CompactFlags.PromoteLeafs)));
            this.blueprintDock.ContainerBackStyle = Krypton.Toolkit.PaletteBackStyle.FormCustom1;
            this.blueprintDock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.blueprintDock.Location = new System.Drawing.Point(3, 3);
            this.blueprintDock.Name = "blueprintDock";
            // 
            // 
            // 
            this.blueprintDock.Root.UniqueName = "ab97069bd5da4900883796bd4f6a8e33";
            this.blueprintDock.Root.WorkspaceControl = this.blueprintDock;
            this.blueprintDock.SeparatorStyle = Krypton.Toolkit.SeparatorStyle.LowProfile;
            this.blueprintDock.ShowMaximizeButton = false;
            this.blueprintDock.Size = new System.Drawing.Size(2434, 1188);
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
            // blueprintDockContainer
            // 
            this.blueprintDockContainer.Controls.Add(this.blueprintDock);
            this.blueprintDockContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.blueprintDockContainer.Location = new System.Drawing.Point(0, 54);
            this.blueprintDockContainer.Name = "blueprintDockContainer";
            this.blueprintDockContainer.Padding = new System.Windows.Forms.Padding(3);
            this.blueprintDockContainer.Size = new System.Drawing.Size(2440, 1194);
            this.blueprintDockContainer.TabIndex = 3;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BlueprintFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ClientSize = new System.Drawing.Size(2440, 1248);
            this.Controls.Add(this.blueprintDockContainer);
            this.Controls.Add(this.topBarContainer);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.LinkFont = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.Name = "Form1";
            this.Text = "BlueprintDB";
            this.topBarContainer.ResumeLayout(false);
            this.controlBar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.blueprintDock)).EndInit();
            this.blueprintDockContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel topBarContainer;
        private System.Windows.Forms.TableLayoutPanel controlBar;
        private System.Windows.Forms.Button settingsButton;
        private System.Windows.Forms.ComboBox availableVersions;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Button notifications;
        private Krypton.Docking.KryptonDockableWorkspace blueprintDock;
        private Krypton.Docking.KryptonDockingManager kDockManager;
        private Krypton.Toolkit.KryptonManager kGlobalManager;
        private BubbleLabel header;
        private System.Windows.Forms.Panel blueprintDockContainer;
    }
}

