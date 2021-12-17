
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.references = new System.Windows.Forms.DataGridView();
            this.From = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.historyBread = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.filter = new System.Windows.Forms.TextBox();
            this.openExternal = new System.Windows.Forms.Button();
            this.toast = new System.Windows.Forms.Label();
            this.view.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.references)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // view
            // 
            this.view.AutoScroll = true;
            this.view.AutoScrollMinSize = new System.Drawing.Size(1, 0);
            this.view.Blueprint = null;
            this.view.Controls.Add(this.toast);
            this.view.Dock = System.Windows.Forms.DockStyle.Fill;
            this.view.Filter = "";
            this.view.LevelIndent = 20;
            this.view.LinkFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.view.Location = new System.Drawing.Point(0, 0);
            this.view.Name = "view";
            this.view.NameColumnWidth = 600;
            this.view.RowHeight = 36;
            this.view.Size = new System.Drawing.Size(1428, 1048);
            this.view.TabIndex = 0;
            this.view.Text = "blueprintControl1";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.splitContainer1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.historyBread, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1809, 1174);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 53);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.view);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.references);
            this.splitContainer1.Size = new System.Drawing.Size(1803, 1048);
            this.splitContainer1.SplitterDistance = 1428;
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
            this.references.Size = new System.Drawing.Size(371, 1048);
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
            this.historyBread.Location = new System.Drawing.Point(3, 1107);
            this.historyBread.Name = "historyBread";
            this.historyBread.Size = new System.Drawing.Size(1803, 44);
            this.historyBread.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanel1.Controls.Add(this.label1);
            this.flowLayoutPanel1.Controls.Add(this.filter);
            this.flowLayoutPanel1.Controls.Add(this.openExternal);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1803, 44);
            this.flowLayoutPanel1.TabIndex = 2;
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
            this.filter.Location = new System.Drawing.Point(97, 3);
            this.filter.Name = "filter";
            this.filter.PlaceholderText = "...";
            this.filter.Size = new System.Drawing.Size(298, 39);
            this.filter.TabIndex = 1;
            // 
            // openExternal
            // 
            this.openExternal.AutoSize = true;
            this.openExternal.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.openExternal.Location = new System.Drawing.Point(401, 3);
            this.openExternal.Name = "openExternal";
            this.openExternal.Size = new System.Drawing.Size(192, 40);
            this.openExternal.TabIndex = 1;
            this.openExternal.Text = "Open In Editor";
            this.openExternal.UseVisualStyleBackColor = true;
            // 
            // toast
            // 
            this.toast.Font = new System.Drawing.Font("Comic Sans MS", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.toast.Location = new System.Drawing.Point(953, 763);
            this.toast.Name = "toast";
            this.toast.Size = new System.Drawing.Size(164, 45);
            this.toast.TabIndex = 0;
            this.toast.Text = "Text Copied";
            this.toast.Visible = false;
            // 
            // BlueprintViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "BlueprintViewer";
            this.Size = new System.Drawing.Size(1809, 1174);
            this.view.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.references)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private BlueprintControl view;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox filter;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FlowLayoutPanel historyBread;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView references;
        private System.Windows.Forms.DataGridViewTextBoxColumn From;
        private System.Windows.Forms.Button openExternal;
        private System.Windows.Forms.Label toast;
    }
}
