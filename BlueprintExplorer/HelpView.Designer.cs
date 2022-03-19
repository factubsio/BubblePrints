
namespace BlueprintExplorer
{
    partial class HelpView
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
            this.contents = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contents
            // 
            this.contents.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.contents.BulletIndent = 2;
            this.contents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contents.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.contents.Location = new System.Drawing.Point(32, 32);
            this.contents.Margin = new System.Windows.Forms.Padding(32);
            this.contents.Name = "contents";
            this.contents.ReadOnly = true;
            this.contents.ShortcutsEnabled = false;
            this.contents.Size = new System.Drawing.Size(1250, 858);
            this.contents.TabIndex = 0;
            this.contents.Text = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.contents);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(32);
            this.panel1.Size = new System.Drawing.Size(1314, 922);
            this.panel1.TabIndex = 1;
            // 
            // HelpView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1314, 922);
            this.Controls.Add(this.panel1);
            this.Name = "HelpView";
            this.Text = "HelpView";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox contents;
        private System.Windows.Forms.Panel panel1;
    }
}