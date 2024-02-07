namespace ENTcapture
{
    partial class Form4
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form4));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBoxCodec = new System.Windows.Forms.ComboBox();
            this.buttonCompress = new System.Windows.Forms.Button();
            this.labelFilename = new System.Windows.Forms.Label();
            this.labelOrgSize = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxFPS = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxCommandline = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.trackBarComp = new System.Windows.Forms.TrackBar();
            this.label8 = new System.Windows.Forms.Label();
            this.numericUpDownComp = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.textBoxSize = new System.Windows.Forms.TextBox();
            this.textBoxBR = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarComp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownComp)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "ファイル名";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(105, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "オリジナルサイズ(MB)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 149);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "ターゲットサイズ(MB)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(207, 71);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "Codec";
            // 
            // comboBoxCodec
            // 
            this.comboBoxCodec.FormattingEnabled = true;
            this.comboBoxCodec.Location = new System.Drawing.Point(250, 68);
            this.comboBoxCodec.Name = "comboBoxCodec";
            this.comboBoxCodec.Size = new System.Drawing.Size(161, 20);
            this.comboBoxCodec.TabIndex = 1;
            this.comboBoxCodec.TextChanged += new System.EventHandler(this.comboBoxCodec_TextChanged);
            // 
            // buttonCompress
            // 
            this.buttonCompress.Location = new System.Drawing.Point(342, 240);
            this.buttonCompress.Name = "buttonCompress";
            this.buttonCompress.Size = new System.Drawing.Size(86, 23);
            this.buttonCompress.TabIndex = 3;
            this.buttonCompress.Text = "圧縮";
            this.buttonCompress.UseVisualStyleBackColor = true;
            this.buttonCompress.Click += new System.EventHandler(this.buttonCompress_Click);
            // 
            // labelFilename
            // 
            this.labelFilename.AutoSize = true;
            this.labelFilename.Location = new System.Drawing.Point(135, 23);
            this.labelFilename.Name = "labelFilename";
            this.labelFilename.Size = new System.Drawing.Size(35, 12);
            this.labelFilename.TabIndex = 6;
            this.labelFilename.Text = "label5";
            // 
            // labelOrgSize
            // 
            this.labelOrgSize.AutoSize = true;
            this.labelOrgSize.Location = new System.Drawing.Point(135, 49);
            this.labelOrgSize.Name = "labelOrgSize";
            this.labelOrgSize.Size = new System.Drawing.Size(35, 12);
            this.labelOrgSize.TabIndex = 7;
            this.labelOrgSize.Text = "label6";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(250, 240);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(86, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "キャンセル";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(252, 91);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(159, 12);
            this.label5.TabIndex = 8;
            this.label5.Text = "(空欄の場合元ファイルのCodec)";
            // 
            // textBoxFPS
            // 
            this.textBoxFPS.Location = new System.Drawing.Point(135, 68);
            this.textBoxFPS.Name = "textBoxFPS";
            this.textBoxFPS.Size = new System.Drawing.Size(35, 19);
            this.textBoxFPS.TabIndex = 9;
            this.textBoxFPS.TextChanged += new System.EventHandler(this.textBoxFPS_TextChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(22, 75);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(50, 12);
            this.label6.TabIndex = 10;
            this.label6.Text = "FPS指定";
            // 
            // textBoxCommandline
            // 
            this.textBoxCommandline.Location = new System.Drawing.Point(24, 191);
            this.textBoxCommandline.Multiline = true;
            this.textBoxCommandline.Name = "textBoxCommandline";
            this.textBoxCommandline.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxCommandline.Size = new System.Drawing.Size(404, 43);
            this.textBoxCommandline.TabIndex = 11;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(22, 176);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(109, 12);
            this.label7.TabIndex = 12;
            this.label7.Text = "コマンドラインオプション";
            // 
            // trackBarComp
            // 
            this.trackBarComp.LargeChange = 10;
            this.trackBarComp.Location = new System.Drawing.Point(135, 108);
            this.trackBarComp.Maximum = 100;
            this.trackBarComp.Name = "trackBarComp";
            this.trackBarComp.Size = new System.Drawing.Size(227, 45);
            this.trackBarComp.SmallChange = 5;
            this.trackBarComp.TabIndex = 13;
            this.trackBarComp.TickFrequency = 10;
            this.trackBarComp.Scroll += new System.EventHandler(this.trackBarComp_Scroll);
            this.trackBarComp.MouseUp += new System.Windows.Forms.MouseEventHandler(this.trackBarComp_MouseUp);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(22, 117);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(41, 12);
            this.label8.TabIndex = 14;
            this.label8.Text = "圧縮率";
            // 
            // numericUpDownComp
            // 
            this.numericUpDownComp.Location = new System.Drawing.Point(369, 109);
            this.numericUpDownComp.Name = "numericUpDownComp";
            this.numericUpDownComp.Size = new System.Drawing.Size(42, 19);
            this.numericUpDownComp.TabIndex = 15;
            this.numericUpDownComp.ValueChanged += new System.EventHandler(this.numericUpDownComp_ValueChanged);
            this.numericUpDownComp.Scroll += new System.Windows.Forms.ScrollEventHandler(this.numericUpDownComp_Scroll);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(417, 120);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(11, 12);
            this.label9.TabIndex = 16;
            this.label9.Text = "%";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(248, 149);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(55, 12);
            this.label10.TabIndex = 17;
            this.label10.Text = "ビットレート";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(399, 149);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(29, 12);
            this.label11.TabIndex = 19;
            this.label11.Text = "kbps";
            // 
            // textBoxSize
            // 
            this.textBoxSize.Location = new System.Drawing.Point(137, 146);
            this.textBoxSize.Name = "textBoxSize";
            this.textBoxSize.ReadOnly = true;
            this.textBoxSize.Size = new System.Drawing.Size(33, 19);
            this.textBoxSize.TabIndex = 20;
            // 
            // textBoxBR
            // 
            this.textBoxBR.Location = new System.Drawing.Point(321, 146);
            this.textBoxBR.Name = "textBoxBR";
            this.textBoxBR.ReadOnly = true;
            this.textBoxBR.Size = new System.Drawing.Size(72, 19);
            this.textBoxBR.TabIndex = 21;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(176, 149);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(22, 12);
            this.label12.TabIndex = 22;
            this.label12.Text = "MB";
            // 
            // Form4
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(440, 275);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.textBoxBR);
            this.Controls.Add(this.textBoxSize);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.numericUpDownComp);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.trackBarComp);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.textBoxCommandline);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBoxFPS);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.labelOrgSize);
            this.Controls.Add(this.labelFilename);
            this.Controls.Add(this.buttonCompress);
            this.Controls.Add(this.comboBoxCodec);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form4";
            this.Text = "動画圧縮";
            this.Load += new System.EventHandler(this.Form4_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackBarComp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownComp)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxCodec;
        private System.Windows.Forms.Button buttonCompress;
        private System.Windows.Forms.Label labelFilename;
        private System.Windows.Forms.Label labelOrgSize;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxFPS;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxCommandline;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TrackBar trackBarComp;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numericUpDownComp;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox textBoxSize;
        private System.Windows.Forms.TextBox textBoxBR;
        private System.Windows.Forms.Label label12;
    }
}