
namespace BlueprintExplorer
{
    partial class SettingsView
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
            this.settingsPropView = new System.Windows.Forms.PropertyGrid();
            this.cacheControlButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.deleteBinz = new System.Windows.Forms.Button();
            this.deleteEditorCache = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.formActionButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.formSave = new System.Windows.Forms.Button();
            this.formCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cacheControlButtons.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.formActionButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // settingsPropView
            // 
            this.settingsPropView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsPropView.Location = new System.Drawing.Point(3, 47);
            this.settingsPropView.Name = "settingsPropView";
            this.settingsPropView.Size = new System.Drawing.Size(789, 538);
            this.settingsPropView.TabIndex = 0;
            // 
            // cacheControlButtons
            // 
            this.cacheControlButtons.Controls.Add(this.label1);
            this.cacheControlButtons.Controls.Add(this.deleteBinz);
            this.cacheControlButtons.Controls.Add(this.deleteEditorCache);
            this.cacheControlButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cacheControlButtons.Location = new System.Drawing.Point(3, 3);
            this.cacheControlButtons.Name = "cacheControlButtons";
            this.cacheControlButtons.Size = new System.Drawing.Size(789, 38);
            this.cacheControlButtons.TabIndex = 1;
            // 
            // deleteBinz
            // 
            this.deleteBinz.AutoSize = true;
            this.deleteBinz.Location = new System.Drawing.Point(140, 3);
            this.deleteBinz.Name = "deleteBinz";
            this.deleteBinz.Size = new System.Drawing.Size(146, 35);
            this.deleteBinz.TabIndex = 0;
            this.deleteBinz.Text = "Delete binz files";
            this.deleteBinz.UseVisualStyleBackColor = true;
            this.deleteBinz.Click += new System.EventHandler(this.deleteBinz_Click);
            // 
            // deleteEditorCache
            // 
            this.deleteEditorCache.AutoSize = true;
            this.deleteEditorCache.Location = new System.Drawing.Point(292, 3);
            this.deleteEditorCache.Name = "deleteEditorCache";
            this.deleteEditorCache.Size = new System.Drawing.Size(162, 35);
            this.deleteEditorCache.TabIndex = 1;
            this.deleteEditorCache.Text = "Delete editor-files";
            this.deleteEditorCache.UseVisualStyleBackColor = true;
            this.deleteEditorCache.Click += new System.EventHandler(this.deleteEditorCache_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.formActionButtons, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.cacheControlButtons, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.settingsPropView, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(795, 632);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // formActionButtons
            // 
            this.formActionButtons.Controls.Add(this.formSave);
            this.formActionButtons.Controls.Add(this.formCancel);
            this.formActionButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.formActionButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.formActionButtons.Location = new System.Drawing.Point(3, 591);
            this.formActionButtons.Name = "formActionButtons";
            this.formActionButtons.Size = new System.Drawing.Size(789, 38);
            this.formActionButtons.TabIndex = 2;
            // 
            // formSave
            // 
            this.formSave.Location = new System.Drawing.Point(674, 3);
            this.formSave.Name = "formSave";
            this.formSave.Size = new System.Drawing.Size(112, 34);
            this.formSave.TabIndex = 0;
            this.formSave.Text = "Save";
            this.formSave.UseVisualStyleBackColor = true;
            this.formSave.Click += new System.EventHandler(this.formSave_Click);
            // 
            // formCancel
            // 
            this.formCancel.Location = new System.Drawing.Point(556, 3);
            this.formCancel.Name = "formCancel";
            this.formCancel.Size = new System.Drawing.Size(112, 34);
            this.formCancel.TabIndex = 1;
            this.formCancel.Text = "Cancel";
            this.formCancel.UseVisualStyleBackColor = true;
            this.formCancel.Click += new System.EventHandler(this.formCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 41);
            this.label1.TabIndex = 2;
            this.label1.Text = "Cache Controls";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // SettingsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(795, 632);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "SettingsView";
            this.Text = "BubblePrints Settings";
            this.cacheControlButtons.ResumeLayout(false);
            this.cacheControlButtons.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.formActionButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid settingsPropView;
        private System.Windows.Forms.FlowLayoutPanel cacheControlButtons;
        private System.Windows.Forms.Button deleteBinz;
        private System.Windows.Forms.Button deleteEditorCache;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel formActionButtons;
        private System.Windows.Forms.Button formSave;
        private System.Windows.Forms.Button formCancel;
        private System.Windows.Forms.Label label1;
    }
}