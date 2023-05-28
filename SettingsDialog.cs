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
    public partial class SettingsDialog : Form
    {
        uint m_pitch = 2;
        uint m_thread_starts = 4;
        uint m_steps_rev = 200;
        uint m_microsteps = 32;
        uint m_gear_ratio = 1;
        uint m_mm_rev = 0;
        uint m_total_steps_rev = 0;
        uint m_steps_mm = 0;
        uint m_max_speed = 100000000;
        uint m_jog_speed = 20;

        public SettingsDialog()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.FixedDialog;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBoxMaxSpeed.Text, out int _))
            {
                textBoxMaxSpeed.Text = "0";
            }

            m_max_speed = uint.Parse(textBoxMaxSpeed.Text);

            if (m_max_speed < 0)
            {
                textBoxMaxSpeed.Text = "0";
                m_max_speed = 0;
            }

            if (!uint.TryParse(textBoxStepsMM.Text, out uint _))
            {
                textBoxStepsMM.Text = "0";
            }

            m_steps_mm = uint.Parse(textBoxStepsMM.Text);
        }

        private void buttonCalculate_Click(object sender, EventArgs e)
        {
            m_pitch = uint.Parse(textBoxThreadPitch.Text);
            m_thread_starts = uint.Parse(comboBoxThreadStarts.SelectedItem.ToString());
            m_steps_rev = uint.Parse(comboBoxStepsPerRevolution.SelectedItem.ToString());
            m_microsteps = uint.Parse(comboBoxMicrosteps.SelectedItem.ToString());
            m_gear_ratio = uint.Parse(textBoxGearRatio.Text);
            m_mm_rev = m_pitch * m_thread_starts;
            m_total_steps_rev = m_steps_rev * m_microsteps * m_gear_ratio;
            m_steps_mm = (uint)(m_total_steps_rev / m_mm_rev);

            textBoxStepsMM.Text = m_steps_mm.ToString("###0");

            try
            {
                uint l_step_mm = uint.Parse(textBoxStepsMM.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Calulation Error while converting Steps in mm to an Integrer: " + ex.Message);
            }
        }

        public uint StepsMM
        {
            get { return m_steps_mm; }
            set { m_steps_mm = value; }
        }

        public uint MaxSpeed
        {
            get { return m_max_speed; }
            set { m_max_speed = value; }
        }

        public uint Pitch
        {
            get { return m_pitch; }
            set { m_pitch = value; }
        }

        public uint ThreadStarts
        {
            get { return m_thread_starts; }
            set { m_thread_starts = value; }
        }
        public uint StepsPerRevolution
        {
            get { return m_steps_rev; }
            set { m_steps_rev = value; }
        }

        public uint Microsteps
        {
            get { return m_microsteps; }
            set { m_microsteps = value; }
        }

        public uint GearRatio
        {
            get { return m_gear_ratio; }
            set { m_gear_ratio = value; }
        }

        private void SettingsDialog_Load(object sender, EventArgs e)
        {
            textBoxThreadPitch.Text = m_pitch.ToString();
            comboBoxThreadStarts.Text = m_thread_starts.ToString();
            comboBoxStepsPerRevolution.Text = m_steps_rev.ToString();
            comboBoxMicrosteps.Text = m_microsteps.ToString();
            textBoxGearRatio.Text = m_gear_ratio.ToString();
            textBoxMaxSpeed.Text = m_max_speed.ToString();
            textBoxStepsMM.Text = m_steps_mm.ToString();
        }

        private void textBoxJogSpeed_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                m_jog_speed = uint.Parse(textBoxJogSpeed.Text);
            }
            catch
            {
                e.Cancel = true;
            }
        }

        private void textBoxThreadPitch_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                m_pitch = uint.Parse(textBoxThreadPitch.Text);
            }
            catch
            {
                e.Cancel = true;
            }
        }

        private void textBoxGearRatio_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                m_gear_ratio = uint.Parse(textBoxGearRatio.Text);
            }
            catch
            {
                e.Cancel = true;
            }
        }

        private void textBoxStepsMM_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                m_steps_mm = uint.Parse(textBoxStepsMM.Text);
            }
            catch
            {
                e.Cancel = true;
            }
        }

        private void textBoxMaxSpeed_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                m_max_speed = uint.Parse(textBoxMaxSpeed.Text);
            }
            catch
            {
                e.Cancel = true;
            }
        }
    }
}
