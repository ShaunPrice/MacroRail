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
            label1 = new Label();
            textBoxShootDirectory = new TextBox();
            textBoxShootName = new TextBox();
            label3 = new Label();
            textBoxShootVersion = new TextBox();
            label4 = new Label();
            labelStepCount = new Label();
            labelStepSize = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            labelCamera = new Label();
            label9 = new Label();
            labelShutterSpeed = new Label();
            label11 = new Label();
            labelApeture = new Label();
            label8 = new Label();
            labelSensitivity = new Label();
            buttonCancel = new Button();
            buttonStart = new Button();
            folderBrowserDialogProject = new FolderBrowserDialog();
            buttonSetProjectDirectory = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 99);
            label1.Name = "label1";
            label1.Size = new Size(145, 25);
            label1.TabIndex = 0;
            label1.Text = "Images directory";
            // 
            // textBoxShootDirectory
            // 
            textBoxShootDirectory.Location = new Point(162, 95);
            textBoxShootDirectory.Margin = new Padding(3, 4, 3, 4);
            textBoxShootDirectory.Name = "textBoxShootDirectory";
            textBoxShootDirectory.Size = new Size(453, 31);
            textBoxShootDirectory.TabIndex = 1;
            textBoxShootDirectory.TextChanged += textBoxShootDirectory_TextChanged;
            // 
            // textBoxShootName
            // 
            textBoxShootName.ForeColor = SystemColors.WindowText;
            textBoxShootName.Location = new Point(162, 15);
            textBoxShootName.Margin = new Padding(3, 4, 3, 4);
            textBoxShootName.Name = "textBoxShootName";
            textBoxShootName.Size = new Size(486, 31);
            textBoxShootName.TabIndex = 5;
            textBoxShootName.TextChanged += textBoxShootName_TextChanged;
            textBoxShootName.Validating += textBoxShootName_Validating;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(13, 19);
            label3.Name = "label3";
            label3.Size = new Size(112, 25);
            label3.TabIndex = 4;
            label3.Text = "Shoot Name";
            // 
            // textBoxShootVersion
            // 
            textBoxShootVersion.Location = new Point(162, 55);
            textBoxShootVersion.Margin = new Padding(3, 4, 3, 4);
            textBoxShootVersion.Name = "textBoxShootVersion";
            textBoxShootVersion.Size = new Size(486, 31);
            textBoxShootVersion.TabIndex = 6;
            textBoxShootVersion.TextChanged += textBoxShootVersion_TextChanged;
            textBoxShootVersion.Validating += textBoxShootVersion_Validating;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(13, 59);
            label4.Name = "label4";
            label4.Size = new Size(123, 25);
            label4.TabIndex = 7;
            label4.Text = "Shoot Version";
            // 
            // labelStepCount
            // 
            labelStepCount.Location = new Point(158, 145);
            labelStepCount.Name = "labelStepCount";
            labelStepCount.Size = new Size(222, 25);
            labelStepCount.TabIndex = 8;
            labelStepCount.Text = "0";
            // 
            // labelStepSize
            // 
            labelStepSize.Location = new Point(158, 185);
            labelStepSize.Name = "labelStepSize";
            labelStepSize.Size = new Size(222, 25);
            labelStepSize.TabIndex = 9;
            labelStepSize.Text = "0";
            // 
            // label5
            // 
            label5.Location = new Point(13, 185);
            label5.Name = "label5";
            label5.Size = new Size(138, 25);
            label5.TabIndex = 11;
            label5.Text = "Step Size (mm):";
            // 
            // label6
            // 
            label6.Location = new Point(13, 145);
            label6.Name = "label6";
            label6.Size = new Size(138, 25);
            label6.TabIndex = 10;
            label6.Text = "Image Count:";
            // 
            // label7
            // 
            label7.Location = new Point(13, 224);
            label7.Name = "label7";
            label7.Size = new Size(138, 25);
            label7.TabIndex = 13;
            label7.Text = "Camera:";
            // 
            // labelCamera
            // 
            labelCamera.Location = new Point(158, 224);
            labelCamera.Name = "labelCamera";
            labelCamera.Size = new Size(222, 25);
            labelCamera.TabIndex = 12;
            labelCamera.Text = "none";
            // 
            // label9
            // 
            label9.Location = new Point(13, 265);
            label9.Name = "label9";
            label9.Size = new Size(138, 25);
            label9.TabIndex = 15;
            label9.Text = "Shutter Speed:";
            // 
            // labelShutterSpeed
            // 
            labelShutterSpeed.Location = new Point(158, 265);
            labelShutterSpeed.Name = "labelShutterSpeed";
            labelShutterSpeed.Size = new Size(222, 25);
            labelShutterSpeed.TabIndex = 14;
            labelShutterSpeed.Text = "none";
            // 
            // label11
            // 
            label11.Location = new Point(13, 302);
            label11.Name = "label11";
            label11.Size = new Size(138, 25);
            label11.TabIndex = 17;
            label11.Text = "Apeture:";
            // 
            // labelApeture
            // 
            labelApeture.Location = new Point(158, 302);
            labelApeture.Name = "labelApeture";
            labelApeture.Size = new Size(222, 25);
            labelApeture.TabIndex = 16;
            labelApeture.Text = "none";
            // 
            // label8
            // 
            label8.Location = new Point(13, 341);
            label8.Name = "label8";
            label8.Size = new Size(138, 25);
            label8.TabIndex = 19;
            label8.Text = "Sensitivity (ISO):";
            // 
            // labelSensitivity
            // 
            labelSensitivity.Location = new Point(158, 341);
            labelSensitivity.Name = "labelSensitivity";
            labelSensitivity.Size = new Size(222, 25);
            labelSensitivity.TabIndex = 18;
            labelSensitivity.Text = "none";
            // 
            // buttonCancel
            // 
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.Location = new Point(453, 322);
            buttonCancel.Margin = new Padding(3, 4, 3, 4);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(83, 44);
            buttonCancel.TabIndex = 20;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonStart
            // 
            buttonStart.DialogResult = DialogResult.OK;
            buttonStart.Location = new Point(559, 322);
            buttonStart.Margin = new Padding(3, 4, 3, 4);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(90, 44);
            buttonStart.TabIndex = 21;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // buttonSetProjectDirectory
            // 
            buttonSetProjectDirectory.AutoSize = true;
            buttonSetProjectDirectory.Location = new Point(619, 95);
            buttonSetProjectDirectory.Margin = new Padding(3, 4, 3, 4);
            buttonSetProjectDirectory.Name = "buttonSetProjectDirectory";
            buttonSetProjectDirectory.Size = new Size(34, 38);
            buttonSetProjectDirectory.TabIndex = 22;
            buttonSetProjectDirectory.Text = "...";
            buttonSetProjectDirectory.TextAlign = ContentAlignment.TopCenter;
            buttonSetProjectDirectory.UseVisualStyleBackColor = true;
            buttonSetProjectDirectory.Click += buttonSetProjectDirectory_Click;
            // 
            // StartShooting
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(682, 408);
            Controls.Add(buttonSetProjectDirectory);
            Controls.Add(buttonStart);
            Controls.Add(buttonCancel);
            Controls.Add(label8);
            Controls.Add(labelSensitivity);
            Controls.Add(label11);
            Controls.Add(labelApeture);
            Controls.Add(label9);
            Controls.Add(labelShutterSpeed);
            Controls.Add(label7);
            Controls.Add(labelCamera);
            Controls.Add(label5);
            Controls.Add(label6);
            Controls.Add(labelStepSize);
            Controls.Add(labelStepCount);
            Controls.Add(label4);
            Controls.Add(textBoxShootVersion);
            Controls.Add(textBoxShootName);
            Controls.Add(label3);
            Controls.Add(textBoxShootDirectory);
            Controls.Add(label1);
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "StartShooting";
            ShowIcon = false;
            Text = "Start Shooting";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox textBoxShootDirectory;
        private TextBox textBoxShootName;
        private Label label3;
        private TextBox textBoxShootVersion;
        private Label label4;
        private Label labelStepCount;
        private Label labelStepSize;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label labelCamera;
        private Label label9;
        private Label labelShutterSpeed;
        private Label label11;
        private Label labelApeture;
        private Label label8;
        private Label labelSensitivity;
        private Button buttonCancel;
        private Button buttonStart;
        private FolderBrowserDialog folderBrowserDialogProject;
        private Button buttonSetProjectDirectory;
    }
}