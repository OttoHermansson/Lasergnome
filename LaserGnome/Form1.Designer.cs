namespace Lasergnome
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.button1 = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.isConnectedToLaserOS = new System.Windows.Forms.CheckBox();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.laserOn = new System.Windows.Forms.CheckBox();
            this.simulatorActive = new System.Windows.Forms.CheckBox();
            this.runningAutoRandom = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(94, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(52, 21);
            this.button1.TabIndex = 0;
            this.button1.Text = "Open";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(12, 12);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(76, 21);
            this.comboBox1.TabIndex = 1;
            // 
            // isConnectedToLaserOS
            // 
            this.isConnectedToLaserOS.AutoSize = true;
            this.isConnectedToLaserOS.Enabled = false;
            this.isConnectedToLaserOS.Location = new System.Drawing.Point(12, 39);
            this.isConnectedToLaserOS.Name = "isConnectedToLaserOS";
            this.isConnectedToLaserOS.Size = new System.Drawing.Size(134, 17);
            this.isConnectedToLaserOS.TabIndex = 73;
            this.isConnectedToLaserOS.Text = "Connected to LaserOS";
            this.isConnectedToLaserOS.UseVisualStyleBackColor = true;
            // 
            // trackBar1
            // 
            this.trackBar1.Location = new System.Drawing.Point(12, 131);
            this.trackBar1.Maximum = 15;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(136, 45);
            this.trackBar1.TabIndex = 74;
            this.trackBar1.Value = 15;
            this.trackBar1.ValueChanged += new System.EventHandler(this.trackBar1_ValueChanged);
            // 
            // laserOn
            // 
            this.laserOn.AutoSize = true;
            this.laserOn.Enabled = false;
            this.laserOn.Location = new System.Drawing.Point(12, 62);
            this.laserOn.Name = "laserOn";
            this.laserOn.Size = new System.Drawing.Size(94, 17);
            this.laserOn.TabIndex = 75;
            this.laserOn.Text = "Laser is active";
            this.laserOn.UseVisualStyleBackColor = true;
            // 
            // simulatorActive
            // 
            this.simulatorActive.AutoSize = true;
            this.simulatorActive.Enabled = false;
            this.simulatorActive.Location = new System.Drawing.Point(12, 85);
            this.simulatorActive.Name = "simulatorActive";
            this.simulatorActive.Size = new System.Drawing.Size(101, 17);
            this.simulatorActive.TabIndex = 76;
            this.simulatorActive.Text = "Simulator active";
            this.simulatorActive.UseVisualStyleBackColor = true;
            // 
            // runningAutoRandom
            // 
            this.runningAutoRandom.AutoSize = true;
            this.runningAutoRandom.Enabled = false;
            this.runningAutoRandom.Location = new System.Drawing.Point(12, 108);
            this.runningAutoRandom.Name = "runningAutoRandom";
            this.runningAutoRandom.Size = new System.Drawing.Size(120, 17);
            this.runningAutoRandom.TabIndex = 77;
            this.runningAutoRandom.Text = "Auto Random mode";
            this.runningAutoRandom.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(160, 169);
            this.Controls.Add(this.runningAutoRandom);
            this.Controls.Add(this.simulatorActive);
            this.Controls.Add(this.laserOn);
            this.Controls.Add(this.trackBar1);
            this.Controls.Add(this.isConnectedToLaserOS);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "LaserGnome";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.CheckBox isConnectedToLaserOS;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.CheckBox laserOn;
        private System.Windows.Forms.CheckBox simulatorActive;
        private System.Windows.Forms.CheckBox runningAutoRandom;
    }
}

