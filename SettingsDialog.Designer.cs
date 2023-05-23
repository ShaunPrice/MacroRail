namespace MacroRail
{
    partial class SettingsDialog
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
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxStepsMM = new System.Windows.Forms.TextBox();
            this.groupBoxThreadRod = new System.Windows.Forms.GroupBox();
            this.textBoxGearRatio = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxStepsPerRevolution = new System.Windows.Forms.ComboBox();
            this.labelMicrosteps = new System.Windows.Forms.Label();
            this.comboBoxMicrosteps = new System.Windows.Forms.ComboBox();
            this.buttonCalculate = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxThreadPitch = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxThreadStarts = new System.Windows.Forms.ComboBox();
            this.textBoxMaxSpeed = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBoxThreadRod.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonSave
            // 
            this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonSave.Location = new System.Drawing.Point(279, 323);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 32);
            this.buttonSave.TabIndex = 0;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(198, 323);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 32);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(21, 249);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Steps per mm";
            // 
            // textBoxStepsMM
            // 
            this.textBoxStepsMM.Location = new System.Drawing.Point(210, 249);
            this.textBoxStepsMM.Name = "textBoxStepsMM";
            this.textBoxStepsMM.Size = new System.Drawing.Size(144, 26);
            this.textBoxStepsMM.TabIndex = 3;
            this.textBoxStepsMM.Text = "800";
            // 
            // groupBoxThreadRod
            // 
            this.groupBoxThreadRod.Controls.Add(this.textBoxGearRatio);
            this.groupBoxThreadRod.Controls.Add(this.label5);
            this.groupBoxThreadRod.Controls.Add(this.label2);
            this.groupBoxThreadRod.Controls.Add(this.comboBoxStepsPerRevolution);
            this.groupBoxThreadRod.Controls.Add(this.labelMicrosteps);
            this.groupBoxThreadRod.Controls.Add(this.comboBoxMicrosteps);
            this.groupBoxThreadRod.Controls.Add(this.buttonCalculate);
            this.groupBoxThreadRod.Controls.Add(this.label4);
            this.groupBoxThreadRod.Controls.Add(this.textBoxThreadPitch);
            this.groupBoxThreadRod.Controls.Add(this.label3);
            this.groupBoxThreadRod.Controls.Add(this.comboBoxThreadStarts);
            this.groupBoxThreadRod.Location = new System.Drawing.Point(12, 12);
            this.groupBoxThreadRod.Name = "groupBoxThreadRod";
            this.groupBoxThreadRod.Size = new System.Drawing.Size(371, 231);
            this.groupBoxThreadRod.TabIndex = 6;
            this.groupBoxThreadRod.TabStop = false;
            this.groupBoxThreadRod.Text = "Lead Screw Calculator";
            // 
            // textBoxGearRatio
            // 
            this.textBoxGearRatio.Location = new System.Drawing.Point(142, 159);
            this.textBoxGearRatio.Name = "textBoxGearRatio";
            this.textBoxGearRatio.Size = new System.Drawing.Size(200, 26);
            this.textBoxGearRatio.TabIndex = 18;
            this.textBoxGearRatio.Text = "1";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 162);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(117, 20);
            this.label5.TabIndex = 17;
            this.label5.Text = "Gear Ration : 1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 94);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(123, 20);
            this.label2.TabIndex = 15;
            this.label2.Text = "Steps/revolution";
            // 
            // comboBoxStepsPerRevolution
            // 
            this.comboBoxStepsPerRevolution.FormattingEnabled = true;
            this.comboBoxStepsPerRevolution.Items.AddRange(new object[] {
            "200",
            "400"});
            this.comboBoxStepsPerRevolution.Location = new System.Drawing.Point(142, 91);
            this.comboBoxStepsPerRevolution.Name = "comboBoxStepsPerRevolution";
            this.comboBoxStepsPerRevolution.Size = new System.Drawing.Size(200, 28);
            this.comboBoxStepsPerRevolution.TabIndex = 14;
            // 
            // labelMicrosteps
            // 
            this.labelMicrosteps.AutoSize = true;
            this.labelMicrosteps.Location = new System.Drawing.Point(9, 128);
            this.labelMicrosteps.Name = "labelMicrosteps";
            this.labelMicrosteps.Size = new System.Drawing.Size(86, 20);
            this.labelMicrosteps.TabIndex = 13;
            this.labelMicrosteps.Text = "Microsteps";
            // 
            // comboBoxMicrosteps
            // 
            this.comboBoxMicrosteps.FormattingEnabled = true;
            this.comboBoxMicrosteps.Items.AddRange(new object[] {
            "1",
            "2",
            "4",
            "8",
            "16",
            "32",
            "64",
            "128",
            "256"});
            this.comboBoxMicrosteps.Location = new System.Drawing.Point(142, 125);
            this.comboBoxMicrosteps.Name = "comboBoxMicrosteps";
            this.comboBoxMicrosteps.Size = new System.Drawing.Size(200, 28);
            this.comboBoxMicrosteps.TabIndex = 12;
            // 
            // buttonCalculate
            // 
            this.buttonCalculate.Location = new System.Drawing.Point(142, 193);
            this.buttonCalculate.Name = "buttonCalculate";
            this.buttonCalculate.Size = new System.Drawing.Size(200, 32);
            this.buttonCalculate.TabIndex = 11;
            this.buttonCalculate.Text = "Calculate steps/mm";
            this.buttonCalculate.UseVisualStyleBackColor = true;
            this.buttonCalculate.Click += new System.EventHandler(this.buttonCalculate_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 60);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(106, 20);
            this.label4.TabIndex = 10;
            this.label4.Text = "Thread Starts";
            // 
            // textBoxThreadPitch
            // 
            this.textBoxThreadPitch.Location = new System.Drawing.Point(142, 25);
            this.textBoxThreadPitch.Name = "textBoxThreadPitch";
            this.textBoxThreadPitch.Size = new System.Drawing.Size(200, 26);
            this.textBoxThreadPitch.TabIndex = 9;
            this.textBoxThreadPitch.Text = "2";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 20);
            this.label3.TabIndex = 8;
            this.label3.Text = "Pitch (mm)";
            // 
            // comboBoxThreadStarts
            // 
            this.comboBoxThreadStarts.FormattingEnabled = true;
            this.comboBoxThreadStarts.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6"});
            this.comboBoxThreadStarts.Location = new System.Drawing.Point(142, 57);
            this.comboBoxThreadStarts.Name = "comboBoxThreadStarts";
            this.comboBoxThreadStarts.Size = new System.Drawing.Size(200, 28);
            this.comboBoxThreadStarts.TabIndex = 7;
            this.comboBoxThreadStarts.Text = "4";
            // 
            // textBoxMaxSpeed
            // 
            this.textBoxMaxSpeed.Location = new System.Drawing.Point(210, 282);
            this.textBoxMaxSpeed.Name = "textBoxMaxSpeed";
            this.textBoxMaxSpeed.Size = new System.Drawing.Size(144, 26);
            this.textBoxMaxSpeed.TabIndex = 8;
            this.textBoxMaxSpeed.Text = "100000000";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(21, 282);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(178, 20);
            this.label6.TabIndex = 7;
            this.label6.Text = "Max Speed (pulses/sec)";
            // 
            // SettingsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(398, 364);
            this.ControlBox = false;
            this.Controls.Add(this.textBoxMaxSpeed);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.groupBoxThreadRod);
            this.Controls.Add(this.textBoxStepsMM);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSave);
            this.Name = "SettingsDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingsDialog_Load);
            this.groupBoxThreadRod.ResumeLayout(false);
            this.groupBoxThreadRod.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxStepsMM;
        private System.Windows.Forms.GroupBox groupBoxThreadRod;
        private System.Windows.Forms.ComboBox comboBoxThreadStarts;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxStepsPerRevolution;
        private System.Windows.Forms.Label labelMicrosteps;
        private System.Windows.Forms.ComboBox comboBoxMicrosteps;
        private System.Windows.Forms.Button buttonCalculate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxThreadPitch;
        private System.Windows.Forms.TextBox textBoxGearRatio;
        private System.Windows.Forms.TextBox textBoxMaxSpeed;
        private System.Windows.Forms.Label label6;
    }
}