namespace MacroRail
{
    partial class AboutBox1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            tableLayoutPanel = new TableLayoutPanel();
            logoPictureBox = new PictureBox();
            labelProductName = new Label();
            labelVersion = new Label();
            labelCopyright = new Label();
            textBoxDescription = new TextBox();
            okButton = new Button();
            linkLabelGitRepo = new LinkLabel();
            tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel.Controls.Add(logoPictureBox, 0, 0);
            tableLayoutPanel.Controls.Add(labelProductName, 1, 0);
            tableLayoutPanel.Controls.Add(labelVersion, 1, 1);
            tableLayoutPanel.Controls.Add(labelCopyright, 1, 2);
            tableLayoutPanel.Controls.Add(textBoxDescription, 1, 4);
            tableLayoutPanel.Controls.Add(okButton, 1, 5);
            tableLayoutPanel.Controls.Add(linkLabelGitRepo, 1, 3);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new Point(16, 18);
            tableLayoutPanel.Margin = new Padding(4, 6, 4, 6);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 6;
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.98792F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.98792F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.98792F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 6.141938F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 61.90638F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 7.98792F));
            tableLayoutPanel.Size = new Size(1067, 790);
            tableLayoutPanel.TabIndex = 0;
            // 
            // logoPictureBox
            // 
            logoPictureBox.Dock = DockStyle.Fill;
            logoPictureBox.Image = Properties.Resources.About;
            logoPictureBox.Location = new Point(4, 6);
            logoPictureBox.Margin = new Padding(4, 6, 4, 6);
            logoPictureBox.Name = "logoPictureBox";
            tableLayoutPanel.SetRowSpan(logoPictureBox, 6);
            logoPictureBox.Size = new Size(525, 778);
            logoPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            logoPictureBox.TabIndex = 12;
            logoPictureBox.TabStop = false;
            // 
            // labelProductName
            // 
            labelProductName.Dock = DockStyle.Fill;
            labelProductName.Location = new Point(543, 0);
            labelProductName.Margin = new Padding(10, 0, 4, 0);
            labelProductName.MaximumSize = new Size(0, 32);
            labelProductName.Name = "labelProductName";
            labelProductName.Size = new Size(520, 32);
            labelProductName.TabIndex = 19;
            labelProductName.Text = "Product Name";
            labelProductName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelVersion
            // 
            labelVersion.Dock = DockStyle.Fill;
            labelVersion.Location = new Point(543, 63);
            labelVersion.Margin = new Padding(10, 0, 4, 0);
            labelVersion.MaximumSize = new Size(0, 32);
            labelVersion.Name = "labelVersion";
            labelVersion.Size = new Size(520, 32);
            labelVersion.TabIndex = 0;
            labelVersion.Text = "Version";
            labelVersion.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelCopyright
            // 
            labelCopyright.Dock = DockStyle.Fill;
            labelCopyright.Location = new Point(543, 126);
            labelCopyright.Margin = new Padding(10, 0, 4, 0);
            labelCopyright.MaximumSize = new Size(0, 32);
            labelCopyright.Name = "labelCopyright";
            labelCopyright.Size = new Size(520, 32);
            labelCopyright.TabIndex = 21;
            labelCopyright.Text = "Copyright";
            labelCopyright.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxDescription
            // 
            textBoxDescription.Dock = DockStyle.Fill;
            textBoxDescription.Location = new Point(543, 243);
            textBoxDescription.Margin = new Padding(10, 6, 4, 6);
            textBoxDescription.Multiline = true;
            textBoxDescription.Name = "textBoxDescription";
            textBoxDescription.ReadOnly = true;
            textBoxDescription.ScrollBars = ScrollBars.Both;
            textBoxDescription.Size = new Size(520, 477);
            textBoxDescription.TabIndex = 23;
            textBoxDescription.TabStop = false;
            textBoxDescription.Text = "Description";
            // 
            // okButton
            // 
            okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            okButton.DialogResult = DialogResult.Cancel;
            okButton.Location = new Point(939, 740);
            okButton.Margin = new Padding(4, 6, 4, 6);
            okButton.Name = "okButton";
            okButton.Size = new Size(124, 44);
            okButton.TabIndex = 24;
            okButton.Text = "&OK";
            // 
            // linkLabelGitRepo
            // 
            linkLabelGitRepo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            linkLabelGitRepo.Location = new Point(536, 189);
            linkLabelGitRepo.Name = "linkLabelGitRepo";
            linkLabelGitRepo.Size = new Size(528, 48);
            linkLabelGitRepo.TabIndex = 25;
            linkLabelGitRepo.TabStop = true;
            linkLabelGitRepo.Text = "https://github.com/ShaunPrice/MacroRail";
            linkLabelGitRepo.LinkClicked += linkLabelGitRepo_LinkClicked;
            // 
            // AboutBox1
            // 
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1099, 826);
            Controls.Add(tableLayoutPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 6, 4, 6);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutBox1";
            Padding = new Padding(16, 18, 16, 18);
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "AboutBox1";
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel;
        private PictureBox logoPictureBox;
        private Label labelProductName;
        private Label labelVersion;
        private Label labelCopyright;
        private TextBox textBoxDescription;
        private Button okButton;
        private LinkLabel linkLabelGitRepo;
    }
}
