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
using System.Linq;
using System.Windows.Forms;

namespace MacroRail
{
    public partial class SaveAs : Form
    {
        private string m_project = "";
        private string m_description = "";
        private string m_version = "";
        private string m_filename = "";

        public SaveAs()
        {
            InitializeComponent();
        }

        public SaveAs(string project, string description, string version, string filename) : this()
        {
            m_project = project;
            m_description = description;
            m_version = version;
            m_filename = filename;

            textBoxProjectName.Text = m_project;
            textBoxDescription.Text = m_description;
            textBoxVersion.Text = m_version;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Macro Slider Project (*.proj)|*.proj|Any|*.*";
            saveFileDialog.FileName = m_project + "_" + m_version + ".proj";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                m_filename = saveFileDialog.FileName;
                m_project = m_project;
                m_description = m_description;
                m_version = m_version;

                DialogResult = DialogResult.OK;
            }
            else
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        private void textBoxProjectName_TextChanged(object sender, EventArgs e)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (invalidFileNameChars.Any(((TextBox)sender).Text.Contains))
            {
                ((TextBox)sender).ForeColor = Color.Red;
            }
            else
            {
                ((TextBox)sender).ForeColor = SystemColors.WindowText;
                m_project = (string)textBoxProjectName.Text;
            }
        }

        private void textBoxDescription_TextChanged(object sender, EventArgs e)
        {
            m_description += textBoxDescription.Text;
        }

        private void textBoxVersion_TextChanged(object sender, EventArgs e)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (invalidFileNameChars.Any(((TextBox)sender).Text.Contains))
            {
                ((TextBox)sender).ForeColor = Color.Red;
            }
            else
            {
                ((TextBox)sender).ForeColor = SystemColors.WindowText;
                m_version += textBoxVersion.Text;
            }
        }

        private void textBoxProjectName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (invalidFileNameChars.Any(textBoxProjectName.Text.Contains))
            {
                e.Cancel = true;
            }
        }

        private void textBoxVersion_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (invalidFileNameChars.Any(textBoxVersion.Text.Contains))
            {
                e.Cancel = true;
            }
        }

        public string ProjectName
        {
            get { return m_project; }
            set { m_project = value; }
        }

        public string ProjectDescription
        {
            get { return m_description; }
            set { m_description = value; }
        }

        public string ProjectVersion
        {
            get { return m_version; }
            set { m_version = value; }
        }

        public string ProjectFilename
        {
            get { return m_filename; }
            set { m_filename = value; }
        }
    }
}
