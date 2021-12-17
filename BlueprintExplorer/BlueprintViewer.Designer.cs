
namespace BlueprintExplorer
{
    partial class BlueprintViewer
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.view = new BlueprintExplorer.BlueprintControl();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.currentPath = new System.Windows.Forms.TextBox();
            this.filter = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.openExternal = new System.Windows.Forms.Button();
            this.close = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.references = new System.Windows.Forms.DataGridView();
            this.From = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.historyBread = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.references)).BeginInit();
            this.SuspendLayout();
            // 
            // view
            // 
            this.view.AutoScroll = true;
            this.view.AutoScrollMinSize = new System.Drawing.Size(1, 0);
            this.view.Blueprint = null;
            this.view.Dock = System.Windows.Forms.DockStyle.Fill;
            this.view.Filter = "";
            this.view.LevelIndent = 20;
            this.view.LinkFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.view.Location = new System.Drawing.Point(0, 0);
            this.view.Name = "view";
            this.view.NameColumnWidth = 600;
            this.view.RowHeight = 36;
            this.view.Size = new System.Drawing.Size(1845, 1054);
            this.view.TabIndex = 0;
            this.view.Text = "blueprintControl1";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.splitContainer1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.historyBread, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(2336, 1174);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 5;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 400F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.tableLayoutPanel2.Controls.Add(this.currentPath, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.filter, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.openExternal, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.close, 4, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(2330, 48);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // currentPath
            // 
            this.currentPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentPath.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.currentPath.Location = new System.Drawing.Point(703, 3);
            this.currentPath.Name = "currentPath";
            this.currentPath.PlaceholderText = "...";
            this.currentPath.ReadOnly = true;
            this.currentPath.Size = new System.Drawing.Size(1570, 39);
            this.currentPath.TabIndex = 3;
            // 
            // filter
            // 
            this.filter.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.filter.Location = new System.Drawing.Point(103, 3);
            this.filter.Name = "filter";
            this.filter.PlaceholderText = "...";
            this.filter.Size = new System.Drawing.Size(394, 39);
            this.filter.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 38);
            this.label1.TabIndex = 2;
            this.label1.Text = "Filter:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // openExternal
            // 
            this.openExternal.AutoSize = true;
            this.openExternal.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.openExternal.Location = new System.Drawing.Point(503, 3);
            this.openExternal.Name = "openExternal";
            this.openExternal.Size = new System.Drawing.Size(194, 40);
            this.openExternal.TabIndex = 1;
            this.openExternal.Text = "Open In Editor";
            this.openExternal.UseVisualStyleBackColor = true;
            // 
            // close
            // 
            this.close.AutoSize = true;
            this.close.Dock = System.Windows.Forms.DockStyle.Fill;
            this.close.Image = global::BlueprintExplorer.Properties.Resources.close;
            this.close.Location = new System.Drawing.Point(2279, 3);
            this.close.Name = "close";
            this.close.Size = new System.Drawing.Size(48, 42);
            this.close.TabIndex = 4;
            this.close.UseVisualStyleBackColor = true;
            this.close.Click += new System.EventHandler(this.close_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 57);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.view);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.references);
            this.splitContainer1.Size = new System.Drawing.Size(2330, 1054);
            this.splitContainer1.SplitterDistance = 1845;
            this.splitContainer1.TabIndex = 0;
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
            this.references.Size = new System.Drawing.Size(481, 1054);
            this.references.TabIndex = 1;
            // 
            // From
            // 
            this.From.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.From.HeaderText = "Referenced By";
            this.From.MinimumWidth = 8;
            this.From.Name = "From";
            this.From.ReadOnly = true;
            // 
            // historyBread
            // 
            this.historyBread.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.historyBread.Dock = System.Windows.Forms.DockStyle.Fill;
            this.historyBread.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.historyBread.Location = new System.Drawing.Point(3, 1117);
            this.historyBread.Name = "historyBread";
            this.historyBread.Size = new System.Drawing.Size(2330, 54);
            this.historyBread.TabIndex = 0;
            // 
            // BlueprintViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "BlueprintViewer";
            this.Size = new System.Drawing.Size(2336, 1174);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.references)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private BlueprintControl view;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox filter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FlowLayoutPanel historyBread;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView references;
        private System.Windows.Forms.DataGridViewTextBoxColumn From;
        private System.Windows.Forms.Button openExternal;
        private System.Windows.Forms.TextBox currentPath;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button close;
    }
}
