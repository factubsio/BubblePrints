namespace BlueprintExplorer
{
    partial class SplashScreenChooserJobbie
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
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.title = new System.Windows.Forms.Label();
            this.versions = new System.Windows.Forms.DataGridView();
            this.Version = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Local = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.import = new System.Windows.Forms.Button();
            this.load = new System.Windows.Forms.Button();
            this.mainLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.versions)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.title, 0, 0);
            this.mainLayout.Controls.Add(this.versions, 0, 1);
            this.mainLayout.Controls.Add(this.tableLayoutPanel2, 0, 2);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 3;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.mainLayout.Size = new System.Drawing.Size(1894, 920);
            this.mainLayout.TabIndex = 0;
            this.mainLayout.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
            // 
            // title
            // 
            this.title.AutoSize = true;
            this.title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.title.Location = new System.Drawing.Point(3, 0);
            this.title.Name = "title";
            this.title.Size = new System.Drawing.Size(1888, 50);
            this.title.TabIndex = 0;
            this.title.Text = "BubblePrints";
            // 
            // versions
            // 
            this.versions.AllowUserToAddRows = false;
            this.versions.AllowUserToDeleteRows = false;
            this.versions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.versions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Version,
            this.Local});
            this.versions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.versions.Location = new System.Drawing.Point(3, 53);
            this.versions.Name = "versions";
            this.versions.ReadOnly = true;
            this.versions.RowHeadersWidth = 62;
            this.versions.RowTemplate.Height = 33;
            this.versions.Size = new System.Drawing.Size(1888, 814);
            this.versions.TabIndex = 2;
            // 
            // Version
            // 
            this.Version.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Version.DataPropertyName = "Version";
            this.Version.HeaderText = "Version";
            this.Version.MinimumWidth = 8;
            this.Version.Name = "Version";
            this.Version.ReadOnly = true;
            // 
            // Local
            // 
            this.Local.DataPropertyName = "Local";
            this.Local.HeaderText = "Downloaded";
            this.Local.MinimumWidth = 8;
            this.Local.Name = "Local";
            this.Local.ReadOnly = true;
            this.Local.Width = 150;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.import, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.load, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 873);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(1888, 44);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // import
            // 
            this.import.Dock = System.Windows.Forms.DockStyle.Fill;
            this.import.Location = new System.Drawing.Point(3, 3);
            this.import.Name = "import";
            this.import.Size = new System.Drawing.Size(938, 38);
            this.import.TabIndex = 0;
            this.import.Text = "Import From Game";
            this.import.UseVisualStyleBackColor = true;
            this.import.Click += new System.EventHandler(this.DoImportFromGame);
            // 
            // load
            // 
            this.load.Dock = System.Windows.Forms.DockStyle.Fill;
            this.load.Location = new System.Drawing.Point(947, 3);
            this.load.Name = "load";
            this.load.Size = new System.Drawing.Size(938, 38);
            this.load.TabIndex = 1;
            this.load.Text = "Load Selected";
            this.load.UseVisualStyleBackColor = true;
            this.load.Click += new System.EventHandler(this.DoLoadSelected);
            // 
            // SplashScreenChooserJobbie
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1894, 920);
            this.Controls.Add(this.mainLayout);
            this.Name = "SplashScreenChooserJobbie";
            this.Text = "SplashScreenChooserJobbie";
            this.mainLayout.ResumeLayout(false);
            this.mainLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.versions)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Label title;
        private System.Windows.Forms.DataGridView versions;
        private System.Windows.Forms.DataGridViewTextBoxColumn Version;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Local;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button import;
        private System.Windows.Forms.Button load;
    }
}