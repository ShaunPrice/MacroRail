namespace MacroRail
{
    partial class SaveAs
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
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            label1 = new Label();
            textBoxProjectName = new TextBox();
            textBoxDescription = new TextBox();
            labelDescription = new Label();
            textBoxVersion = new TextBox();
            label3 = new Label();
            saveFileDialog = new SaveFileDialog();
            buttonCancel = new Button();
            buttonSave = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(14, 16);
            label1.Name = "label1";
            label1.Size = new Size(118, 25);
            label1.TabIndex = 0;
            label1.Text = "Project Name";
            // 
            // textBoxProjectName
            // 
            textBoxProjectName.Location = new Point(159, 16);
            textBoxProjectName.Margin = new Padding(3, 4, 3, 4);
            textBoxProjectName.Name = "textBoxProjectName";
            textBoxProjectName.Size = new Size(462, 31);
            textBoxProjectName.TabIndex = 1;
            textBoxProjectName.Text = "Macro Project";
            textBoxProjectName.TextChanged += textBoxProjectName_TextChanged;
            textBoxProjectName.Validating += textBoxProjectName_Validating;
            // 
            // textBoxDescription
            // 
            textBoxDescription.Location = new Point(159, 56);
            textBoxDescription.Margin = new Padding(3, 4, 3, 4);
            textBoxDescription.Name = "textBoxDescription";
            textBoxDescription.Size = new Size(462, 31);
            textBoxDescription.TabIndex = 3;
            textBoxDescription.TextChanged += textBoxDescription_TextChanged;
            // 
            // labelDescription
            // 
            labelDescription.AutoSize = true;
            labelDescription.Location = new Point(14, 56);
            labelDescription.Name = "labelDescription";
            labelDescription.Size = new Size(102, 25);
            labelDescription.TabIndex = 2;
            labelDescription.Text = "Description";
            // 
            // textBoxVersion
            // 
            textBoxVersion.Location = new Point(159, 96);
            textBoxVersion.Margin = new Padding(3, 4, 3, 4);
            textBoxVersion.Name = "textBoxVersion";
            textBoxVersion.Size = new Size(462, 31);
            textBoxVersion.TabIndex = 5;
            textBoxVersion.Text = "0.1.0";
            textBoxVersion.TextChanged += textBoxVersion_TextChanged;
            textBoxVersion.Validating += textBoxVersion_Validating;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(14, 96);
            label3.Name = "label3";
            label3.Size = new Size(129, 25);
            label3.TabIndex = 4;
            label3.Text = "Project Version";
            // 
            // saveFileDialog
            // 
            saveFileDialog.CheckFileExists = true;
            saveFileDialog.DefaultExt = "project";
            saveFileDialog.FileName = "macro";
            saveFileDialog.Filter = "Macro Project|*.project|All Files|*.*";
            // 
            // buttonCancel
            // 
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.Location = new Point(448, 159);
            buttonCancel.Margin = new Padding(3, 4, 3, 4);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(83, 42);
            buttonCancel.TabIndex = 6;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonSave
            // 
            buttonSave.Location = new Point(538, 159);
            buttonSave.Margin = new Padding(3, 4, 3, 4);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(83, 42);
            buttonSave.TabIndex = 7;
            buttonSave.Text = "Save";
            buttonSave.UseVisualStyleBackColor = true;
            buttonSave.Click += buttonSave_Click;
            // 
            // SaveAs
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = buttonCancel;
            ClientSize = new Size(642, 256);
            ControlBox = false;
            Controls.Add(buttonSave);
            Controls.Add(buttonCancel);
            Controls.Add(textBoxVersion);
            Controls.Add(label3);
            Controls.Add(textBoxDescription);
            Controls.Add(labelDescription);
            Controls.Add(textBoxProjectName);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SaveAs";
            ShowIcon = false;
            Text = "Save As";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private Label label1;
        private TextBox textBoxProjectName;
        private TextBox textBoxDescription;
        private Label labelDescription;
        private TextBox textBoxVersion;
        private Label label3;
        private SaveFileDialog saveFileDialog;
        private Button buttonCancel;
        private Button buttonSave;
    }
}