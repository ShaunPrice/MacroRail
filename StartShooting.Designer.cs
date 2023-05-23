namespace MacroRail
{
    partial class StartShooting
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxShootDirectory = new System.Windows.Forms.TextBox();
            this.textBoxShootName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxShootVersion = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.labelStepCount = new System.Windows.Forms.Label();
            this.labelStepSize = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.labelCamera = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.labelShutterSpeed = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.labelApeture = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.labelSensitivity = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonStart = new System.Windows.Forms.Button();
            this.folderBrowserDialogProject = new System.Windows.Forms.FolderBrowserDialog();
            this.buttonSetProjectDirectory = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 79);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(126, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Images directory";
            // 
            // textBoxShootDirectory
            // 
            this.textBoxShootDirectory.Location = new System.Drawing.Point(146, 76);
            this.textBoxShootDirectory.Name = "textBoxShootDirectory";
            this.textBoxShootDirectory.Size = new System.Drawing.Size(408, 26);
            this.textBoxShootDirectory.TabIndex = 1;
            this.textBoxShootDirectory.TextChanged += new System.EventHandler(this.textBoxShootDirectory_TextChanged);
            // 
            // textBoxShootName
            // 
            this.textBoxShootName.Location = new System.Drawing.Point(146, 12);
            this.textBoxShootName.Name = "textBoxShootName";
            this.textBoxShootName.Size = new System.Drawing.Size(438, 26);
            this.textBoxShootName.TabIndex = 5;
            this.textBoxShootName.TextChanged += new System.EventHandler(this.textBoxShootName_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 20);
            this.label3.TabIndex = 4;
            this.label3.Text = "Shoot Name";
            // 
            // textBoxShootVersion
            // 
            this.textBoxShootVersion.Location = new System.Drawing.Point(146, 44);
            this.textBoxShootVersion.Name = "textBoxShootVersion";
            this.textBoxShootVersion.Size = new System.Drawing.Size(438, 26);
            this.textBoxShootVersion.TabIndex = 6;
            this.textBoxShootVersion.TextChanged += new System.EventHandler(this.textBoxShootVersion_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 47);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(110, 20);
            this.label4.TabIndex = 7;
            this.label4.Text = "Shoot Version";
            // 
            // labelStepCount
            // 
            this.labelStepCount.Location = new System.Drawing.Point(142, 116);
            this.labelStepCount.Name = "labelStepCount";
            this.labelStepCount.Size = new System.Drawing.Size(200, 20);
            this.labelStepCount.TabIndex = 8;
            this.labelStepCount.Text = "0";
            // 
            // labelStepSize
            // 
            this.labelStepSize.Location = new System.Drawing.Point(142, 148);
            this.labelStepSize.Name = "labelStepSize";
            this.labelStepSize.Size = new System.Drawing.Size(200, 20);
            this.labelStepSize.TabIndex = 9;
            this.labelStepSize.Text = "0";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(12, 148);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(124, 20);
            this.label5.TabIndex = 11;
            this.label5.Text = "Step Size (mm):";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(12, 116);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(124, 20);
            this.label6.TabIndex = 10;
            this.label6.Text = "Image Count:";
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(12, 179);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(124, 20);
            this.label7.TabIndex = 13;
            this.label7.Text = "Camera:";
            // 
            // labelCamera
            // 
            this.labelCamera.Location = new System.Drawing.Point(142, 179);
            this.labelCamera.Name = "labelCamera";
            this.labelCamera.Size = new System.Drawing.Size(200, 20);
            this.labelCamera.TabIndex = 12;
            this.labelCamera.Text = "none";
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(12, 212);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(124, 20);
            this.label9.TabIndex = 15;
            this.label9.Text = "Shutter Speed:";
            // 
            // labelShutterSpeed
            // 
            this.labelShutterSpeed.Location = new System.Drawing.Point(142, 212);
            this.labelShutterSpeed.Name = "labelShutterSpeed";
            this.labelShutterSpeed.Size = new System.Drawing.Size(200, 20);
            this.labelShutterSpeed.TabIndex = 14;
            this.labelShutterSpeed.Text = "none";
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(12, 242);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(124, 20);
            this.label11.TabIndex = 17;
            this.label11.Text = "Apeture:";
            // 
            // labelApeture
            // 
            this.labelApeture.Location = new System.Drawing.Point(142, 242);
            this.labelApeture.Name = "labelApeture";
            this.labelApeture.Size = new System.Drawing.Size(200, 20);
            this.labelApeture.TabIndex = 16;
            this.labelApeture.Text = "none";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(12, 273);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(124, 20);
            this.label8.TabIndex = 19;
            this.label8.Text = "Sensitivity (ISO):";
            // 
            // labelSensitivity
            // 
            this.labelSensitivity.Location = new System.Drawing.Point(142, 273);
            this.labelSensitivity.Name = "labelSensitivity";
            this.labelSensitivity.Size = new System.Drawing.Size(200, 20);
            this.labelSensitivity.TabIndex = 18;
            this.labelSensitivity.Text = "none";
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(408, 258);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 35);
            this.buttonCancel.TabIndex = 20;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonStart
            // 
            this.buttonStart.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonStart.Location = new System.Drawing.Point(503, 258);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(81, 35);
            this.buttonStart.TabIndex = 21;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // buttonSetProjectDirectory
            // 
            this.buttonSetProjectDirectory.AutoSize = true;
            this.buttonSetProjectDirectory.Location = new System.Drawing.Point(557, 76);
            this.buttonSetProjectDirectory.Name = "buttonSetProjectDirectory";
            this.buttonSetProjectDirectory.Size = new System.Drawing.Size(31, 30);
            this.buttonSetProjectDirectory.TabIndex = 22;
            this.buttonSetProjectDirectory.Text = "...";
            this.buttonSetProjectDirectory.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.buttonSetProjectDirectory.UseVisualStyleBackColor = true;
            this.buttonSetProjectDirectory.Click += new System.EventHandler(this.buttonSetProjectDirectory_Click);
            // 
            // StartShooting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(614, 326);
            this.Controls.Add(this.buttonSetProjectDirectory);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.labelSensitivity);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.labelApeture);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.labelShutterSpeed);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.labelCamera);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.labelStepSize);
            this.Controls.Add(this.labelStepCount);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxShootVersion);
            this.Controls.Add(this.textBoxShootName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxShootDirectory);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartShooting";
            this.ShowIcon = false;
            this.Text = "Start Shooting";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxShootDirectory;
        private System.Windows.Forms.TextBox textBoxShootName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxShootVersion;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelStepCount;
        private System.Windows.Forms.Label labelStepSize;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label labelCamera;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label labelShutterSpeed;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label labelApeture;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label labelSensitivity;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogProject;
        private System.Windows.Forms.Button buttonSetProjectDirectory;
    }
}