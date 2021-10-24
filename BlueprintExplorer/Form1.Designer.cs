
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
            this.omniSearch = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SearchLabel = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.resultsGrid = new System.Windows.Forms.DataGridView();
            this.BPName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPNamespace = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPGuid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.count = new System.Windows.Forms.Label();
            this.filter = new System.Windows.Forms.TextBox();
            this.historyBread = new System.Windows.Forms.FlowLayoutPanel();
            this.bpView = new System.Windows.Forms.TreeView();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // omniSearch
            // 
            this.omniSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.omniSearch.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.omniSearch.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.omniSearch.Location = new System.Drawing.Point(277, 21);
            this.omniSearch.Margin = new System.Windows.Forms.Padding(12);
            this.omniSearch.Name = "omniSearch";
            this.omniSearch.PlaceholderText = "enter search text...";
            this.omniSearch.Size = new System.Drawing.Size(2960, 80);
            this.omniSearch.TabIndex = 0;
            this.omniSearch.TextChanged += new System.EventHandler(this.omniSearch_TextChanged_1);
            this.omniSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.omniSearch_KeyDown);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.SearchLabel);
            this.panel1.Controls.Add(this.omniSearch);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(12);
            this.panel1.Size = new System.Drawing.Size(3258, 121);
            this.panel1.TabIndex = 1;
            // 
            // SearchLabel
            // 
            this.SearchLabel.AutoSize = true;
            this.SearchLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.SearchLabel.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.SearchLabel.Location = new System.Drawing.Point(12, 12);
            this.SearchLabel.Name = "SearchLabel";
            this.SearchLabel.Size = new System.Drawing.Size(268, 96);
            this.SearchLabel.TabIndex = 1;
            this.SearchLabel.Text = "Search:";
            this.SearchLabel.Click += new System.EventHandler(this.label1_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 121);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.resultsGrid);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.count);
            this.splitContainer1.Panel2.Controls.Add(this.filter);
            this.splitContainer1.Panel2.Controls.Add(this.historyBread);
            this.splitContainer1.Panel2.Controls.Add(this.bpView);
            this.splitContainer1.Panel2.Cursor = System.Windows.Forms.Cursors.Default;
            this.splitContainer1.Size = new System.Drawing.Size(3258, 1781);
            this.splitContainer1.SplitterDistance = 467;
            this.splitContainer1.SplitterWidth = 6;
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
            this.BPGuid});
            this.resultsGrid.Cursor = System.Windows.Forms.Cursors.Default;
            this.resultsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultsGrid.Location = new System.Drawing.Point(0, 0);
            this.resultsGrid.Margin = new System.Windows.Forms.Padding(4);
            this.resultsGrid.MultiSelect = false;
            this.resultsGrid.Name = "resultsGrid";
            this.resultsGrid.ReadOnly = true;
            this.resultsGrid.RowHeadersWidth = 62;
            this.resultsGrid.RowTemplate.Height = 33;
            this.resultsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.resultsGrid.Size = new System.Drawing.Size(3258, 467);
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
            // BPGuid
            // 
            this.BPGuid.HeaderText = "Guid";
            this.BPGuid.MinimumWidth = 240;
            this.BPGuid.Name = "BPGuid";
            this.BPGuid.ReadOnly = true;
            this.BPGuid.Width = 240;
            // 
            // count
            // 
            this.count.AutoSize = true;
            this.count.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.count.Location = new System.Drawing.Point(7, 6);
            this.count.Name = "count";
            this.count.Size = new System.Drawing.Size(100, 48);
            this.count.TabIndex = 3;
            this.count.Text = "        ";
            // 
            // filter
            // 
            this.filter.Location = new System.Drawing.Point(186, 8);
            this.filter.Margin = new System.Windows.Forms.Padding(4);
            this.filter.Name = "filter";
            this.filter.PlaceholderText = "Filter Blueprint...";
            this.filter.Size = new System.Drawing.Size(1460, 43);
            this.filter.TabIndex = 2;
            this.filter.TextChanged += new System.EventHandler(this.filter_TextChanged);
            // 
            // historyBread
            // 
            this.historyBread.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.historyBread.Location = new System.Drawing.Point(4, 1029);
            this.historyBread.Margin = new System.Windows.Forms.Padding(4);
            this.historyBread.Name = "historyBread";
            this.historyBread.Size = new System.Drawing.Size(3254, 80);
            this.historyBread.TabIndex = 1;
            // 
            // bpView
            // 
            this.bpView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bpView.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.bpView.Indent = 42;
            this.bpView.Location = new System.Drawing.Point(4, 59);
            this.bpView.Margin = new System.Windows.Forms.Padding(4);
            this.bpView.Name = "bpView";
            this.bpView.Size = new System.Drawing.Size(3252, 959);
            this.bpView.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 37F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(3258, 1902);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "BlueprintDB";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox omniSearch;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView bpView;
        private System.Windows.Forms.FlowLayoutPanel historyBread;
        private System.Windows.Forms.DataGridView resultsGrid;
        private System.Windows.Forms.TextBox filter;
        private System.Windows.Forms.Label SearchLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPName;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPType;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPNamespace;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPGuid;
        private System.Windows.Forms.Label count;
    }
}

