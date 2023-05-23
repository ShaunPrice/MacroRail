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
            saveFileDialog.ShowDialog();

            m_filename = saveFileDialog.FileName;
            m_project = textBoxProjectName.Text;
            m_description = textBoxDescription.Text;
            m_version = (string)textBoxVersion.Text;
        }

        public string ProjectName
        {
            get { return m_project; }
            set { m_project = value; }
        }

        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

        public string Version
        {
            get { return m_version; }
            set { m_version = value; }
        }

        public string Filename
        {
            get { return m_filename; }
            set { m_filename = value; }
        }
    }
}
