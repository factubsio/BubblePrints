﻿
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.omniSearch = new System.Windows.Forms.TextBox();
            this.SearchLabel = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.resultsGrid = new System.Windows.Forms.DataGridView();
            this.BPName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPNamespace = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Score = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPGuid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.bpProps = new System.Windows.Forms.PropertyGrid();
            this.filter = new System.Windows.Forms.TextBox();
            this.references = new System.Windows.Forms.DataGridView();
            this.From = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.count = new System.Windows.Forms.Label();
            this.historyBread = new System.Windows.Forms.FlowLayoutPanel();
            this.bottomPanel = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.settingsButton = new System.Windows.Forms.Button();
            this.availableVersions = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.references)).BeginInit();
            this.bottomPanel.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tableLayoutPanel1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(8);
            this.panel1.Size = new System.Drawing.Size(2442, 82);
            this.panel1.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.omniSearch, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.SearchLabel, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(8, 8);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(2426, 66);
            this.tableLayoutPanel1.TabIndex = 2;
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
            this.omniSearch.Size = new System.Drawing.Size(2227, 54);
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
            // splitContainer1
            // 
            this.splitContainer1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.resultsGrid);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Panel2.Controls.Add(this.count);
            this.splitContainer1.Panel2.Cursor = System.Windows.Forms.Cursors.Default;
            this.splitContainer1.Size = new System.Drawing.Size(2436, 1081);
            this.splitContainer1.SplitterDistance = 277;
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
            this.resultsGrid.Cursor = System.Windows.Forms.Cursors.Default;
            this.resultsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultsGrid.Location = new System.Drawing.Point(0, 0);
            this.resultsGrid.MultiSelect = false;
            this.resultsGrid.Name = "resultsGrid";
            this.resultsGrid.ReadOnly = true;
            this.resultsGrid.RowHeadersWidth = 62;
            this.resultsGrid.RowTemplate.Height = 33;
            this.resultsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.resultsGrid.Size = new System.Drawing.Size(2436, 277);
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
            // splitContainer2
            // 
            this.splitContainer2.Cursor = System.Windows.Forms.Cursors.Default;
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.bpProps);
            this.splitContainer2.Panel1.Controls.Add(this.filter);
            this.splitContainer2.Panel1.Cursor = System.Windows.Forms.Cursors.Default;
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.references);
            this.splitContainer2.Panel2.Cursor = System.Windows.Forms.Cursors.Default;
            this.splitContainer2.Size = new System.Drawing.Size(2436, 788);
            this.splitContainer2.SplitterDistance = 1771;
            this.splitContainer2.TabIndex = 5;
            // 
            // bpProps
            // 
            this.bpProps.CategorySplitterColor = System.Drawing.SystemColors.ControlLight;
            this.bpProps.Cursor = System.Windows.Forms.Cursors.Default;
            this.bpProps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bpProps.Font = new System.Drawing.Font("Consolas", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.bpProps.Location = new System.Drawing.Point(0, 0);
            this.bpProps.Name = "bpProps";
            this.bpProps.Size = new System.Drawing.Size(1771, 788);
            this.bpProps.TabIndex = 4;
            this.bpProps.Click += new System.EventHandler(this.bpProps_Click);
            // 
            // filter
            // 
            this.filter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.filter.Location = new System.Drawing.Point(1370, 206);
            this.filter.Name = "filter";
            this.filter.PlaceholderText = "Filter Blueprint...";
            this.filter.Size = new System.Drawing.Size(398, 31);
            this.filter.TabIndex = 2;
            this.filter.TextChanged += new System.EventHandler(this.filter_TextChanged);
            // 
            // references
            // 
            this.references.AllowUserToAddRows = false;
            this.references.AllowUserToDeleteRows = false;
            this.references.AllowUserToResizeRows = false;
            this.references.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.references.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.From});
            this.references.Cursor = System.Windows.Forms.Cursors.Default;
            this.references.Dock = System.Windows.Forms.DockStyle.Fill;
            this.references.Location = new System.Drawing.Point(0, 0);
            this.references.MultiSelect = false;
            this.references.Name = "references";
            this.references.ReadOnly = true;
            this.references.RowHeadersVisible = false;
            this.references.RowHeadersWidth = 62;
            this.references.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.references.RowTemplate.Height = 33;
            this.references.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.references.Size = new System.Drawing.Size(661, 788);
            this.references.TabIndex = 0;
            // 
            // From
            // 
            this.From.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.From.HeaderText = "Referenced By";
            this.From.MinimumWidth = 8;
            this.From.Name = "From";
            this.From.ReadOnly = true;
            // 
            // count
            // 
            this.count.AutoSize = true;
            this.count.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.count.Location = new System.Drawing.Point(5, 4);
            this.count.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.count.Name = "count";
            this.count.Size = new System.Drawing.Size(70, 32);
            this.count.TabIndex = 3;
            this.count.Text = "        ";
            // 
            // historyBread
            // 
            this.historyBread.Dock = System.Windows.Forms.DockStyle.Fill;
            this.historyBread.Location = new System.Drawing.Point(3, 3);
            this.historyBread.Name = "historyBread";
            this.historyBread.Size = new System.Drawing.Size(1978, 53);
            this.historyBread.TabIndex = 1;
            // 
            // bottomPanel
            // 
            this.bottomPanel.ColumnCount = 1;
            this.bottomPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomPanel.Controls.Add(this.tableLayoutPanel3, 0, 2);
            this.bottomPanel.Controls.Add(this.splitContainer1, 0, 0);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomPanel.Location = new System.Drawing.Point(0, 82);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.RowCount = 3;
            this.bottomPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.bottomPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 65F));
            this.bottomPanel.Size = new System.Drawing.Size(2442, 1167);
            this.bottomPanel.TabIndex = 5;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 152F));
            this.tableLayoutPanel3.Controls.Add(this.historyBread, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.settingsButton, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.availableVersions, 1, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 1105);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(2436, 59);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // settingsButton
            // 
            this.settingsButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsButton.Location = new System.Drawing.Point(2287, 3);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(146, 53);
            this.settingsButton.TabIndex = 2;
            this.settingsButton.Text = "Settings";
            this.settingsButton.UseVisualStyleBackColor = true;
            // 
            // availableVersions
            // 
            this.availableVersions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.availableVersions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.availableVersions.FormattingEnabled = true;
            this.availableVersions.ItemHeight = 25;
            this.availableVersions.Location = new System.Drawing.Point(1987, 3);
            this.availableVersions.Name = "availableVersions";
            this.availableVersions.Size = new System.Drawing.Size(294, 33);
            this.availableVersions.TabIndex = 3;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2442, 1249);
            this.Controls.Add(this.bottomPanel);
            this.Controls.Add(this.panel1);
            this.Name = "Form1";
            this.Text = "BlueprintDB";
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).EndInit();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.references)).EndInit();
            this.bottomPanel.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.FlowLayoutPanel historyBread;
        private System.Windows.Forms.DataGridView resultsGrid;
        private System.Windows.Forms.TextBox filter;
        private System.Windows.Forms.Label count;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPName;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPType;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPNamespace;
        private System.Windows.Forms.DataGridViewTextBoxColumn Score;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPGuid;
        private System.Windows.Forms.PropertyGrid bpProps;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox omniSearch;
        private System.Windows.Forms.Label SearchLabel;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.DataGridView references;
        private System.Windows.Forms.DataGridViewTextBoxColumn From;
        private System.Windows.Forms.TableLayoutPanel bottomPanel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Button settingsButton;
        private System.Windows.Forms.ComboBox availableVersions;
    }
}

