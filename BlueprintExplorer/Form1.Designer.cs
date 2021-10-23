
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.resultsGrid = new System.Windows.Forms.DataGridView();
            this.BPName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BPGuid = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this.omniSearch.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.omniSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.omniSearch.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.omniSearch.Location = new System.Drawing.Point(8, 8);
            this.omniSearch.Margin = new System.Windows.Forms.Padding(8);
            this.omniSearch.Name = "omniSearch";
            this.omniSearch.PlaceholderText = "Find Blueprint...";
            this.omniSearch.Size = new System.Drawing.Size(2156, 64);
            this.omniSearch.TabIndex = 0;
            this.omniSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.omniSearch_KeyDown);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.omniSearch);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(8);
            this.panel1.Size = new System.Drawing.Size(2172, 82);
            this.panel1.TabIndex = 1;
            // 
            // splitContainer1
            // 
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
            this.splitContainer1.Panel2.Controls.Add(this.filter);
            this.splitContainer1.Panel2.Controls.Add(this.historyBread);
            this.splitContainer1.Panel2.Controls.Add(this.bpView);
            this.splitContainer1.Panel2.Cursor = System.Windows.Forms.Cursors.Default;
            this.splitContainer1.Size = new System.Drawing.Size(2172, 1203);
            this.splitContainer1.SplitterDistance = 316;
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
            this.resultsGrid.Size = new System.Drawing.Size(2172, 316);
            this.resultsGrid.TabIndex = 2;
            this.resultsGrid.VirtualMode = true;
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
            this.BPType.MinimumWidth = 600;
            this.BPType.Name = "BPType";
            this.BPType.ReadOnly = true;
            this.BPType.Width = 600;
            // 
            // BPGuid
            // 
            this.BPGuid.HeaderText = "Guid";
            this.BPGuid.MinimumWidth = 240;
            this.BPGuid.Name = "BPGuid";
            this.BPGuid.ReadOnly = true;
            this.BPGuid.Width = 240;
            // 
            // filter
            // 
            this.filter.Location = new System.Drawing.Point(0, 3);
            this.filter.Name = "filter";
            this.filter.PlaceholderText = "Filter Blueprint...";
            this.filter.Size = new System.Drawing.Size(796, 31);
            this.filter.TabIndex = 2;
            this.filter.TextChanged += new System.EventHandler(this.filter_TextChanged);
            // 
            // historyBread
            // 
            this.historyBread.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.historyBread.Location = new System.Drawing.Point(3, 826);
            this.historyBread.Name = "historyBread";
            this.historyBread.Size = new System.Drawing.Size(2169, 54);
            this.historyBread.TabIndex = 1;
            // 
            // bpView
            // 
            this.bpView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bpView.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.bpView.Indent = 42;
            this.bpView.Location = new System.Drawing.Point(3, 40);
            this.bpView.Name = "bpView";
            this.bpView.Size = new System.Drawing.Size(2169, 780);
            this.bpView.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2172, 1285);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
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
        private System.Windows.Forms.DataGridViewTextBoxColumn BPName;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPType;
        private System.Windows.Forms.DataGridViewTextBoxColumn BPGuid;
        private System.Windows.Forms.TextBox filter;
    }
}

