using Nikon;

namespace MacroRail
{
    partial class Main
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            tableLayoutPanel1 = new TableLayoutPanel();
            labelDOF = new Label();
            pictureBox = new PictureBox();
            labelCameraName = new Label();
            buttonCapture = new Button();
            labelBattery = new Label();
            buttonAutoFocus = new Button();
            labelCompression = new Label();
            labelShutterSpeed = new Label();
            labelApeture = new Label();
            labelSensitivity = new Label();
            labelFlashSyncTime = new Label();
            labelFlashSlowLimit = new Label();
            buttonToggleliveview = new Button();
            comboBoxCompression = new ComboBox();
            comboBoxShutterSpeed = new ComboBox();
            comboBoxApeture = new ComboBox();
            comboBoxSensitivity = new ComboBox();
            comboBoxFlashSyncTime = new ComboBox();
            comboBoxFlashSlowLimit = new ComboBox();
            checkBoxAutoISO = new CheckBox();
            label1 = new Label();
            labelTICConnection = new Label();
            buttonTICDeEnergize = new Button();
            buttonTICResume = new Button();
            buttonTICConnect = new Button();
            buttonGoStart = new Button();
            buttonJogForward = new Button();
            buttonJogBackward = new Button();
            labelJogSpeed = new Label();
            textBoxJogSpeed = new TextBox();
            label4 = new Label();
            buttonStart = new Button();
            buttonPause = new Button();
            buttonStop = new Button();
            labelStepCount = new Label();
            textBoxStepCount = new TextBox();
            label2 = new Label();
            textBoxStepSize = new TextBox();
            labelDelayBeforeShooting = new Label();
            textBoxDelayBeforeShooting = new TextBox();
            label8 = new Label();
            comboBoxCameraSensorSize = new ComboBox();
            label6 = new Label();
            label9 = new Label();
            textBoxLensFocalLength = new TextBox();
            label10 = new Label();
            textBoxMacroTubeSize = new TextBox();
            buttonCalculate = new Button();
            textBoxSubjectDepth = new TextBox();
            label12 = new Label();
            label11 = new Label();
            textBoxSubjectDistance = new TextBox();
            label13 = new Label();
            textBoxDOFAperture = new TextBox();
            labelShotsRequired = new Label();
            labelStepSizeRequired = new Label();
            checkBoxExposureDelay = new CheckBox();
            checkBoxEnableCopyright = new CheckBox();
            labelArtistsName = new Label();
            textBoxArtistsName = new TextBox();
            labelCopyright = new Label();
            textBoxCopyrightInfo = new TextBox();
            checkBoxNoShooting = new CheckBox();
            labelRunStatus = new Label();
            buttonSetZero = new Button();
            labelCurrentPositionValue = new Label();
            labelCurrentPosition = new Label();
            labelTICStatus = new Label();
            checkBoxManualShooting = new CheckBox();
            statusStrip1 = new StatusStrip();
            label3 = new Label();
            label5 = new Label();
            label7 = new Label();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem1 = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            exitApplicationToolStripMenuItem1 = new ToolStripMenuItem();
            toolsToolStripMenuItem1 = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            fileToolStripMenuItem = new ToolStripMenuItem();
            saveSettingsToolStripMenuItem = new ToolStripMenuItem();
            saveSettingsAsToolStripMenuItem = new ToolStripMenuItem();
            openSettingsToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripSeparator2 = new ToolStripSeparator();
            exitApplicationToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            timerTIC = new System.Windows.Forms.Timer(components);
            statusStrip2 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            timerProject = new System.Windows.Forms.Timer(components);
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            menuStrip1.SuspendLayout();
            statusStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.ColumnCount = 4;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 278F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 278F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 278F));
            tableLayoutPanel1.Controls.Add(labelDOF, 3, 20);
            tableLayoutPanel1.Controls.Add(pictureBox, 1, 0);
            tableLayoutPanel1.Controls.Add(labelCameraName, 0, 0);
            tableLayoutPanel1.Controls.Add(buttonCapture, 0, 2);
            tableLayoutPanel1.Controls.Add(labelBattery, 0, 1);
            tableLayoutPanel1.Controls.Add(buttonAutoFocus, 0, 3);
            tableLayoutPanel1.Controls.Add(labelCompression, 0, 5);
            tableLayoutPanel1.Controls.Add(labelShutterSpeed, 0, 7);
            tableLayoutPanel1.Controls.Add(labelApeture, 0, 9);
            tableLayoutPanel1.Controls.Add(labelSensitivity, 0, 12);
            tableLayoutPanel1.Controls.Add(labelFlashSyncTime, 0, 14);
            tableLayoutPanel1.Controls.Add(labelFlashSlowLimit, 0, 16);
            tableLayoutPanel1.Controls.Add(buttonToggleliveview, 0, 4);
            tableLayoutPanel1.Controls.Add(comboBoxCompression, 0, 6);
            tableLayoutPanel1.Controls.Add(comboBoxShutterSpeed, 0, 8);
            tableLayoutPanel1.Controls.Add(comboBoxApeture, 0, 10);
            tableLayoutPanel1.Controls.Add(comboBoxSensitivity, 0, 13);
            tableLayoutPanel1.Controls.Add(comboBoxFlashSyncTime, 0, 15);
            tableLayoutPanel1.Controls.Add(comboBoxFlashSlowLimit, 0, 17);
            tableLayoutPanel1.Controls.Add(checkBoxAutoISO, 0, 11);
            tableLayoutPanel1.Controls.Add(label1, 2, 0);
            tableLayoutPanel1.Controls.Add(labelTICConnection, 2, 1);
            tableLayoutPanel1.Controls.Add(buttonTICDeEnergize, 2, 4);
            tableLayoutPanel1.Controls.Add(buttonTICResume, 2, 3);
            tableLayoutPanel1.Controls.Add(buttonTICConnect, 2, 2);
            tableLayoutPanel1.Controls.Add(buttonGoStart, 2, 5);
            tableLayoutPanel1.Controls.Add(buttonJogForward, 2, 6);
            tableLayoutPanel1.Controls.Add(buttonJogBackward, 2, 7);
            tableLayoutPanel1.Controls.Add(labelJogSpeed, 3, 6);
            tableLayoutPanel1.Controls.Add(textBoxJogSpeed, 3, 7);
            tableLayoutPanel1.Controls.Add(label4, 2, 8);
            tableLayoutPanel1.Controls.Add(buttonStart, 2, 9);
            tableLayoutPanel1.Controls.Add(buttonPause, 2, 10);
            tableLayoutPanel1.Controls.Add(buttonStop, 2, 11);
            tableLayoutPanel1.Controls.Add(labelStepCount, 3, 9);
            tableLayoutPanel1.Controls.Add(textBoxStepCount, 3, 10);
            tableLayoutPanel1.Controls.Add(label2, 3, 11);
            tableLayoutPanel1.Controls.Add(textBoxStepSize, 3, 12);
            tableLayoutPanel1.Controls.Add(labelDelayBeforeShooting, 3, 13);
            tableLayoutPanel1.Controls.Add(textBoxDelayBeforeShooting, 3, 14);
            tableLayoutPanel1.Controls.Add(label8, 2, 15);
            tableLayoutPanel1.Controls.Add(comboBoxCameraSensorSize, 2, 17);
            tableLayoutPanel1.Controls.Add(label6, 2, 16);
            tableLayoutPanel1.Controls.Add(label9, 2, 18);
            tableLayoutPanel1.Controls.Add(textBoxLensFocalLength, 2, 19);
            tableLayoutPanel1.Controls.Add(label10, 2, 20);
            tableLayoutPanel1.Controls.Add(textBoxMacroTubeSize, 2, 21);
            tableLayoutPanel1.Controls.Add(buttonCalculate, 3, 23);
            tableLayoutPanel1.Controls.Add(textBoxSubjectDepth, 3, 19);
            tableLayoutPanel1.Controls.Add(label12, 3, 18);
            tableLayoutPanel1.Controls.Add(label11, 3, 16);
            tableLayoutPanel1.Controls.Add(textBoxSubjectDistance, 3, 17);
            tableLayoutPanel1.Controls.Add(label13, 2, 22);
            tableLayoutPanel1.Controls.Add(textBoxDOFAperture, 2, 23);
            tableLayoutPanel1.Controls.Add(labelShotsRequired, 3, 21);
            tableLayoutPanel1.Controls.Add(labelStepSizeRequired, 3, 22);
            tableLayoutPanel1.Controls.Add(checkBoxExposureDelay, 0, 18);
            tableLayoutPanel1.Controls.Add(checkBoxEnableCopyright, 0, 19);
            tableLayoutPanel1.Controls.Add(labelArtistsName, 0, 20);
            tableLayoutPanel1.Controls.Add(textBoxArtistsName, 0, 21);
            tableLayoutPanel1.Controls.Add(labelCopyright, 0, 22);
            tableLayoutPanel1.Controls.Add(textBoxCopyrightInfo, 0, 23);
            tableLayoutPanel1.Controls.Add(checkBoxNoShooting, 2, 13);
            tableLayoutPanel1.Controls.Add(labelRunStatus, 2, 12);
            tableLayoutPanel1.Controls.Add(buttonSetZero, 3, 5);
            tableLayoutPanel1.Controls.Add(labelCurrentPositionValue, 3, 4);
            tableLayoutPanel1.Controls.Add(labelCurrentPosition, 3, 3);
            tableLayoutPanel1.Controls.Add(labelTICStatus, 3, 2);
            tableLayoutPanel1.Controls.Add(checkBoxManualShooting, 2, 14);
            tableLayoutPanel1.Location = new Point(0, 32);
            tableLayoutPanel1.Margin = new Padding(3, 4, 3, 4);
            tableLayoutPanel1.MinimumSize = new Size(889, 750);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 24;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.Size = new Size(2212, 1262);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // labelDOF
            // 
            labelDOF.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelDOF.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            labelDOF.Location = new Point(1937, 1062);
            labelDOF.Name = "labelDOF";
            labelDOF.Size = new Size(272, 50);
            labelDOF.TabIndex = 65;
            labelDOF.Text = "0mm Depth of Field";
            labelDOF.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pictureBox
            // 
            pictureBox.BackColor = Color.Black;
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.Location = new Point(281, 4);
            pictureBox.Margin = new Padding(3, 4, 3, 4);
            pictureBox.MinimumSize = new Size(356, 300);
            pictureBox.Name = "pictureBox";
            tableLayoutPanel1.SetRowSpan(pictureBox, 24);
            pictureBox.Size = new Size(1372, 1254);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.TabIndex = 2;
            pictureBox.TabStop = false;
            // 
            // labelCameraName
            // 
            labelCameraName.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            labelCameraName.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point);
            labelCameraName.ImageAlign = ContentAlignment.BottomRight;
            labelCameraName.Location = new Point(3, 0);
            labelCameraName.Name = "labelCameraName";
            labelCameraName.Size = new Size(271, 100);
            labelCameraName.TabIndex = 0;
            labelCameraName.Text = "No Camera";
            labelCameraName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // buttonCapture
            // 
            buttonCapture.Location = new Point(0, 162);
            buttonCapture.Margin = new Padding(0);
            buttonCapture.Name = "buttonCapture";
            buttonCapture.Size = new Size(278, 50);
            buttonCapture.TabIndex = 1;
            buttonCapture.Text = "Capture";
            buttonCapture.UseVisualStyleBackColor = true;
            buttonCapture.Click += buttonCapture_Click;
            // 
            // labelBattery
            // 
            labelBattery.Dock = DockStyle.Left;
            labelBattery.Enabled = false;
            labelBattery.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            labelBattery.Location = new Point(3, 100);
            labelBattery.Name = "labelBattery";
            labelBattery.RightToLeft = RightToLeft.Yes;
            labelBattery.Size = new Size(271, 62);
            labelBattery.TabIndex = 0;
            labelBattery.Text = "Battery Level: 0%";
            labelBattery.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // buttonAutoFocus
            // 
            buttonAutoFocus.Enabled = false;
            buttonAutoFocus.Location = new Point(0, 212);
            buttonAutoFocus.Margin = new Padding(0);
            buttonAutoFocus.Name = "buttonAutoFocus";
            buttonAutoFocus.Size = new Size(278, 50);
            buttonAutoFocus.TabIndex = 4;
            buttonAutoFocus.Text = "Auto Focus";
            buttonAutoFocus.UseVisualStyleBackColor = true;
            buttonAutoFocus.Click += buttonAutoFocus_Click;
            // 
            // labelCompression
            // 
            labelCompression.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelCompression.Enabled = false;
            labelCompression.Location = new Point(3, 333);
            labelCompression.Name = "labelCompression";
            labelCompression.Size = new Size(271, 29);
            labelCompression.TabIndex = 0;
            labelCompression.Text = "Compression";
            // 
            // labelShutterSpeed
            // 
            labelShutterSpeed.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelShutterSpeed.Enabled = false;
            labelShutterSpeed.Location = new Point(3, 433);
            labelShutterSpeed.Name = "labelShutterSpeed";
            labelShutterSpeed.Size = new Size(271, 29);
            labelShutterSpeed.TabIndex = 0;
            labelShutterSpeed.Text = "Shutter Speed";
            // 
            // labelApeture
            // 
            labelApeture.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelApeture.Enabled = false;
            labelApeture.Location = new Point(3, 533);
            labelApeture.Name = "labelApeture";
            labelApeture.Size = new Size(271, 29);
            labelApeture.TabIndex = 0;
            labelApeture.Text = "Aperture";
            // 
            // labelSensitivity
            // 
            labelSensitivity.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelSensitivity.Enabled = false;
            labelSensitivity.Location = new Point(3, 683);
            labelSensitivity.Name = "labelSensitivity";
            labelSensitivity.Size = new Size(271, 29);
            labelSensitivity.TabIndex = 0;
            labelSensitivity.Text = "Sensitivity (ISO)";
            // 
            // labelFlashSyncTime
            // 
            labelFlashSyncTime.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelFlashSyncTime.Enabled = false;
            labelFlashSyncTime.Location = new Point(3, 783);
            labelFlashSyncTime.Name = "labelFlashSyncTime";
            labelFlashSyncTime.Size = new Size(271, 29);
            labelFlashSyncTime.TabIndex = 0;
            labelFlashSyncTime.Text = "Flash Sync Time";
            // 
            // labelFlashSlowLimit
            // 
            labelFlashSlowLimit.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelFlashSlowLimit.Enabled = false;
            labelFlashSlowLimit.Location = new Point(3, 883);
            labelFlashSlowLimit.Name = "labelFlashSlowLimit";
            labelFlashSlowLimit.Size = new Size(271, 29);
            labelFlashSlowLimit.TabIndex = 0;
            labelFlashSlowLimit.Text = "Flash Slow Limit";
            // 
            // buttonToggleliveview
            // 
            buttonToggleliveview.Location = new Point(0, 262);
            buttonToggleliveview.Margin = new Padding(0);
            buttonToggleliveview.Name = "buttonToggleliveview";
            buttonToggleliveview.Size = new Size(278, 50);
            buttonToggleliveview.TabIndex = 5;
            buttonToggleliveview.Text = "Toggle Liveview";
            buttonToggleliveview.UseVisualStyleBackColor = true;
            buttonToggleliveview.Click += buttonToggleliveview_Click;
            // 
            // comboBoxCompression
            // 
            comboBoxCompression.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxCompression.Enabled = false;
            comboBoxCompression.FormattingEnabled = true;
            comboBoxCompression.Location = new Point(3, 366);
            comboBoxCompression.Margin = new Padding(3, 4, 3, 4);
            comboBoxCompression.Name = "comboBoxCompression";
            comboBoxCompression.Size = new Size(271, 33);
            comboBoxCompression.TabIndex = 6;
            comboBoxCompression.SelectedIndexChanged += comboBoxCompression_SelectedIndexChanged;
            // 
            // comboBoxShutterSpeed
            // 
            comboBoxShutterSpeed.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxShutterSpeed.Enabled = false;
            comboBoxShutterSpeed.FormattingEnabled = true;
            comboBoxShutterSpeed.Location = new Point(3, 466);
            comboBoxShutterSpeed.Margin = new Padding(3, 4, 3, 4);
            comboBoxShutterSpeed.Name = "comboBoxShutterSpeed";
            comboBoxShutterSpeed.Size = new Size(271, 33);
            comboBoxShutterSpeed.TabIndex = 7;
            comboBoxShutterSpeed.SelectedIndexChanged += comboBoxShutterSpeed_SelectedIndexChanged;
            // 
            // comboBoxApeture
            // 
            comboBoxApeture.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxApeture.Enabled = false;
            comboBoxApeture.FormattingEnabled = true;
            comboBoxApeture.Location = new Point(3, 566);
            comboBoxApeture.Margin = new Padding(3, 4, 3, 4);
            comboBoxApeture.Name = "comboBoxApeture";
            comboBoxApeture.Size = new Size(271, 33);
            comboBoxApeture.TabIndex = 8;
            comboBoxApeture.SelectedIndexChanged += comboBoxApeture_SelectedIndexChanged;
            // 
            // comboBoxSensitivity
            // 
            comboBoxSensitivity.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxSensitivity.Enabled = false;
            comboBoxSensitivity.FormattingEnabled = true;
            comboBoxSensitivity.Location = new Point(3, 716);
            comboBoxSensitivity.Margin = new Padding(3, 4, 3, 4);
            comboBoxSensitivity.Name = "comboBoxSensitivity";
            comboBoxSensitivity.Size = new Size(271, 33);
            comboBoxSensitivity.TabIndex = 10;
            comboBoxSensitivity.SelectedIndexChanged += comboBoxSensitivity_SelectedIndexChanged;
            // 
            // comboBoxFlashSyncTime
            // 
            comboBoxFlashSyncTime.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFlashSyncTime.Enabled = false;
            comboBoxFlashSyncTime.FormattingEnabled = true;
            comboBoxFlashSyncTime.Location = new Point(3, 816);
            comboBoxFlashSyncTime.Margin = new Padding(3, 4, 3, 4);
            comboBoxFlashSyncTime.Name = "comboBoxFlashSyncTime";
            comboBoxFlashSyncTime.Size = new Size(271, 33);
            comboBoxFlashSyncTime.TabIndex = 11;
            comboBoxFlashSyncTime.SelectedIndexChanged += comboBoxFlashSyncTime_SelectedIndexChanged;
            // 
            // comboBoxFlashSlowLimit
            // 
            comboBoxFlashSlowLimit.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFlashSlowLimit.Enabled = false;
            comboBoxFlashSlowLimit.FormattingEnabled = true;
            comboBoxFlashSlowLimit.Location = new Point(3, 916);
            comboBoxFlashSlowLimit.Margin = new Padding(3, 4, 3, 4);
            comboBoxFlashSlowLimit.Name = "comboBoxFlashSlowLimit";
            comboBoxFlashSlowLimit.Size = new Size(271, 33);
            comboBoxFlashSlowLimit.TabIndex = 12;
            comboBoxFlashSlowLimit.SelectedIndexChanged += comboBoxFlashSlowLimit_SelectedIndexChanged;
            // 
            // checkBoxAutoISO
            // 
            checkBoxAutoISO.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            checkBoxAutoISO.Enabled = false;
            checkBoxAutoISO.Location = new Point(3, 628);
            checkBoxAutoISO.Margin = new Padding(3, 4, 3, 4);
            checkBoxAutoISO.Name = "checkBoxAutoISO";
            checkBoxAutoISO.Size = new Size(271, 30);
            checkBoxAutoISO.TabIndex = 9;
            checkBoxAutoISO.Text = "Auto ISO";
            checkBoxAutoISO.UseVisualStyleBackColor = true;
            checkBoxAutoISO.CheckedChanged += checkBoxAutoISO_CheckedChanged;
            // 
            // label1
            // 
            tableLayoutPanel1.SetColumnSpan(label1, 2);
            label1.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(1659, 0);
            label1.Name = "label1";
            label1.Size = new Size(549, 100);
            label1.TabIndex = 17;
            label1.Text = "Macro Rail";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelTICConnection
            // 
            tableLayoutPanel1.SetColumnSpan(labelTICConnection, 2);
            labelTICConnection.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            labelTICConnection.ForeColor = Color.Red;
            labelTICConnection.Location = new Point(1659, 100);
            labelTICConnection.Name = "labelTICConnection";
            labelTICConnection.Size = new Size(549, 62);
            labelTICConnection.TabIndex = 18;
            labelTICConnection.Text = "Not Connected";
            labelTICConnection.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // buttonTICDeEnergize
            // 
            buttonTICDeEnergize.BackColor = SystemColors.Control;
            buttonTICDeEnergize.Enabled = false;
            buttonTICDeEnergize.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            buttonTICDeEnergize.ForeColor = SystemColors.ControlText;
            buttonTICDeEnergize.Location = new Point(1656, 262);
            buttonTICDeEnergize.Margin = new Padding(0);
            buttonTICDeEnergize.Name = "buttonTICDeEnergize";
            buttonTICDeEnergize.Size = new Size(278, 50);
            buttonTICDeEnergize.TabIndex = 19;
            buttonTICDeEnergize.Text = "TIC De-energize";
            buttonTICDeEnergize.UseVisualStyleBackColor = true;
            buttonTICDeEnergize.Click += buttonTICDeEnergize_Click;
            // 
            // buttonTICResume
            // 
            buttonTICResume.Enabled = false;
            buttonTICResume.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            buttonTICResume.Location = new Point(1656, 212);
            buttonTICResume.Margin = new Padding(0);
            buttonTICResume.Name = "buttonTICResume";
            buttonTICResume.Size = new Size(278, 50);
            buttonTICResume.TabIndex = 18;
            buttonTICResume.Text = "TIC Resume";
            buttonTICResume.UseVisualStyleBackColor = true;
            buttonTICResume.Click += buttonTICResume_Click;
            // 
            // buttonTICConnect
            // 
            buttonTICConnect.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            buttonTICConnect.Location = new Point(1656, 162);
            buttonTICConnect.Margin = new Padding(0);
            buttonTICConnect.Name = "buttonTICConnect";
            buttonTICConnect.Size = new Size(278, 50);
            buttonTICConnect.TabIndex = 17;
            buttonTICConnect.Text = "TIC Connect";
            buttonTICConnect.UseVisualStyleBackColor = true;
            buttonTICConnect.Click += buttonTICConnect_Click_1;
            // 
            // buttonGoStart
            // 
            buttonGoStart.Enabled = false;
            buttonGoStart.Location = new Point(1656, 312);
            buttonGoStart.Margin = new Padding(0);
            buttonGoStart.Name = "buttonGoStart";
            buttonGoStart.Size = new Size(278, 50);
            buttonGoStart.TabIndex = 20;
            buttonGoStart.Text = "Go To Start";
            buttonGoStart.UseVisualStyleBackColor = true;
            buttonGoStart.Click += buttonGoStart_Click;
            // 
            // buttonJogForward
            // 
            buttonJogForward.Enabled = false;
            buttonJogForward.Location = new Point(1656, 362);
            buttonJogForward.Margin = new Padding(0);
            buttonJogForward.Name = "buttonJogForward";
            buttonJogForward.Size = new Size(278, 50);
            buttonJogForward.TabIndex = 21;
            buttonJogForward.Text = "Jog Forwards";
            buttonJogForward.UseVisualStyleBackColor = true;
            buttonJogForward.MouseDown += buttonJogForward_MouseDown;
            buttonJogForward.MouseUp += buttonJogForward_MouseUp;
            // 
            // buttonJogBackward
            // 
            buttonJogBackward.Enabled = false;
            buttonJogBackward.Location = new Point(1656, 412);
            buttonJogBackward.Margin = new Padding(0);
            buttonJogBackward.Name = "buttonJogBackward";
            buttonJogBackward.Size = new Size(278, 50);
            buttonJogBackward.TabIndex = 22;
            buttonJogBackward.Text = "Jog Backwards";
            buttonJogBackward.UseVisualStyleBackColor = true;
            buttonJogBackward.MouseDown += buttonJogBackward_MouseDown;
            buttonJogBackward.MouseUp += buttonJogBackward_MouseUp;
            // 
            // labelJogSpeed
            // 
            labelJogSpeed.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelJogSpeed.Location = new Point(1937, 383);
            labelJogSpeed.Name = "labelJogSpeed";
            labelJogSpeed.Size = new Size(271, 29);
            labelJogSpeed.TabIndex = 35;
            labelJogSpeed.Text = "Jog Speed %";
            // 
            // textBoxJogSpeed
            // 
            textBoxJogSpeed.Location = new Point(1937, 416);
            textBoxJogSpeed.Margin = new Padding(3, 4, 3, 4);
            textBoxJogSpeed.Name = "textBoxJogSpeed";
            textBoxJogSpeed.Size = new Size(271, 31);
            textBoxJogSpeed.TabIndex = 25;
            textBoxJogSpeed.Text = "20";
            textBoxJogSpeed.Validating += textBoxJogSpeed_Validating;
            // 
            // label4
            // 
            tableLayoutPanel1.SetColumnSpan(label4, 2);
            label4.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(1659, 462);
            label4.Name = "label4";
            label4.Size = new Size(549, 44);
            label4.TabIndex = 46;
            label4.Text = "Shooting";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // buttonStart
            // 
            buttonStart.BackColor = Color.FromArgb(192, 255, 192);
            buttonStart.Enabled = false;
            buttonStart.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            buttonStart.Location = new Point(1656, 512);
            buttonStart.Margin = new Padding(0);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(278, 50);
            buttonStart.TabIndex = 26;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = false;
            buttonStart.Click += buttonStart_Click;
            // 
            // buttonPause
            // 
            buttonPause.BackColor = Color.FromArgb(255, 255, 192);
            buttonPause.Enabled = false;
            buttonPause.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            buttonPause.Location = new Point(1656, 562);
            buttonPause.Margin = new Padding(0);
            buttonPause.Name = "buttonPause";
            buttonPause.Size = new Size(278, 50);
            buttonPause.TabIndex = 27;
            buttonPause.Text = "Pause";
            buttonPause.UseVisualStyleBackColor = false;
            buttonPause.Click += buttonPause_Click;
            // 
            // buttonStop
            // 
            buttonStop.BackColor = Color.FromArgb(255, 192, 192);
            buttonStop.Enabled = false;
            buttonStop.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            buttonStop.Location = new Point(1656, 612);
            buttonStop.Margin = new Padding(0);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(278, 50);
            buttonStop.TabIndex = 28;
            buttonStop.Text = "Stop";
            buttonStop.UseVisualStyleBackColor = false;
            buttonStop.Click += buttonStop_Click;
            // 
            // labelStepCount
            // 
            labelStepCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelStepCount.Location = new Point(1937, 533);
            labelStepCount.Name = "labelStepCount";
            labelStepCount.Size = new Size(271, 29);
            labelStepCount.TabIndex = 24;
            labelStepCount.Text = "Step Count";
            // 
            // textBoxStepCount
            // 
            textBoxStepCount.Location = new Point(1937, 566);
            textBoxStepCount.Margin = new Padding(3, 4, 3, 4);
            textBoxStepCount.Name = "textBoxStepCount";
            textBoxStepCount.Size = new Size(271, 31);
            textBoxStepCount.TabIndex = 32;
            textBoxStepCount.Text = "1";
            textBoxStepCount.Validating += textBoxStepCount_Validating;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label2.Location = new Point(1937, 633);
            label2.Name = "label2";
            label2.Size = new Size(271, 29);
            label2.TabIndex = 19;
            label2.Text = "Step Size (mm)";
            // 
            // textBoxStepSize
            // 
            textBoxStepSize.Location = new Point(1937, 666);
            textBoxStepSize.Margin = new Padding(3, 4, 3, 4);
            textBoxStepSize.Name = "textBoxStepSize";
            textBoxStepSize.Size = new Size(271, 31);
            textBoxStepSize.TabIndex = 33;
            textBoxStepSize.Text = "1";
            textBoxStepSize.Validating += textBoxStepSize_Validating;
            // 
            // labelDelayBeforeShooting
            // 
            labelDelayBeforeShooting.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelDelayBeforeShooting.Location = new Point(1937, 733);
            labelDelayBeforeShooting.Name = "labelDelayBeforeShooting";
            labelDelayBeforeShooting.Size = new Size(271, 29);
            labelDelayBeforeShooting.TabIndex = 39;
            labelDelayBeforeShooting.Text = "Delay Before Shooting (sec)";
            // 
            // textBoxDelayBeforeShooting
            // 
            textBoxDelayBeforeShooting.Location = new Point(1937, 766);
            textBoxDelayBeforeShooting.Margin = new Padding(3, 4, 3, 4);
            textBoxDelayBeforeShooting.Name = "textBoxDelayBeforeShooting";
            textBoxDelayBeforeShooting.Size = new Size(271, 31);
            textBoxDelayBeforeShooting.TabIndex = 34;
            textBoxDelayBeforeShooting.Text = "0";
            textBoxDelayBeforeShooting.Validating += textBoxDelayBeforeShooting_Validating;
            // 
            // label8
            // 
            tableLayoutPanel1.SetColumnSpan(label8, 2);
            label8.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label8.Location = new Point(1659, 812);
            label8.Name = "label8";
            label8.Size = new Size(549, 44);
            label8.TabIndex = 49;
            label8.Text = "Calculations";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // comboBoxCameraSensorSize
            // 
            comboBoxCameraSensorSize.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxCameraSensorSize.FormattingEnabled = true;
            comboBoxCameraSensorSize.Items.AddRange(new object[] { "Full Frame (35mm)", "APC", "Micro Four Thirds", "Medium Format", "1-inch" });
            comboBoxCameraSensorSize.Location = new Point(1659, 916);
            comboBoxCameraSensorSize.Margin = new Padding(3, 4, 3, 4);
            comboBoxCameraSensorSize.Name = "comboBoxCameraSensorSize";
            comboBoxCameraSensorSize.Size = new Size(271, 33);
            comboBoxCameraSensorSize.TabIndex = 35;
            // 
            // label6
            // 
            label6.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label6.Location = new Point(1659, 883);
            label6.Name = "label6";
            label6.Size = new Size(271, 29);
            label6.TabIndex = 51;
            label6.Text = "Camera Sensor Size";
            // 
            // label9
            // 
            label9.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label9.Location = new Point(1659, 983);
            label9.Name = "label9";
            label9.Size = new Size(271, 29);
            label9.TabIndex = 52;
            label9.Text = "Len Focal Length (mm)";
            // 
            // textBoxLensFocalLength
            // 
            textBoxLensFocalLength.Location = new Point(1659, 1016);
            textBoxLensFocalLength.Margin = new Padding(3, 4, 3, 4);
            textBoxLensFocalLength.Name = "textBoxLensFocalLength";
            textBoxLensFocalLength.Size = new Size(271, 31);
            textBoxLensFocalLength.TabIndex = 36;
            textBoxLensFocalLength.Text = "105";
            textBoxLensFocalLength.Validating += textBoxLensFocalLength_Validating;
            // 
            // label10
            // 
            label10.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label10.Location = new Point(1659, 1083);
            label10.Name = "label10";
            label10.Size = new Size(271, 29);
            label10.TabIndex = 54;
            label10.Text = "Macro Tube Size (mm)";
            // 
            // textBoxMacroTubeSize
            // 
            textBoxMacroTubeSize.Location = new Point(1659, 1116);
            textBoxMacroTubeSize.Margin = new Padding(3, 4, 3, 4);
            textBoxMacroTubeSize.Name = "textBoxMacroTubeSize";
            textBoxMacroTubeSize.Size = new Size(271, 31);
            textBoxMacroTubeSize.TabIndex = 37;
            textBoxMacroTubeSize.Text = "0";
            textBoxMacroTubeSize.Validating += textBoxMacroTubeSize_Validating;
            // 
            // buttonCalculate
            // 
            buttonCalculate.Location = new Point(1934, 1212);
            buttonCalculate.Margin = new Padding(0);
            buttonCalculate.Name = "buttonCalculate";
            buttonCalculate.Size = new Size(278, 50);
            buttonCalculate.TabIndex = 41;
            buttonCalculate.Text = "Calculate";
            buttonCalculate.UseVisualStyleBackColor = true;
            buttonCalculate.Click += buttonCalculate_Click;
            // 
            // textBoxSubjectDepth
            // 
            textBoxSubjectDepth.Location = new Point(1937, 1016);
            textBoxSubjectDepth.Margin = new Padding(3, 4, 3, 4);
            textBoxSubjectDepth.Name = "textBoxSubjectDepth";
            textBoxSubjectDepth.Size = new Size(271, 31);
            textBoxSubjectDepth.TabIndex = 40;
            textBoxSubjectDepth.Text = "0";
            // 
            // label12
            // 
            label12.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label12.Location = new Point(1937, 983);
            label12.Name = "label12";
            label12.Size = new Size(271, 29);
            label12.TabIndex = 60;
            label12.Text = "Subject Depth (mm)";
            // 
            // label11
            // 
            label11.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label11.Location = new Point(1937, 883);
            label11.Name = "label11";
            label11.Size = new Size(271, 29);
            label11.TabIndex = 56;
            label11.Text = "Distance to Subject (mm)";
            // 
            // textBoxSubjectDistance
            // 
            textBoxSubjectDistance.Location = new Point(1937, 916);
            textBoxSubjectDistance.Margin = new Padding(3, 4, 3, 4);
            textBoxSubjectDistance.Name = "textBoxSubjectDistance";
            textBoxSubjectDistance.Size = new Size(271, 31);
            textBoxSubjectDistance.TabIndex = 39;
            textBoxSubjectDistance.Text = "0";
            // 
            // label13
            // 
            label13.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label13.Location = new Point(1659, 1183);
            label13.Name = "label13";
            label13.Size = new Size(271, 29);
            label13.TabIndex = 61;
            label13.Text = "Aperture";
            // 
            // textBoxDOFAperture
            // 
            textBoxDOFAperture.Location = new Point(1659, 1216);
            textBoxDOFAperture.Margin = new Padding(3, 4, 3, 4);
            textBoxDOFAperture.Name = "textBoxDOFAperture";
            textBoxDOFAperture.Size = new Size(271, 31);
            textBoxDOFAperture.TabIndex = 38;
            textBoxDOFAperture.Text = "11";
            textBoxDOFAperture.Validating += textBoxDOFAperture_Validating;
            // 
            // labelShotsRequired
            // 
            labelShotsRequired.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelShotsRequired.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            labelShotsRequired.Location = new Point(1937, 1112);
            labelShotsRequired.Name = "labelShotsRequired";
            labelShotsRequired.Size = new Size(272, 50);
            labelShotsRequired.TabIndex = 63;
            labelShotsRequired.Text = "0 Shots recomended";
            labelShotsRequired.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelStepSizeRequired
            // 
            labelStepSizeRequired.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelStepSizeRequired.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            labelStepSizeRequired.Location = new Point(1937, 1162);
            labelStepSizeRequired.Name = "labelStepSizeRequired";
            labelStepSizeRequired.Size = new Size(272, 50);
            labelStepSizeRequired.TabIndex = 64;
            labelStepSizeRequired.Text = "0mm per step";
            labelStepSizeRequired.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // checkBoxExposureDelay
            // 
            checkBoxExposureDelay.Anchor = AnchorStyles.Left;
            checkBoxExposureDelay.Enabled = false;
            checkBoxExposureDelay.Location = new Point(3, 972);
            checkBoxExposureDelay.Margin = new Padding(3, 4, 3, 4);
            checkBoxExposureDelay.Name = "checkBoxExposureDelay";
            checkBoxExposureDelay.Size = new Size(271, 30);
            checkBoxExposureDelay.TabIndex = 68;
            checkBoxExposureDelay.Text = "Exposure Delay";
            checkBoxExposureDelay.UseVisualStyleBackColor = true;
            // 
            // checkBoxEnableCopyright
            // 
            checkBoxEnableCopyright.Anchor = AnchorStyles.Left;
            checkBoxEnableCopyright.Enabled = false;
            checkBoxEnableCopyright.Location = new Point(3, 1022);
            checkBoxEnableCopyright.Margin = new Padding(3, 4, 3, 4);
            checkBoxEnableCopyright.Name = "checkBoxEnableCopyright";
            checkBoxEnableCopyright.Size = new Size(271, 30);
            checkBoxEnableCopyright.TabIndex = 14;
            checkBoxEnableCopyright.Text = "Enable Copyright";
            checkBoxEnableCopyright.UseVisualStyleBackColor = true;
            checkBoxEnableCopyright.CheckedChanged += checkBoxEnableCopyright_CheckedChanged;
            // 
            // labelArtistsName
            // 
            labelArtistsName.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelArtistsName.Enabled = false;
            labelArtistsName.Location = new Point(3, 1083);
            labelArtistsName.Name = "labelArtistsName";
            labelArtistsName.Size = new Size(271, 29);
            labelArtistsName.TabIndex = 0;
            labelArtistsName.Text = "Artists Name";
            // 
            // textBoxArtistsName
            // 
            textBoxArtistsName.Enabled = false;
            textBoxArtistsName.Location = new Point(3, 1116);
            textBoxArtistsName.Margin = new Padding(3, 4, 3, 4);
            textBoxArtistsName.MaxLength = 36;
            textBoxArtistsName.Name = "textBoxArtistsName";
            textBoxArtistsName.Size = new Size(271, 31);
            textBoxArtistsName.TabIndex = 15;
            textBoxArtistsName.Text = "Artists Name";
            textBoxArtistsName.WordWrap = false;
            textBoxArtistsName.TextChanged += textBoxArtistsName_TextChanged;
            // 
            // labelCopyright
            // 
            labelCopyright.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelCopyright.Enabled = false;
            labelCopyright.Location = new Point(3, 1183);
            labelCopyright.Name = "labelCopyright";
            labelCopyright.Size = new Size(271, 29);
            labelCopyright.TabIndex = 0;
            labelCopyright.Text = "Copyright";
            // 
            // textBoxCopyrightInfo
            // 
            textBoxCopyrightInfo.Enabled = false;
            textBoxCopyrightInfo.Location = new Point(3, 1216);
            textBoxCopyrightInfo.Margin = new Padding(3, 4, 3, 4);
            textBoxCopyrightInfo.MaxLength = 54;
            textBoxCopyrightInfo.Name = "textBoxCopyrightInfo";
            textBoxCopyrightInfo.Size = new Size(271, 31);
            textBoxCopyrightInfo.TabIndex = 16;
            textBoxCopyrightInfo.Text = "Copyright © Author 2023";
            textBoxCopyrightInfo.WordWrap = false;
            textBoxCopyrightInfo.TextChanged += textBoxCopyrightInfo_TextChanged;
            // 
            // checkBoxNoShooting
            // 
            checkBoxNoShooting.Anchor = AnchorStyles.Left;
            checkBoxNoShooting.Location = new Point(1659, 722);
            checkBoxNoShooting.Margin = new Padding(3, 4, 3, 4);
            checkBoxNoShooting.Name = "checkBoxNoShooting";
            checkBoxNoShooting.Size = new Size(271, 30);
            checkBoxNoShooting.TabIndex = 30;
            checkBoxNoShooting.Text = "Don't Shoot Camera";
            checkBoxNoShooting.UseVisualStyleBackColor = true;
            checkBoxNoShooting.CheckedChanged += checkBoxNoShooting_CheckedChanged;
            // 
            // labelRunStatus
            // 
            labelRunStatus.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            labelRunStatus.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            labelRunStatus.Location = new Point(1659, 672);
            labelRunStatus.Name = "labelRunStatus";
            labelRunStatus.Size = new Size(272, 29);
            labelRunStatus.TabIndex = 69;
            labelRunStatus.Text = "Shoot Status None";
            labelRunStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // buttonSetZero
            // 
            buttonSetZero.Enabled = false;
            buttonSetZero.Location = new Point(1934, 312);
            buttonSetZero.Margin = new Padding(0);
            buttonSetZero.Name = "buttonSetZero";
            buttonSetZero.Size = new Size(278, 50);
            buttonSetZero.TabIndex = 23;
            buttonSetZero.Text = "Halt && Set Start Position";
            buttonSetZero.UseVisualStyleBackColor = true;
            buttonSetZero.Click += buttonSetStart_Click;
            // 
            // labelCurrentPositionValue
            // 
            labelCurrentPositionValue.Anchor = AnchorStyles.Left;
            labelCurrentPositionValue.Enabled = false;
            labelCurrentPositionValue.Location = new Point(1937, 272);
            labelCurrentPositionValue.Name = "labelCurrentPositionValue";
            labelCurrentPositionValue.Size = new Size(271, 29);
            labelCurrentPositionValue.TabIndex = 31;
            labelCurrentPositionValue.Text = "0";
            // 
            // labelCurrentPosition
            // 
            labelCurrentPosition.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelCurrentPosition.Location = new Point(1937, 233);
            labelCurrentPosition.Name = "labelCurrentPosition";
            labelCurrentPosition.Size = new Size(271, 29);
            labelCurrentPosition.TabIndex = 34;
            labelCurrentPosition.Text = "Current Position";
            // 
            // labelTICStatus
            // 
            labelTICStatus.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            labelTICStatus.AutoSize = true;
            labelTICStatus.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point);
            labelTICStatus.Location = new Point(1937, 177);
            labelTICStatus.Name = "labelTICStatus";
            labelTICStatus.Size = new Size(272, 20);
            labelTICStatus.TabIndex = 70;
            labelTICStatus.Text = "Disconnected";
            labelTICStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // checkBoxManualShooting
            // 
            checkBoxManualShooting.Anchor = AnchorStyles.Left;
            checkBoxManualShooting.AutoSize = true;
            checkBoxManualShooting.Location = new Point(1659, 772);
            checkBoxManualShooting.Margin = new Padding(3, 4, 3, 4);
            checkBoxManualShooting.Name = "checkBoxManualShooting";
            checkBoxManualShooting.Size = new Size(220, 29);
            checkBoxManualShooting.TabIndex = 71;
            checkBoxManualShooting.Text = "Manual Camera Trigger";
            checkBoxManualShooting.UseVisualStyleBackColor = true;
            checkBoxManualShooting.CheckedChanged += checkBoxManualShooting_CheckedChanged;
            // 
            // statusStrip1
            // 
            statusStrip1.AutoSize = false;
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Location = new Point(0, 851);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1297, 40);
            statusStrip1.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(3, 346);
            label3.Name = "label3";
            label3.Size = new Size(71, 20);
            label3.TabIndex = 0;
            label3.Text = "Aperture";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Enabled = false;
            label5.Location = new Point(3, 454);
            label5.Name = "label5";
            label5.Size = new Size(125, 20);
            label5.TabIndex = 0;
            label5.Text = "Flash Sync Time";
            label5.Visible = false;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Enabled = false;
            label7.Location = new Point(3, 592);
            label7.Name = "label7";
            label7.Size = new Size(100, 20);
            label7.TabIndex = 0;
            label7.Text = "Artists Name";
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem1, toolsToolStripMenuItem1, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(7, 2, 0, 2);
            menuStrip1.Size = new Size(2212, 33);
            menuStrip1.TabIndex = 4;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem1
            // 
            fileToolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, toolStripSeparator3, saveToolStripMenuItem, saveAsToolStripMenuItem, toolStripSeparator4, exitApplicationToolStripMenuItem1 });
            fileToolStripMenuItem1.Name = "fileToolStripMenuItem1";
            fileToolStripMenuItem1.Size = new Size(54, 29);
            fileToolStripMenuItem1.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+O";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new Size(301, 34);
            openToolStripMenuItem.Text = "&Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(298, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+S";
            saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveToolStripMenuItem.Size = new Size(301, 34);
            saveToolStripMenuItem.Text = "&Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+S";
            saveAsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            saveAsToolStripMenuItem.Size = new Size(301, 34);
            saveAsToolStripMenuItem.Text = "Save &As";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(298, 6);
            // 
            // exitApplicationToolStripMenuItem1
            // 
            exitApplicationToolStripMenuItem1.Name = "exitApplicationToolStripMenuItem1";
            exitApplicationToolStripMenuItem1.ShortcutKeyDisplayString = "Ctrl+Q";
            exitApplicationToolStripMenuItem1.ShortcutKeys = Keys.Control | Keys.Q;
            exitApplicationToolStripMenuItem1.Size = new Size(301, 34);
            exitApplicationToolStripMenuItem1.Text = "E&xit Application";
            exitApplicationToolStripMenuItem1.Click += exitApplicationToolStripMenuItem1_Click;
            // 
            // toolsToolStripMenuItem1
            // 
            toolsToolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { settingsToolStripMenuItem });
            toolsToolStripMenuItem1.Name = "toolsToolStripMenuItem1";
            toolsToolStripMenuItem1.Size = new Size(69, 29);
            toolsToolStripMenuItem1.Text = "&Tools";
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+E";
            settingsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.E;
            settingsToolStripMenuItem.Size = new Size(238, 34);
            settingsToolStripMenuItem.Text = "S&ettings";
            settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(65, 29);
            helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(164, 34);
            aboutToolStripMenuItem.Text = "&About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(54, 30);
            fileToolStripMenuItem.Text = "&File";
            // 
            // saveSettingsToolStripMenuItem
            // 
            saveSettingsToolStripMenuItem.Name = "saveSettingsToolStripMenuItem";
            saveSettingsToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+S";
            saveSettingsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveSettingsToolStripMenuItem.Size = new Size(350, 34);
            saveSettingsToolStripMenuItem.Text = "&Save Settings";
            // 
            // saveSettingsAsToolStripMenuItem
            // 
            saveSettingsAsToolStripMenuItem.Name = "saveSettingsAsToolStripMenuItem";
            saveSettingsAsToolStripMenuItem.ShortcutKeyDisplayString = "Shft+Ctrl+S";
            saveSettingsAsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            saveSettingsAsToolStripMenuItem.Size = new Size(350, 34);
            saveSettingsAsToolStripMenuItem.Text = "Save Settings &As";
            // 
            // openSettingsToolStripMenuItem
            // 
            openSettingsToolStripMenuItem.Name = "openSettingsToolStripMenuItem";
            openSettingsToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+O";
            openSettingsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openSettingsToolStripMenuItem.Size = new Size(350, 34);
            openSettingsToolStripMenuItem.Text = "&Open Settings";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(347, 6);
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(347, 6);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(347, 6);
            // 
            // exitApplicationToolStripMenuItem
            // 
            exitApplicationToolStripMenuItem.Name = "exitApplicationToolStripMenuItem";
            exitApplicationToolStripMenuItem.ShortcutKeyDisplayString = "Cttrl+Q";
            exitApplicationToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Q;
            exitApplicationToolStripMenuItem.Size = new Size(350, 34);
            exitApplicationToolStripMenuItem.Text = "E&xit Application";
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(69, 30);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // timerTIC
            // 
            timerTIC.Enabled = true;
            timerTIC.Tick += timerTIC_Tick;
            // 
            // statusStrip2
            // 
            statusStrip2.ImageScalingSize = new Size(24, 24);
            statusStrip2.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip2.Location = new Point(0, 1314);
            statusStrip2.Name = "statusStrip2";
            statusStrip2.Padding = new Padding(1, 0, 16, 0);
            statusStrip2.Size = new Size(2212, 32);
            statusStrip2.TabIndex = 5;
            statusStrip2.Text = "statusStrip2";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(179, 25);
            toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // timerProject
            // 
            timerProject.Tick += timerProject_Tick;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(2212, 1346);
            Controls.Add(statusStrip2);
            Controls.Add(menuStrip1);
            Controls.Add(tableLayoutPanel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new Padding(3, 4, 3, 4);
            Name = "Main";
            Text = "MacroRail";
            FormClosing += Main_FormClosing;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            statusStrip2.ResumeLayout(false);
            statusStrip2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private PictureBox pictureBox;
        private Label labelCameraName;
        private Button buttonCapture;
        private Label labelBattery;
        private Button buttonAutoFocus;
        private CheckBox checkBoxEnableCopyright;
        private TextBox textBoxArtistsName;
        private TextBox textBoxCopyrightInfo;
        private Label labelCompression;
        private Label labelShutterSpeed;
        private Label labelApeture;
        private Label labelSensitivity;
        private Label labelFlashSyncTime;
        private Label labelFlashSlowLimit;
        private Label labelArtistsName;
        private Label labelCopyright;
        private Button buttonToggleliveview;
        private StatusStrip statusStrip1;
        private ComboBox comboBoxCompression;
        private ComboBox comboBoxShutterSpeed;
        private ComboBox comboBoxApeture;
        private Label label3;
        private ComboBox comboBoxSensitivity;
        private ComboBox comboBoxFlashSyncTime;
        private ComboBox comboBoxFlashSlowLimit;
        private Label label5;
        private Label label7;
        private CheckBox checkBoxAutoISO;
        private Label label1;
        private TextBox textBoxDelayBeforeShooting;
        private Label labelDelayBeforeShooting;
        private Button buttonJogBackward;
        private Button buttonJogForward;
        private Label labelCurrentPosition;
        private Button buttonGoStart;
        private Label labelCurrentPositionValue;
        private Button buttonSetZero;
        private TextBox textBoxStepCount;
        private Label labelStepCount;
        private Label labelTICConnection;
        private Label label2;
        private TextBox textBoxStepSize;
        private Button buttonStart;
        private Button buttonPause;
        private Button buttonStop;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem openSettingsToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem saveSettingsToolStripMenuItem;
        private ToolStripMenuItem saveSettingsAsToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem exitApplicationToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem fileToolStripMenuItem1;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem exitApplicationToolStripMenuItem1;
        private ToolStripMenuItem toolsToolStripMenuItem1;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private Button buttonTICDeEnergize;
        private Button buttonTICResume;
        private Button buttonTICConnect;
        private System.Windows.Forms.Timer timerTIC;
        private Label label4;
        private Label labelJogSpeed;
        private TextBox textBoxJogSpeed;
        private Label label8;
        private ComboBox comboBoxCameraSensorSize;
        private Label label6;
        private Label label9;
        private TextBox textBoxLensFocalLength;
        private Label label10;
        private TextBox textBoxMacroTubeSize;
        private Button buttonCalculate;
        private TextBox textBoxSubjectDepth;
        private Label label12;
        private Label label11;
        private TextBox textBoxSubjectDistance;
        private Label label13;
        private TextBox textBoxDOFAperture;
        private Label labelShotsRequired;
        private Label labelStepSizeRequired;
        private Label labelDOF;
        private StatusStrip statusStrip2;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Timer timerProject;
        private CheckBox checkBoxExposureDelay;
        private CheckBox checkBoxNoShooting;
        private Label labelRunStatus;
        private Label labelTICStatus;
        private CheckBox checkBoxManualShooting;
    }
}

