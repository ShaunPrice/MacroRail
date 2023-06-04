/*
 * Copyright (c) 2023 Shaun Price
This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail).
MacroRail is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published
by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
MacroRail is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of 
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacroRail
{
    public partial class StartShooting : Form
    {
        string m_shoot_name = "shoot1";
        string m_shoot_version = "1.0";
        string m_shoot_directory = ".";

        string m_step_count = "";
        string m_step_size = "";
        string m_camera = "";
        string m_shutter_speed = "";
        string m_apeture = "";
        string m_sensitivity = "";

        public StartShooting()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            textBoxShootName.Text = m_shoot_name;
            textBoxShootVersion.Text = m_shoot_version;
            textBoxShootDirectory.Text = m_shoot_directory;
            labelStepCount.Text = m_step_count;
            labelStepSize.Text = m_step_size;
            labelCamera.Text = m_camera;
            labelShutterSpeed.Text = m_shutter_speed;
            labelApeture.Text = m_apeture;
            labelSensitivity.Text = m_sensitivity;
        }

        private void buttonSetProjectDirectory_Click(object sender, EventArgs e)
        {
            folderBrowserDialogProject.RootFolder = Environment.SpecialFolder.MyPictures;
            if (folderBrowserDialogProject.ShowDialog() == DialogResult.OK)
            {
                textBoxShootDirectory.Text = folderBrowserDialogProject.SelectedPath;
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            m_shoot_name = textBoxShootName.Text.Trim();
            m_shoot_version = textBoxShootVersion.Text.Trim();
            m_shoot_directory = textBoxShootDirectory.Text.Trim();

            if (!(m_shoot_name.Trim() == "" || m_shoot_directory.Trim() == "" || m_shoot_version.Trim() == ""))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        public string ShootName
        {
            get { return m_shoot_name; }
            set { m_shoot_name = value; }
        }

        public string ShootVersion
        {
            get { return m_shoot_version; }
            set { m_shoot_version = value; }
        }

        public string ShootDirectory
        {
            get { return m_shoot_directory; }
            set { m_shoot_directory = value; }
        }

        public string ShootStepCount
        {
            set { m_step_count = value; }
        }

        public string ShootStepSize
        {
            set { m_step_size = value; }
        }

        public string Camera
        {
            set { m_camera = value; }
        }

        public string CameraShutterSpeed
        {
            set { m_shutter_speed = value; }
        }

        public string CameraApeture
        {
            set { m_apeture = value; }
        }

        public string CameraSensitivity
        {
            set { m_sensitivity = value; }
        }

        private void textBoxShootDirectory_TextChanged(object sender, EventArgs e)
        {
            m_shoot_directory = (string)textBoxShootDirectory.Text;
        }

        private void textBoxShootVersion_TextChanged(object sender, EventArgs e)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (invalidFileNameChars.Any(textBoxShootName.Text.Contains))
            {
                textBoxShootName.ForeColor = Color.Red;
            }
            else
            {
                textBoxShootName.ForeColor = SystemColors.WindowText;
            }

            m_shoot_version = (string)textBoxShootVersion.Text;
        }

        private void textBoxShootName_TextChanged(object sender, EventArgs e)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (invalidFileNameChars.Any(((TextBox)sender).Text.Contains))
            {
                ((TextBox)sender).ForeColor = Color.Red;
            }
            else
            {
                ((TextBox)sender).ForeColor = SystemColors.WindowText;
            }

            m_shoot_name = (string)textBoxShootName.Text;
        }

        private void textBoxShootName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (invalidFileNameChars.Any(textBoxShootName.Text.Contains))
            {
                e.Cancel = true;
            }
        }

        private void textBoxShootVersion_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (invalidFileNameChars.Any(textBoxShootVersion.Text.Contains))
            {
                e.Cancel = true;
            }
        }
    }
}
