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
            buttonSave = new Button();
            buttonCancel = new Button();
            label1 = new Label();
            textBoxStepsMM = new TextBox();
            groupBoxThreadRod = new GroupBox();
            textBoxGearRatio = new TextBox();
            label5 = new Label();
            label2 = new Label();
            comboBoxStepsPerRevolution = new ComboBox();
            labelMicrosteps = new Label();
            comboBoxMicrosteps = new ComboBox();
            buttonCalculate = new Button();
            label4 = new Label();
            textBoxThreadPitch = new TextBox();
            label3 = new Label();
            comboBoxThreadStarts = new ComboBox();
            textBoxMaxSpeed = new TextBox();
            label6 = new Label();
            textBoxJogSpeed = new TextBox();
            label7 = new Label();
            groupBoxThreadRod.SuspendLayout();
            SuspendLayout();
            // 
            // buttonSave
            // 
            buttonSave.DialogResult = DialogResult.OK;
            buttonSave.Location = new Point(310, 468);
            buttonSave.Margin = new Padding(3, 4, 3, 4);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(83, 40);
            buttonSave.TabIndex = 0;
            buttonSave.Text = "Save";
            buttonSave.UseVisualStyleBackColor = true;
            buttonSave.Click += buttonSave_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.Location = new Point(220, 468);
            buttonCancel.Margin = new Padding(3, 4, 3, 4);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(83, 40);
            buttonCancel.TabIndex = 1;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(23, 311);
            label1.Name = "label1";
            label1.Size = new Size(123, 25);
            label1.TabIndex = 2;
            label1.Text = "Steps per mm";
            // 
            // textBoxStepsMM
            // 
            textBoxStepsMM.Location = new Point(233, 311);
            textBoxStepsMM.Margin = new Padding(3, 4, 3, 4);
            textBoxStepsMM.Name = "textBoxStepsMM";
            textBoxStepsMM.Size = new Size(160, 31);
            textBoxStepsMM.TabIndex = 3;
            textBoxStepsMM.Text = "800";
            textBoxStepsMM.Validating += textBoxStepsMM_Validating;
            // 
            // groupBoxThreadRod
            // 
            groupBoxThreadRod.Controls.Add(textBoxGearRatio);
            groupBoxThreadRod.Controls.Add(label5);
            groupBoxThreadRod.Controls.Add(label2);
            groupBoxThreadRod.Controls.Add(comboBoxStepsPerRevolution);
            groupBoxThreadRod.Controls.Add(labelMicrosteps);
            groupBoxThreadRod.Controls.Add(comboBoxMicrosteps);
            groupBoxThreadRod.Controls.Add(buttonCalculate);
            groupBoxThreadRod.Controls.Add(label4);
            groupBoxThreadRod.Controls.Add(textBoxThreadPitch);
            groupBoxThreadRod.Controls.Add(label3);
            groupBoxThreadRod.Controls.Add(comboBoxThreadStarts);
            groupBoxThreadRod.Location = new Point(13, 15);
            groupBoxThreadRod.Margin = new Padding(3, 4, 3, 4);
            groupBoxThreadRod.Name = "groupBoxThreadRod";
            groupBoxThreadRod.Padding = new Padding(3, 4, 3, 4);
            groupBoxThreadRod.Size = new Size(412, 289);
            groupBoxThreadRod.TabIndex = 6;
            groupBoxThreadRod.TabStop = false;
            groupBoxThreadRod.Text = "Lead Screw Calculator";
            // 
            // textBoxGearRatio
            // 
            textBoxGearRatio.Location = new Point(158, 199);
            textBoxGearRatio.Margin = new Padding(3, 4, 3, 4);
            textBoxGearRatio.Name = "textBoxGearRatio";
            textBoxGearRatio.Size = new Size(222, 31);
            textBoxGearRatio.TabIndex = 18;
            textBoxGearRatio.Text = "1";
            textBoxGearRatio.Validating += textBoxGearRatio_Validating;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(10, 202);
            label5.Name = "label5";
            label5.Size = new Size(128, 25);
            label5.TabIndex = 17;
            label5.Text = "Gear Ration : 1";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(10, 118);
            label2.Name = "label2";
            label2.Size = new Size(142, 25);
            label2.TabIndex = 15;
            label2.Text = "Steps/revolution";
            // 
            // comboBoxStepsPerRevolution
            // 
            comboBoxStepsPerRevolution.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxStepsPerRevolution.FormattingEnabled = true;
            comboBoxStepsPerRevolution.Items.AddRange(new object[] { "200", "400" });
            comboBoxStepsPerRevolution.Location = new Point(158, 114);
            comboBoxStepsPerRevolution.Margin = new Padding(3, 4, 3, 4);
            comboBoxStepsPerRevolution.Name = "comboBoxStepsPerRevolution";
            comboBoxStepsPerRevolution.Size = new Size(222, 33);
            comboBoxStepsPerRevolution.TabIndex = 14;
            // 
            // labelMicrosteps
            // 
            labelMicrosteps.AutoSize = true;
            labelMicrosteps.Location = new Point(10, 160);
            labelMicrosteps.Name = "labelMicrosteps";
            labelMicrosteps.Size = new Size(99, 25);
            labelMicrosteps.TabIndex = 13;
            labelMicrosteps.Text = "Microsteps";
            // 
            // comboBoxMicrosteps
            // 
            comboBoxMicrosteps.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxMicrosteps.FormattingEnabled = true;
            comboBoxMicrosteps.Items.AddRange(new object[] { "1", "2", "4", "8", "16", "32", "64", "128", "256" });
            comboBoxMicrosteps.Location = new Point(158, 156);
            comboBoxMicrosteps.Margin = new Padding(3, 4, 3, 4);
            comboBoxMicrosteps.Name = "comboBoxMicrosteps";
            comboBoxMicrosteps.Size = new Size(222, 33);
            comboBoxMicrosteps.TabIndex = 12;
            // 
            // buttonCalculate
            // 
            buttonCalculate.Location = new Point(158, 241);
            buttonCalculate.Margin = new Padding(3, 4, 3, 4);
            buttonCalculate.Name = "buttonCalculate";
            buttonCalculate.Size = new Size(222, 40);
            buttonCalculate.TabIndex = 11;
            buttonCalculate.Text = "Calculate steps/mm";
            buttonCalculate.UseVisualStyleBackColor = true;
            buttonCalculate.Click += buttonCalculate_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(10, 75);
            label4.Name = "label4";
            label4.Size = new Size(115, 25);
            label4.TabIndex = 10;
            label4.Text = "Thread Starts";
            // 
            // textBoxThreadPitch
            // 
            textBoxThreadPitch.Location = new Point(158, 31);
            textBoxThreadPitch.Margin = new Padding(3, 4, 3, 4);
            textBoxThreadPitch.Name = "textBoxThreadPitch";
            textBoxThreadPitch.Size = new Size(222, 31);
            textBoxThreadPitch.TabIndex = 9;
            textBoxThreadPitch.Text = "2";
            textBoxThreadPitch.Validating += textBoxThreadPitch_Validating;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(10, 35);
            label3.Name = "label3";
            label3.Size = new Size(97, 25);
            label3.TabIndex = 8;
            label3.Text = "Pitch (mm)";
            // 
            // comboBoxThreadStarts
            // 
            comboBoxThreadStarts.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxThreadStarts.FormattingEnabled = true;
            comboBoxThreadStarts.Items.AddRange(new object[] { "1", "2", "3", "4", "5", "6" });
            comboBoxThreadStarts.Location = new Point(158, 71);
            comboBoxThreadStarts.Margin = new Padding(3, 4, 3, 4);
            comboBoxThreadStarts.Name = "comboBoxThreadStarts";
            comboBoxThreadStarts.Size = new Size(222, 33);
            comboBoxThreadStarts.TabIndex = 7;
            // 
            // textBoxMaxSpeed
            // 
            textBoxMaxSpeed.Location = new Point(233, 352);
            textBoxMaxSpeed.Margin = new Padding(3, 4, 3, 4);
            textBoxMaxSpeed.Name = "textBoxMaxSpeed";
            textBoxMaxSpeed.Size = new Size(160, 31);
            textBoxMaxSpeed.TabIndex = 8;
            textBoxMaxSpeed.Text = "100000000";
            textBoxMaxSpeed.Validating += textBoxMaxSpeed_Validating;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(23, 352);
            label6.Name = "label6";
            label6.Size = new Size(197, 25);
            label6.TabIndex = 7;
            label6.Text = "Max Speed (pulses/sec)";
            // 
            // textBoxJogSpeed
            // 
            textBoxJogSpeed.Location = new Point(233, 391);
            textBoxJogSpeed.Margin = new Padding(3, 4, 3, 4);
            textBoxJogSpeed.Name = "textBoxJogSpeed";
            textBoxJogSpeed.Size = new Size(160, 31);
            textBoxJogSpeed.TabIndex = 10;
            textBoxJogSpeed.Text = "20";
            textBoxJogSpeed.Validating += textBoxJogSpeed_Validating;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(23, 391);
            label7.Name = "label7";
            label7.Size = new Size(187, 25);
            label7.TabIndex = 9;
            label7.Text = "Default Jog Speed (%)";
            // 
            // SettingsDialog
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            CancelButton = buttonCancel;
            ClientSize = new Size(442, 530);
            ControlBox = false;
            Controls.Add(textBoxJogSpeed);
            Controls.Add(label7);
            Controls.Add(textBoxMaxSpeed);
            Controls.Add(label6);
            Controls.Add(groupBoxThreadRod);
            Controls.Add(textBoxStepsMM);
            Controls.Add(label1);
            Controls.Add(buttonCancel);
            Controls.Add(buttonSave);
            Margin = new Padding(3, 4, 3, 4);
            Name = "SettingsDialog";
            SizeGripStyle = SizeGripStyle.Hide;
            Text = "Settings";
            Load += SettingsDialog_Load;
            groupBoxThreadRod.ResumeLayout(false);
            groupBoxThreadRod.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonSave;
        private Button buttonCancel;
        private Label label1;
        private TextBox textBoxStepsMM;
        private GroupBox groupBoxThreadRod;
        private ComboBox comboBoxThreadStarts;
        private Label label3;
        private Label label5;
        private Label label2;
        private ComboBox comboBoxStepsPerRevolution;
        private Label labelMicrosteps;
        private ComboBox comboBoxMicrosteps;
        private Button buttonCalculate;
        private Label label4;
        private TextBox textBoxThreadPitch;
        private TextBox textBoxGearRatio;
        private TextBox textBoxMaxSpeed;
        private Label label6;
        private TextBox textBoxJogSpeed;
        private Label label7;
    }
}