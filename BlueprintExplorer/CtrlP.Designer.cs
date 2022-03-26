namespace BlueprintExplorer
{
    partial class CtrlP
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
            this.rootTable = new System.Windows.Forms.TableLayoutPanel();
            this.input = new System.Windows.Forms.TextBox();
            this.root = new System.Windows.Forms.DataGridView();
            this.closeHintLabel = new BlueprintExplorer.BubbleLabel();
            this.rootPanel = new System.Windows.Forms.Panel();
            this.rootTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.root)).BeginInit();
            this.rootPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootTable
            // 
            this.rootTable.ColumnCount = 2;
            this.rootTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.rootTable.Controls.Add(this.input, 0, 0);
            this.rootTable.Controls.Add(this.root, 0, 1);
            this.rootTable.Controls.Add(this.closeHintLabel, 1, 0);
            this.rootTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootTable.Location = new System.Drawing.Point(6, 2);
            this.rootTable.Name = "rootTable";
            this.rootTable.RowCount = 2;
            this.rootTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.rootTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.rootTable.Size = new System.Drawing.Size(1707, 660);
            this.rootTable.TabIndex = 0;
            // 
            // input
            // 
            this.input.Dock = System.Windows.Forms.DockStyle.Fill;
            this.input.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.input.Location = new System.Drawing.Point(3, 3);
            this.input.Name = "input";
            this.input.Size = new System.Drawing.Size(1521, 71);
            this.input.TabIndex = 1;
            // 
            // root
            // 
            this.root.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.rootTable.SetColumnSpan(this.root, 2);
            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.Location = new System.Drawing.Point(3, 78);
            this.root.Name = "root";
            this.root.RowHeadersWidth = 62;
            this.root.RowTemplate.Height = 33;
            this.root.Size = new System.Drawing.Size(1701, 579);
            this.root.TabIndex = 2;
            // 
            // closeHintLabel
            // 
            this.closeHintLabel.Location = new System.Drawing.Point(1530, 16);
            this.closeHintLabel.Margin = new System.Windows.Forms.Padding(3, 16, 3, 3);
            this.closeHintLabel.Marquee = false;
            this.closeHintLabel.Name = "closeHintLabel";
            this.closeHintLabel.OverrideText = null;
            this.closeHintLabel.Size = new System.Drawing.Size(174, 34);
            this.closeHintLabel.TabIndex = 3;
            this.closeHintLabel.Text = "press @{key.esc} to cancel";
            this.closeHintLabel.Text2 = null;
            // 
            // rootPanel
            // 
            this.rootPanel.Controls.Add(this.rootTable);
            this.rootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootPanel.Location = new System.Drawing.Point(0, 0);
            this.rootPanel.Name = "rootPanel";
            this.rootPanel.Padding = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.rootPanel.Size = new System.Drawing.Size(1719, 664);
            this.rootPanel.TabIndex = 4;
            // 
            // CtrlP
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1719, 664);
            this.Controls.Add(this.rootPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CtrlP";
            this.Text = "CtrlP";
            this.rootTable.ResumeLayout(false);
            this.rootTable.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.root)).EndInit();
            this.rootPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel rootTable;
        public System.Windows.Forms.TextBox input;
        private System.Windows.Forms.DataGridView root;
        private BubbleLabel closeHintLabel;
        private System.Windows.Forms.Panel rootPanel;
    }
}