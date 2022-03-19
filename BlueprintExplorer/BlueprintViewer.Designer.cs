
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
            this.openExternal = new System.Windows.Forms.Button();
            this.copyTemplate = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.filter = new System.Windows.Forms.TextBox();
            this.templatesList = new System.Windows.Forms.ComboBox();
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
            this.view.Size = new System.Drawing.Size(1843, 1060);
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
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(2336, 1174);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 6;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 400F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.currentPath, 5, 0);
            this.tableLayoutPanel2.Controls.Add(this.openExternal, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.copyTemplate, 4, 0);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.filter, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.templatesList, 3, 0);
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
            this.currentPath.Location = new System.Drawing.Point(1023, 3);
            this.currentPath.Name = "currentPath";
            this.currentPath.PlaceholderText = "...";
            this.currentPath.ReadOnly = true;
            this.currentPath.Size = new System.Drawing.Size(1304, 39);
            this.currentPath.TabIndex = 3;
            // 
            // openExternal
            // 
            this.openExternal.AutoSize = true;
            this.openExternal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.openExternal.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.openExternal.Location = new System.Drawing.Point(503, 3);
            this.openExternal.Name = "openExternal";
            this.openExternal.Size = new System.Drawing.Size(194, 42);
            this.openExternal.TabIndex = 1;
            this.openExternal.Text = "Open In Editor";
            this.openExternal.UseVisualStyleBackColor = true;
            // 
            // copyTemplate
            // 
            this.copyTemplate.AutoSize = true;
            this.copyTemplate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.copyTemplate.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.copyTemplate.Location = new System.Drawing.Point(903, 3);
            this.copyTemplate.Name = "copyTemplate";
            this.copyTemplate.Size = new System.Drawing.Size(114, 42);
            this.copyTemplate.TabIndex = 4;
            this.copyTemplate.Text = "Template!";
            this.copyTemplate.UseVisualStyleBackColor = true;
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
            // filter
            // 
            this.filter.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.filter.Location = new System.Drawing.Point(103, 3);
            this.filter.Name = "filter";
            this.filter.PlaceholderText = "...";
            this.filter.Size = new System.Drawing.Size(392, 39);
            this.filter.TabIndex = 1;
            // 
            // templatesList
            // 
            this.templatesList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.templatesList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.templatesList.FormattingEnabled = true;
            this.templatesList.Location = new System.Drawing.Point(703, 3);
            this.templatesList.Name = "templatesList";
            this.templatesList.Size = new System.Drawing.Size(194, 33);
            this.templatesList.TabIndex = 5;
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
            this.splitContainer1.Size = new System.Drawing.Size(2330, 1060);
            this.splitContainer1.SplitterDistance = 1843;
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
            this.references.Size = new System.Drawing.Size(483, 1060);
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
            this.historyBread.Location = new System.Drawing.Point(3, 1123);
            this.historyBread.Name = "historyBread";
            this.historyBread.Size = new System.Drawing.Size(2330, 48);
            this.historyBread.TabIndex = 0;
            // 
            // BlueprintViewer
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
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
        private System.Windows.Forms.Button copyTemplate;
        private System.Windows.Forms.ComboBox templatesList;
    }
}
