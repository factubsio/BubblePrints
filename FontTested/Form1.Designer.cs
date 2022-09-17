namespace FontTested
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
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.trackBar2 = new System.Windows.Forms.TrackBar();
            this.trackBar3 = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.thicknessLabel = new System.Windows.Forms.Label();
            this.featherLabel = new System.Windows.Forms.Label();
            this.scaleLabel = new System.Windows.Forms.Label();
            this.outlineThicknessBar = new System.Windows.Forms.TrackBar();
            this.outlineFeatherBar = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.outlineFeatherLabel = new System.Windows.Forms.Label();
            this.outlineThicknessLabel = new System.Windows.Forms.Label();
            this.kryptonColorButton1 = new Krypton.Toolkit.KryptonColorButton();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.outlineThicknessBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.outlineFeatherBar)).BeginInit();
            this.SuspendLayout();
            // 
            // trackBar1
            // 
            this.trackBar1.Location = new System.Drawing.Point(947, 382);
            this.trackBar1.Maximum = 600;
            this.trackBar1.Minimum = 400;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(156, 69);
            this.trackBar1.TabIndex = 0;
            this.trackBar1.Value = 500;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // trackBar2
            // 
            this.trackBar2.Location = new System.Drawing.Point(947, 457);
            this.trackBar2.Maximum = 100;
            this.trackBar2.Name = "trackBar2";
            this.trackBar2.Size = new System.Drawing.Size(156, 69);
            this.trackBar2.TabIndex = 1;
            this.trackBar2.Scroll += new System.EventHandler(this.trackBar2_Scroll);
            // 
            // trackBar3
            // 
            this.trackBar3.Location = new System.Drawing.Point(947, 248);
            this.trackBar3.Maximum = 500;
            this.trackBar3.Minimum = 100;
            this.trackBar3.Name = "trackBar3";
            this.trackBar3.Size = new System.Drawing.Size(156, 69);
            this.trackBar3.TabIndex = 2;
            this.trackBar3.Value = 100;
            this.trackBar3.Scroll += new System.EventHandler(this.trackBar3_Scroll);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(842, 382);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 25);
            this.label1.TabIndex = 3;
            this.label1.Text = "Thickness";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(859, 457);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 25);
            this.label2.TabIndex = 4;
            this.label2.Text = "Feather";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(877, 248);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 25);
            this.label3.TabIndex = 5;
            this.label3.Text = "Scale";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // thicknessLabel
            // 
            this.thicknessLabel.AutoSize = true;
            this.thicknessLabel.Location = new System.Drawing.Point(1109, 382);
            this.thicknessLabel.Name = "thicknessLabel";
            this.thicknessLabel.Size = new System.Drawing.Size(36, 25);
            this.thicknessLabel.TabIndex = 6;
            this.thicknessLabel.Text = "0.5";
            // 
            // featherLabel
            // 
            this.featherLabel.AutoSize = true;
            this.featherLabel.Location = new System.Drawing.Point(1109, 457);
            this.featherLabel.Name = "featherLabel";
            this.featherLabel.Size = new System.Drawing.Size(22, 25);
            this.featherLabel.TabIndex = 7;
            this.featherLabel.Text = "0";
            // 
            // scaleLabel
            // 
            this.scaleLabel.AutoSize = true;
            this.scaleLabel.Location = new System.Drawing.Point(1109, 248);
            this.scaleLabel.Name = "scaleLabel";
            this.scaleLabel.Size = new System.Drawing.Size(22, 25);
            this.scaleLabel.TabIndex = 8;
            this.scaleLabel.Text = "1";
            // 
            // outlineThicknessBar
            // 
            this.outlineThicknessBar.Location = new System.Drawing.Point(947, 593);
            this.outlineThicknessBar.Maximum = 600;
            this.outlineThicknessBar.Minimum = 400;
            this.outlineThicknessBar.Name = "outlineThicknessBar";
            this.outlineThicknessBar.Size = new System.Drawing.Size(156, 69);
            this.outlineThicknessBar.TabIndex = 9;
            this.outlineThicknessBar.Value = 500;
            this.outlineThicknessBar.Scroll += new System.EventHandler(this.outlineThicknessBar_Scroll);
            // 
            // outlineFeatherBar
            // 
            this.outlineFeatherBar.Location = new System.Drawing.Point(947, 668);
            this.outlineFeatherBar.Maximum = 100;
            this.outlineFeatherBar.Name = "outlineFeatherBar";
            this.outlineFeatherBar.Size = new System.Drawing.Size(156, 69);
            this.outlineFeatherBar.TabIndex = 10;
            this.outlineFeatherBar.Value = 10;
            this.outlineFeatherBar.Scroll += new System.EventHandler(this.outlineFeatherBar_Scroll);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(780, 593);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(149, 25);
            this.label4.TabIndex = 11;
            this.label4.Text = "Outline Thickness";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(797, 668);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(132, 25);
            this.label5.TabIndex = 12;
            this.label5.Text = "Outline Feather";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // outlineFeatherLabel
            // 
            this.outlineFeatherLabel.AutoSize = true;
            this.outlineFeatherLabel.Location = new System.Drawing.Point(1109, 668);
            this.outlineFeatherLabel.Name = "outlineFeatherLabel";
            this.outlineFeatherLabel.Size = new System.Drawing.Size(36, 25);
            this.outlineFeatherLabel.TabIndex = 13;
            this.outlineFeatherLabel.Text = "0.1";
            // 
            // outlineThicknessLabel
            // 
            this.outlineThicknessLabel.AutoSize = true;
            this.outlineThicknessLabel.Location = new System.Drawing.Point(1109, 593);
            this.outlineThicknessLabel.Name = "outlineThicknessLabel";
            this.outlineThicknessLabel.Size = new System.Drawing.Size(36, 25);
            this.outlineThicknessLabel.TabIndex = 14;
            this.outlineThicknessLabel.Text = "0.5";
            // 
            // kryptonColorButton1
            // 
            this.kryptonColorButton1.Location = new System.Drawing.Point(968, 97);
            this.kryptonColorButton1.Name = "kryptonColorButton1";
            this.kryptonColorButton1.Size = new System.Drawing.Size(135, 38);
            this.kryptonColorButton1.Splitter = false;
            this.kryptonColorButton1.TabIndex = 15;
            this.kryptonColorButton1.Values.Text = "col";
            this.kryptonColorButton1.SelectedColorChanged += new System.EventHandler<Krypton.Toolkit.ColorEventArgs>(this.kryptonColorButton1_SelectedColorChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1202, 788);
            this.Controls.Add(this.kryptonColorButton1);
            this.Controls.Add(this.outlineThicknessLabel);
            this.Controls.Add(this.outlineFeatherLabel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.outlineFeatherBar);
            this.Controls.Add(this.outlineThicknessBar);
            this.Controls.Add(this.scaleLabel);
            this.Controls.Add(this.featherLabel);
            this.Controls.Add(this.thicknessLabel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.trackBar3);
            this.Controls.Add(this.trackBar2);
            this.Controls.Add(this.trackBar1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.outlineThicknessBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.outlineFeatherBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TrackBar trackBar1;
        private TrackBar trackBar2;
        private TrackBar trackBar3;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label thicknessLabel;
        private Label featherLabel;
        private Label scaleLabel;
        private TrackBar outlineThicknessBar;
        private TrackBar outlineFeatherBar;
        private Label label4;
        private Label label5;
        private Label outlineFeatherLabel;
        private Label outlineThicknessLabel;
        private Krypton.Toolkit.KryptonColorButton kryptonColorButton1;
    }
}