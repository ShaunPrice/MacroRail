/*
 * Copyright (c) 2023 Shaun Price
This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail).
MacroRail is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published
by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
MacroRail is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of 
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
*/
using Newtonsoft.Json;
using Nikon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace MacroRail
{
    public partial class Main : Form
    {
        private Timer liveViewTimer;
        private int m_status_timer = 0;

        List<NikonManager> _managers;
        List<NikonDevice> _devices;
      
        bool m_camera_available = false;
        string m_camera = "";
        string m_compression = "";
        string m_apeture = "";
        string m_shutter_speed = "";
        string m_sensitivity = "";

        tic m_tic;
        string m_tic_name = "";
        bool m_tic_connected = false;
        bool m_moving = false;
        bool m_homing = false;
        bool m_decelerating;
        int m_current_position = 0;

        double m_pitch = 2;
        double m_thread_starts = 4;
        double m_steps_rev = 200;
        double m_microsteps = 32;
        double m_gear_ratio = 1;
        uint m_steps_mm = 800; // Set-up in tools settings
        int m_start = 0;
        int m_max_speed = 100000000;
        int m_jog_speed = 20000000;

        double m_dof = 0;

        string m_project_name = "project1";
        string m_project_description = "";
        string m_project_version = "1.0.0";
        string m_project_filename = "";

        // If the project is opened from a file or saved as was executed we can use Save
        bool m_project_saved = false;

        // The project variables
        string m_project_directory = "";

        // The shoot variables
        string m_shoot_name = "shoot1";
        string m_shoot_version = "1.0.0";
        int m_shoot_shots_required = 1;
        double m_shoot_steps_distance = 1;
        uint m_shoot_delay_before = 0;

        int m_shoot_current_shot = 0;
        string m_shoot_directory = "";
        int m_shoot_delay_counter = 0;

        bool m_no_shooting = false;

        bool m_downloaded_nef = false;
        bool m_downloaded_jpeg = false;

        private enum shoot_status : int
        {
            Idle = 0,       // The project is not running
            Start = 1,      // The project sequence has started
            Move = 2,       // Move to the shot position
            Moving = 3,     // Wait until camera moved
            Delay = 4,      // Make sure the flashes have recharged & the platform has stopped shaking from the move
            Delaying = 5,   // Wait until delay completed
            Shoot = 6,      // Take the shot
            Shooting = 7,   // Wait until shoot completed
            Downloaded = 8,   // Download the images
            Paused = 9,    // Paused by the user
            End = 10        // The project sequence has ended
        }

        shoot_status m_shoot_sequence_status = shoot_status.Idle;
        shoot_status m_shoot_pause_previous_state;

        public Main()
        {
            InitializeComponent();

            Status("Loading");

            // Load the application settings
            m_pitch = Properties.Settings.Default.thread_pitch;
            m_thread_starts = Properties.Settings.Default.thread_starts;
            m_steps_mm = Properties.Settings.Default.steps_mm;
            m_microsteps = Properties.Settings.Default.microsteps;
            m_gear_ratio = Properties.Settings.Default.gear_ratio;
            m_steps_mm = Properties.Settings.Default.steps_mm;
            m_max_speed = Properties.Settings.Default.max_speed;

            // Set up defaults
            comboBoxCameraSensorSize.SelectedIndex = 0;

            // Disable buttons
            ToggleButtons(false);

            // Set Start, Stop, Pause buttons to grayed out
            buttonStart.BackColor = System.Drawing.Color.LightGray;
            buttonStop.BackColor = System.Drawing.Color.LightGray;
            buttonPause.BackColor = System.Drawing.Color.LightGray;

            // Initialize live view timer
            liveViewTimer = new Timer();
            liveViewTimer.Tick += new EventHandler(liveViewTimer_Tick);
            liveViewTimer.Interval = 1000 / 30;

            _managers = new List<NikonManager>();
            _devices = new List<NikonDevice>();

            string[] md3s = Directory.GetFiles(Directory.GetCurrentDirectory(), @"MD3\*.md3", SearchOption.AllDirectories);

            if (md3s.Length == 0)
            {
                Status("Couldn't find any MD3 files in " + Directory.GetCurrentDirectory() + @"\MD3\. Download MD3 files from Nikons SDK website: https://sdk.nikonimaging.com/apply/");
            }
            else
            {
                Status("Loading Nikon MD3 files");

                foreach (string md3 in md3s)
                {
                    const string requiredDllFile = "NkdPTP.dll";

                    string requiredDllPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(md3), requiredDllFile);

                    if (!File.Exists(requiredDllPath))
                    {
                        Status("Warning: Couldn't find " + requiredDllFile + " in " + System.IO.Path.GetDirectoryName(md3) + ". The library will not work properly without it!");
                    }

                    NikonManager manager = new NikonManager(md3);
                    manager.DeviceAdded += new DeviceAddedDelegate(manager_DeviceAdded);
                    manager.DeviceRemoved += new DeviceRemovedDelegate(manager_DeviceRemoved);

                    _managers.Add(manager);
                }
            }

            ////////////////////////////////////////////////////
            ///// TIC Stepper Driver
            ////////////////////////////////////////////////////
            ///
            Status("Nikon Initialise Complete");
            m_tic = new tic();
        }

        private void Status(string message)
        {
            toolStripStatusLabel1.Text = message;
        }

        private void StatusTIC(string message)
        {
            labelTICStatus.Text = message;
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            // Shut down the Nikon manager
            foreach (NikonManager manager in _managers)
            {
                // Disable live view (in case it's enabled)
                for (uint index = 0; index < manager.DeviceCount; index++)
                {
                    manager.GetDeviceByIndex(index).LiveViewEnabled = false;
                }
                manager.Shutdown();
            }
            base.OnClosing(e);
        }

        void manager_DeviceAdded(NikonManager sender, NikonDevice device)
        {
            this._devices.Add(device);

            // Set the device name
            m_camera = device.Name;
            labelCameraName.Text = device.Name;

            Status("Connecting to Nikon camera " + device.Name);

            // Enable buttons
            ToggleButtons(true);

            // Enable supported cotrols
            // ========================

            // Battery Level
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_BatteryLevel))
                {
                    labelBattery.Enabled = true;

                    int battery = device.GetInteger(eNkMAIDCapability.kNkMAIDCapability_BatteryLevel);
                    NkMAIDCapInfo cap = device.GetCapabilityInfo(eNkMAIDCapability.kNkMAIDCapability_BatteryLevel);
                    SetBatteryText(battery);
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving battery capability & setting: " + ex.Message);
            }

            // Auto Focus
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_AutoFocus))
                {
                    buttonAutoFocus.Enabled = true;
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving auto focus capability: " + ex.Message);
            }

            // Compression Control
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel))
                {
                    comboBoxCompression.Enabled = true;
                    labelCompression.Enabled = true;

                    comboBoxCompression.Items.Clear();

                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel);
                    for (int x = 0; x < en.Length; x++)
                    {
                        comboBoxCompression.Items.Add(en.GetEnumValueByIndex(x));
                    }

                    comboBoxCompression.SelectedIndex = en.Index;
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving image compression capability & setting: " + ex.Message);
            }

            // Shutter Speed Control
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed))
                {
                    comboBoxShutterSpeed.Enabled = true;
                    labelShutterSpeed.Enabled = true;

                    comboBoxShutterSpeed.Items.Clear();

                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);
                    for (int x = 0; x < en.Length; x++)
                    {
                        comboBoxShutterSpeed.Items.Add(en.GetEnumValueByIndex(x));
                    }

                    comboBoxShutterSpeed.SelectedIndex = en.Index;
                    m_shutter_speed = en.Value.ToString();
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving shutter speed capability & setting: " + ex.Message);
            }

            // Apeture Control
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_Aperture))
                {
                    comboBoxApeture.Enabled = true;
                    labelApeture.Enabled = true;

                    comboBoxApeture.Items.Clear();

                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Aperture);
                    for (int x = 0; x < en.Length; x++)
                    {
                        comboBoxApeture.Items.Add(en.GetEnumValueByIndex(x));
                    }

                    comboBoxApeture.SelectedIndex = en.Index;

                    m_apeture = en.Value.ToString();
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving apeture capability & setting: " + ex.Message);
            }

            // Sensitivity Control
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_Sensitivity))
                {
                    comboBoxSensitivity.Enabled = true;
                    labelSensitivity.Enabled = true;

                    comboBoxSensitivity.Items.Clear();

                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                    for (int x = 0; x < en.Length; x++)
                    {
                        comboBoxSensitivity.Items.Add(en.GetEnumValueByIndex(x));
                    }

                    comboBoxSensitivity.SelectedIndex = en.Index;

                    m_sensitivity = en.Value.ToString();
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving sentitivity (ISO) capability & setting: " + ex.Message);
            }

            // Auto ISO Control
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_IsoControl))
                {
                    checkBoxAutoISO.Enabled = true;

                    bool autoISO = device.GetBoolean(eNkMAIDCapability.kNkMAIDCapability_IsoControl);
                    checkBoxAutoISO.Checked = autoISO;

                    if (autoISO)
                    {
                        // Grey out manual ISO
                        comboBoxSensitivity.Enabled = false;
                        labelSensitivity.Enabled = false;
                    }
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving auto ISO capability & setting: " + ex.Message);
            }

            // Flash Sync Time Control
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_FlashSyncTime))
                {
                    comboBoxFlashSyncTime.Enabled = true;
                    labelFlashSyncTime.Enabled = true;

                    comboBoxFlashSyncTime.Items.Clear();

                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_FlashSyncTime);
                    for (int x = 0; x < en.Length; x++)
                    {
                        comboBoxFlashSyncTime.Items.Add(en.GetEnumValueByIndex(x));
                    }

                    comboBoxFlashSyncTime.SelectedIndex = en.Index;
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving flash sync time capability & setting: " + ex.Message);
            }

            // Flash Slow Limit Control
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_FlashSlowLimit))
                {
                    comboBoxFlashSlowLimit.Enabled = true;
                    labelFlashSlowLimit.Enabled = true;

                    comboBoxFlashSlowLimit.Items.Clear();

                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_FlashSlowLimit);
                    for (int x = 0; x < en.Length; x++)
                    {
                        comboBoxFlashSlowLimit.Items.Add(en.GetEnumValueByIndex(x));
                    }

                    comboBoxFlashSlowLimit.SelectedIndex = en.Index;
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving flash slow limit capability & setting: " + ex.Message);
            }

            // Exposure Delay
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_ExposureDelay))
                {
                    checkBoxExposureDelay.Enabled = true;

                    bool exposureDelay = device.GetBoolean(eNkMAIDCapability.kNkMAIDCapability_ExposureDelay);
                    checkBoxExposureDelay.Checked = exposureDelay;
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving exposure delay capability & setting: " + ex.Message);
            }

            // Copyright Enable Control
            try
            {
                if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_CopyrightInfo))
                {
                    checkBoxEnableCopyright.Enabled = true;

                    // Load the author and copyright info
                    textBoxArtistsName.Text = device.GetString(eNkMAIDCapability.kNkMAIDCapability_ArtistName);
                    textBoxCopyrightInfo.Text = device.GetString(eNkMAIDCapability.kNkMAIDCapability_CopyrightInfo);

                    if (device.GetBoolean(eNkMAIDCapability.kNkMAIDCapability_EnableCopyright))
                    {
                        checkBoxEnableCopyright.Checked = true;

                        labelArtistsName.Enabled = true;
                        textBoxArtistsName.Enabled = true;
                        labelCopyright.Enabled = true;
                        textBoxCopyrightInfo.Enabled = true;
                    }
                    else
                    {
                        checkBoxEnableCopyright.Checked = false;

                        labelArtistsName.Enabled = false;
                        textBoxArtistsName.Enabled = false;
                        labelCopyright.Enabled = false;
                        textBoxCopyrightInfo.Enabled = false;
                    }
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while retrieving copyright capability & setting: " + ex.Message);
            }

            // Hook up device capture events
            device.CapabilityValueChanged += new CapabilityChangedDelegate(device_ValueChanged);
            device.ImageReady += new ImageReadyDelegate(device_ImageReady);
            device.CaptureComplete += new CaptureCompleteDelegate(device_CaptureComplete);

            Status(device.Name + " connected");
            m_camera_available = true;

            // Enable sequence shooting if TIC Stepper controller is connected
            if (m_tic_connected)
            {
                buttonStart.Enabled = true;
            }
        }

        void SetBatteryText(int level)
        {
            labelBattery.Text = "Battery " + level + "%";

            // Text colour based on battery condition
            if (level <= 20)
            {
                labelBattery.ForeColor = Color.Red;
            }
            else if (level <= 40)
            {
                labelBattery.ForeColor = Color.DarkOrange;
            }
            else
            {
                labelBattery.ForeColor = Color.Green;
            }
        }

        void manager_DeviceRemoved(NikonManager sender, NikonDevice device)
        {
            m_camera_available = false;

            // Disable sequence shooting if it's started
            if(m_shoot_sequence_status != shoot_status.Idle)
            {
                Status("Pausing shoot: Camera removed");
                Pause("Camera Removed");
            }

            this._devices.Remove(device);

            // Stop live view timer
            liveViewTimer.Stop();

            // Clear device name
            labelCameraName.Text = "No Camera";

            // Disable buttons
            ToggleButtons(false);

            // Hide Controls
            labelBattery.Enabled = false;
            comboBoxCompression.Enabled = false;
            labelCompression.Enabled = false;
            comboBoxShutterSpeed.Enabled = false;
            labelShutterSpeed.Enabled = false;
            comboBoxApeture.Enabled = false;
            labelApeture.Enabled = false;
            checkBoxAutoISO.Enabled = false;
            comboBoxSensitivity.Enabled = false;
            labelSensitivity.Enabled = false;
            comboBoxFlashSyncTime.Enabled = false;
            labelFlashSyncTime.Enabled = false;
            comboBoxFlashSlowLimit.Enabled = false;
            labelFlashSlowLimit.Enabled = false;
            checkBoxExposureDelay.Enabled = false;
            checkBoxEnableCopyright.Enabled = false;
            labelArtistsName.Enabled = false;
            textBoxArtistsName.Enabled = false;
            labelCopyright.Enabled = false;
            textBoxCopyrightInfo.Enabled = false;

            // Clear live view picture
            pictureBox.Image = null;

            Status("No camera connected");
        }

        void device_ValueChanged(NikonDevice sender, Nikon.eNkMAIDCapability capability)
        {
            NikonDevice device = (NikonDevice)sender;

            switch (capability)
            {
                case eNkMAIDCapability.kNkMAIDCapability_BatteryLevel:
                    int battery = device.GetInteger(eNkMAIDCapability.kNkMAIDCapability_BatteryLevel);
                    SetBatteryText(battery);
                    break;
                case eNkMAIDCapability.kNkMAIDCapability_ExposureDelay:
                    bool enableExposureDelay = device.GetBoolean(eNkMAIDCapability.kNkMAIDCapability_ExposureDelay);
                    checkBoxEnableCopyright.Checked = enableExposureDelay;
                    break;
                case eNkMAIDCapability.kNkMAIDCapability_EnableCopyright:
                    bool enableCopyright = device.GetBoolean(eNkMAIDCapability.kNkMAIDCapability_EnableCopyright);
                    checkBoxEnableCopyright.Checked = enableCopyright;
                    break;
                case eNkMAIDCapability.kNkMAIDCapability_ArtistName:
                    String artistName = device.GetString(eNkMAIDCapability.kNkMAIDCapability_ArtistName);
                    textBoxArtistsName.Text = artistName;
                    break;
                case eNkMAIDCapability.kNkMAIDCapability_CopyrightInfo:
                    String copyright = device.GetString(eNkMAIDCapability.kNkMAIDCapability_CopyrightInfo);
                    textBoxArtistsName.Text = copyright;
                    break;
                case eNkMAIDCapability.kNkMAIDCapability_IsoControl:
                    bool autoISO = device.GetBoolean(eNkMAIDCapability.kNkMAIDCapability_IsoControl);
                    checkBoxAutoISO.Checked = autoISO;

                    if (autoISO)
                    {
                        // Grey out manual ISO
                        comboBoxSensitivity.Enabled = false;
                        labelSensitivity.Enabled = false;
                    }
                    else
                    {
                        // Enable manual ISO
                        comboBoxSensitivity.Enabled = true;
                        labelSensitivity.Enabled = true;
                    }
                    break;
                default:
                    // All the remaining use the Enum type
                    NikonEnum en = device.GetEnum(capability);
                    switch (capability)
                    {
                        case eNkMAIDCapability.kNkMAIDCapability_CompressionLevel:
                            comboBoxCompression.SelectedIndex = en.Index;
                            m_compression = en.Value.ToString();
                            break;
                        case eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed:
                            comboBoxShutterSpeed.SelectedIndex = en.Index;
                            m_shutter_speed = en.Value.ToString();
                            break;
                        case eNkMAIDCapability.kNkMAIDCapability_Aperture:
                            comboBoxApeture.SelectedIndex = en.Index;
                            m_apeture = en.Value.ToString();
                            break;
                        case eNkMAIDCapability.kNkMAIDCapability_Sensitivity:
                            comboBoxSensitivity.SelectedIndex = en.Index;
                            m_sensitivity = en.Value.ToString();
                            break;
                        case eNkMAIDCapability.kNkMAIDCapability_FlashSyncTime:
                            comboBoxFlashSyncTime.SelectedIndex = en.Index;
                            break;
                        case eNkMAIDCapability.kNkMAIDCapability_FlashSlowLimit:
                            comboBoxFlashSlowLimit.SelectedIndex = en.Index;
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }

        void liveViewTimer_Tick(object sender, EventArgs e)
        {
            // Get live view image
            NikonLiveViewImage image = null;
            foreach (NikonDevice device in _devices)
            {
                if (device.LiveViewEnabled)
                {
                    try
                    {
                        image = device.GetLiveViewImage();
                    }
                    catch (NikonException)
                    {
                        liveViewTimer.Stop();
                    }

                    // Set captured image on picture box
                    if (image != null)
                    {
                        MemoryStream stream = new MemoryStream(image.JpegBuffer);
                        pictureBox.Image = Image.FromStream(stream);
                    }
                    break;
                }
            }
        }
        void device_ImageReady(NikonDevice sender, NikonImage image)
        {
            // If shooting a sequence we just finished the shot
            if (m_shoot_sequence_status == shoot_status.Shooting)
            {
                var directory = m_shoot_directory + @"\" + m_shoot_name + @"\" + m_shoot_version + @"\";

                // Create the shoot directory if it doesn't exist
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var filename = directory + m_shoot_current_shot.ToString();

                switch (image.Type)
                {
                    case NikonImageType.Jpeg:
                        SaveImageDirectory(image, filename + ".jpg");
                        m_downloaded_jpeg = true;
                        break;
                    case NikonImageType.Raw:
                        SaveImageDirectory(image, filename + ".NEF");
                        m_downloaded_nef = true;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                SaveImageDialog(image);
            }
        }

        /// <summary>
        /// Saves the image by asking the user to select the location
        /// </summary>
        /// /// <param image="image"></param>
        /// <returns>True if successfu</returns>
        private bool SaveImageDialog(NikonImage image)
        {
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.Filter = (image.Type == NikonImageType.Jpeg) ?
                "Jpeg Image (*.jpg)|*.jpg" :
                "Nikon NEF (*.nef)|*.nef";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (!SaveImageDirectory(image, dialog.FileName))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Saves the image using the provided directory
        /// </summary>
        /// <param name="image"></param>
        /// /// <param name="filename"></param>
        /// <returns>True if successful</returns>
        private bool SaveImageDirectory(NikonImage image, string filename)
        {
            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    stream.Write(image.Buffer, 0, image.Buffer.Length);
                }
                if (image.Type == NikonImageType.Jpeg)
                {
                    Bitmap bitmap;

                    using (var ms = new MemoryStream(image.Buffer))
                    {
                        bitmap = new Bitmap(ms);
                    }

                    pictureBox.Image = bitmap;
                }
            }
            catch (Exception ex)
            {
                Status("Error: " + ex.Message);
                return false;
            }
            return true;
        }

        void device_CaptureComplete(NikonDevice sender, int data)
        {
            // Re-enable buttons when the capture completes
            ToggleButtons(true);
            Status("Photo complete");

            // If shoot sequence
            if (m_shoot_sequence_status == shoot_status.Shooting)
            {
                // See if all the shots have saved
                if (!(!(m_compression.ToLower().Contains("jpeg") ^ m_downloaded_jpeg) ^ !(m_compression.ToLower().Contains("raw") ^ m_downloaded_nef)))
                {

                    m_shoot_sequence_status = shoot_status.Downloaded;
                }
            }
        }

        void ToggleButtons(bool enabled)
        {
            this.buttonCapture.Enabled = enabled;
            this.buttonAutoFocus.Enabled = enabled;
            this.buttonToggleliveview.Enabled = enabled;
        }

        private void buttonToggleliveview_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (NikonDevice device in _devices)
                {
                    if (device != null)
                    {
                        if (device.LiveViewEnabled)
                        {
                            device.LiveViewEnabled = false;
                            liveViewTimer.Stop();
                            pictureBox.Image = null;
                            comboBoxShutterSpeed.Enabled = true;
                            comboBoxApeture.Enabled = true;
                        }
                        else
                        {
                            device.LiveViewEnabled = true;
                            liveViewTimer.Start();
                            comboBoxShutterSpeed.Enabled = false;
                            comboBoxApeture.Enabled = false;
                        }
                        break;
                    }
                }
            }
            catch (NikonException ex)
            {
                Status("Nikon Error while enabling preview: " + ex.Message);
            }
        }
        private void buttonCapture_Click(object sender, EventArgs e)
        {
            ImageCapture();
        }

        private bool ImageCapture()
        {
            Status("Taking photo");

            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {

                    ToggleButtons(false);

                    try
                    {
                        device.Capture();
                    }
                    catch (NikonException ex)
                    {
                        Status("Capture Error: " + ex.Message);
                        ToggleButtons(true);
                    }

                    pictureBox.Image = null;
                    break;
                }
            }
            return true;
        }

        private void buttonAutoFocus_Click(object sender, EventArgs e)
        {
            Status("Auto Focusing Camera");

            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    try
                    {
                        if (device.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_AutoFocus))
                        {
                            if (device.LiveViewEnabled)
                            {
                                device.LiveViewEnabled = false;
                                liveViewTimer.Stop();
                                pictureBox.Image = null;

                                try
                                {
                                    device.Start(eNkMAIDCapability.kNkMAIDCapability_AutoFocus);
                                }
                                catch (NikonException ex)
                                {
                                    errorStatus(ex);
                                }
                                catch (Exception ex)
                                {
                                    Status("Auto Focus Error: " + ex.Message);
                                }

                                device.LiveViewEnabled = true;
                                liveViewTimer.Start();

                                Status("Auto Focus Completed");
                            }
                            else
                            {
                                try
                                {
                                    device.Start(eNkMAIDCapability.kNkMAIDCapability_AutoFocus);
                                }
                                catch (NikonException ex)
                                {
                                    errorStatus(ex);
                                }
                                catch (Exception ex)
                                {
                                    Status("Auto Focus Error: " + ex.Message);
                                }
                            }
                        }
                    }
                    catch (NikonException ex)
                    {
                        Status("Nikon Error while retrieving auto focus capability: " + ex.Message);
                    }
                    break;
                }
            }
        }

        private void errorStatus(NikonException ex)
        {
            if (ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_OutOfFocus ||
                ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_AutoFocusFailed)
            {
                Status("Camera could not focus!");
            }
            else if (ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_BatteryExhausted ||
                     ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_CpxBatteryLow ||
                     ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_BatteryDontWork)
            {
                Status("Camera low battery!");
            }
            else
            {
                Status("Status Error: " + ex.Message);
            }
        }

        private void comboBoxCompression_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel);
                    int index = ((ComboBox)sender).SelectedIndex;
                    en.Index = index;
                    device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel, en);
                    m_compression = en.Value.ToString();
                    break;
                }
            }
        }

        private void comboBoxShutterSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);
                    int index = ((ComboBox)sender).SelectedIndex;
                    en.Index = index;
                    device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed, en);
                    m_shutter_speed = en.Value.ToString();
                    break;
                }
            }
        }

        private void comboBoxApeture_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Aperture);
                    int index = ((ComboBox)sender).SelectedIndex;
                    en.Index = index;
                    device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_Aperture, en);
                    m_apeture = en.Value.ToString();
                    break;
                }
            }
        }

        private void comboBoxSensitivity_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                    int index = ((ComboBox)sender).SelectedIndex;
                    en.Index = index;
                    device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity, en);
                    m_sensitivity = en.Value.ToString();
                    break;
                }
            }
        }

        private void comboBoxFlashSyncTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_FlashSyncTime);
                    int index = ((ComboBox)sender).SelectedIndex;
                    en.Index = index;
                    device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_FlashSyncTime, en);
                    break;
                }
            }
        }

        private void comboBoxFlashSlowLimit_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_FlashSlowLimit);
                    int index = ((ComboBox)sender).SelectedIndex;
                    en.Index = index;
                    device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_FlashSlowLimit, en);
                    break;
                }
            }
        }

        private void checkBoxEnableCopyright_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxEnableCopyright.Checked)
            {
                labelArtistsName.Enabled = true;
                textBoxArtistsName.Enabled = true;
                labelCopyright.Enabled = true;
                textBoxCopyrightInfo.Enabled = true;
                foreach (NikonDevice device in _devices)
                {
                    if (device != null)
                    {
                        device.SetBoolean(eNkMAIDCapability.kNkMAIDCapability_EnableCopyright, true);
                        break;
                    }
                }
            }
            else
            {
                labelArtistsName.Enabled = false;
                textBoxArtistsName.Enabled = false;
                labelCopyright.Enabled = false;
                textBoxCopyrightInfo.Enabled = false;
                foreach (NikonDevice device in _devices)
                {
                    if (device != null)
                    {
                        device.SetBoolean(eNkMAIDCapability.kNkMAIDCapability_EnableCopyright, false);
                        break;
                    }
                }
            }
        }

        private void textBoxArtistsName_TextChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NkMAIDCapInfo cap = device.GetCapabilityInfo(eNkMAIDCapability.kNkMAIDCapability_ArtistName);

                    // Length is fixed for Nikon camera
                    if (((TextBox)sender).Text.Length <= 36)
                    {
                        device.SetString(eNkMAIDCapability.kNkMAIDCapability_ArtistName, ((TextBox)sender).Text);
                    }
                    else
                    {
                        Status("Maximum legth of the Artist Name of 36 characters exceeded.");
                    }
                    break;
                }
            }
        }

        private void textBoxCopyrightInfo_TextChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NkMAIDCapInfo cap = device.GetCapabilityInfo(eNkMAIDCapability.kNkMAIDCapability_CopyrightInfo);

                    // Length is fixed for Nikon camera
                    if (((TextBox)sender).Text.Length <= 54)
                    {
                        device.SetString(eNkMAIDCapability.kNkMAIDCapability_CopyrightInfo, ((TextBox)sender).Text);
                    }
                    else
                    {
                        Status("Maximum legth of the Copyright Information of 54 characters exceeded.");
                    }
                    break;
                }
            }
        }

        private void checkBoxAutoISO_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                // Grey out manual ISO
                comboBoxSensitivity.Enabled = false;
                labelSensitivity.Enabled = false;
                foreach (NikonDevice device in _devices)
                {
                    if (device != null)
                    {
                        device.SetBoolean(eNkMAIDCapability.kNkMAIDCapability_IsoControl, true);
                        break;
                    }
                }
            }
            else
            {
                // Enable manual ISO
                comboBoxSensitivity.Enabled = true;
                labelSensitivity.Enabled = true;
                foreach (NikonDevice device in _devices)
                {
                    if (device != null)
                    {
                        device.SetBoolean(eNkMAIDCapability.kNkMAIDCapability_IsoControl, false);
                        break;
                    }
                }
            }
        }

        private void checkBoxExposureDelay_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                foreach (NikonDevice device in _devices)
                {
                    if (device != null)
                    {
                        device.SetBoolean(eNkMAIDCapability.kNkMAIDCapability_ExposureDelay, true);
                        break;
                    }
                }
            }
            else
            {
                foreach (NikonDevice device in _devices)
                {
                    if (device != null)
                    {
                        device.SetBoolean(eNkMAIDCapability.kNkMAIDCapability_ExposureDelay, false);
                        break;
                    }
                }
            }
        }

        private void buttonTICConnect_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (!m_tic_connected)
                {
                    StatusTIC("Searching for TIC controller");
                    // Iterate through all the tic versions
                    foreach (int id in Enum.GetValues(typeof(tic.PRODUCT_ID)))
                    {
                        try
                        {
                            m_tic_connected = m_tic.open((tic.PRODUCT_ID)id);
                            if (m_tic_connected)
                            {
                                m_tic_name = Enum.GetName(typeof(tic.PRODUCT_ID), id);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            StatusTIC("Failled connecting to " + Enum.GetName(typeof(tic.PRODUCT_ID), id)
                                + ". Error Message: " + ex.Message);
                        }
                    }

                    if (!m_tic_connected) throw (new Exception("TIC stepper motor not found"));

                    StatusTIC(m_tic_name + " controller found");

                    m_tic.reinitialize();
                    m_tic.energize();
                    m_tic.clear_driver_error();

                    m_tic.exit_safe_start();
                    StatusTIC("Waiting for TIC ready state");
                    m_tic.wait_for_device_ready();
                    m_tic.set_target_velocity(0);

                    StatusTIC(m_tic_name + " ready");
                    labelTICConnection.ForeColor = System.Drawing.Color.Green;
                    labelTICConnection.Text = "Connected";
                    buttonTICConnect.Text = "TIC Disconnect";

                    buttonTICResume.Enabled = true;
                    buttonTICDeEnergize.Enabled = true;

                    // Only enable if the camera is available or we don't want to shoot
                    if (m_camera_available || m_no_shooting)
                    {
                        buttonStart.Enabled = true;
                    }
                    buttonStop.Enabled = false;
                    buttonPause.Enabled = false;
                    buttonSetZero.Enabled = true;
                    buttonGoStart.Enabled = true;
                    buttonJogBackward.Enabled = true;
                    buttonJogForward.Enabled = true;

                    buttonStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
                    buttonStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
                    buttonPause.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
                }
                else
                {
                    m_tic.deenergize();
                    m_tic.close();

                    labelTICConnection.ForeColor = System.Drawing.Color.Red;
                    labelTICConnection.Text = "Disconnected";
                    buttonTICConnect.Text = "TIC Connect";
                    StatusTIC("Disconnected");
                    m_tic_connected = false;

                    buttonTICResume.Enabled = false;
                    buttonTICDeEnergize.Enabled = false;
                    buttonStart.Enabled = false;
                    buttonStop.Enabled = false;
                    buttonPause.Enabled = false;
                    buttonSetZero.Enabled = false;
                    buttonGoStart.Enabled = false;
                    buttonJogBackward.Enabled = false;
                    buttonJogForward.Enabled = false;

                    buttonStart.BackColor = System.Drawing.Color.LightGray;
                    buttonStop.BackColor = System.Drawing.Color.LightGray;
                    buttonPause.BackColor = System.Drawing.Color.LightGray;
                }
            }
            catch (Exception ex)
            {
                m_tic_connected = false;
                StatusTIC("Connect Error: " + ex.Message);
            }
        }

        private void buttonJogForward_MouseDown(object sender, MouseEventArgs e)
        {
            if (m_tic_connected)
            {
                Status("Jogging camera forwards");

                m_tic.exit_safe_start();
                m_tic.set_max_speed(m_max_speed);

                if (!m_tic.set_target_velocity(m_jog_speed))
                {
                    Status("Could not set target velocity");
                }
            }
        }

        private void buttonJogForward_MouseUp(object sender, MouseEventArgs e)
        {
            if (!m_tic.set_target_velocity(0))
            {
                Status("Could not set target velocity of 0");
            }
        }

        private void buttonJogBackward_MouseDown(object sender, MouseEventArgs e)
        {
            if (m_tic_connected)
            {
                Status("Jogging camera backwards");

                m_tic.exit_safe_start();
                m_tic.set_max_speed(m_max_speed);

                if (!m_tic.set_target_velocity(-(m_jog_speed)))
                {
                    Status("Could not set target velocity");
                }
            }
        }

        private void buttonJogBackward_MouseUp(object sender, MouseEventArgs e)
        {
            if (!m_tic.set_target_velocity(0))
            {
                Status("Could not set target velocity of 0");
            }
        }

        private void buttonTICResume_Click(object sender, EventArgs e)
        {
            StatusTIC("TIC stepper driver resume");

            m_tic.energize();
            m_tic.clear_driver_error();
            m_tic.exit_safe_start();
            StatusTIC("Waiting for TIC ready state");
            m_tic.wait_for_device_ready();
            StatusTIC("TIC stepper driver resumed");
        }

        private void timerTIC_Tick(object sender, EventArgs e)
        {
            if (m_tic_connected)
            {
                m_tic.reset_command_timeout();
                m_tic.get_variables();
                m_tic.get_status_variables();
                labelCurrentPositionValue.Text = (m_tic.vars.current_position / m_steps_mm).ToString("#,##0.000");

                if (m_moving && m_tic.in_position())
                {
                    m_moving = false;
                }
                if (m_homing && m_tic.in_home())
                {
                    m_homing = false;
                }

                if (m_decelerating && m_tic.vars.current_velocity == 0)
                {
                    m_decelerating = false;
                }

                if (m_tic.status_vars.energized)
                {
                    buttonTICResume.Enabled = false;
                    buttonTICResume.BackColor = Color.LightGray;
                    buttonTICResume.ForeColor = Color.Gray;
                }
                else
                {
                    buttonTICResume.Enabled = true;
                    buttonTICResume.BackColor = Color.Green;
                    buttonTICResume.ForeColor = Color.White;
                }

                m_status_timer++;

                // Update status every second
                if (m_status_timer >= 10)
                {
                    m_status_timer = 0;
                    StatusTIC("TIC Status: " + m_tic.status_vars.operation_state.ToString());
                }
            }
        }

        private void buttonCalculate_Click(object sender, EventArgs e)
        {
            double l_aperture = Double.Parse(textBoxDOFAperture.Text);
            double l_focal_length = Double.Parse(textBoxLensFocalLength.Text);
            double l_subject_distance = Double.Parse(textBoxSubjectDistance.Text);
            double l_subject_depth = Double.Parse(textBoxSubjectDepth.Text);
            double l_macro_tube_size = Double.Parse(textBoxMacroTubeSize.Text);

            // Determin the Circle of Confusion
            double l_circle_confusion = 0;

            switch (comboBoxCameraSensorSize.SelectedItem)
            {
                case "Full Frame(35mm)":
                    l_circle_confusion = 0.03;
                    break;
                case "APC":
                    l_circle_confusion = 0.02;
                    break;
                case "Micro Four Thirds":
                    l_circle_confusion = 0.015;
                    break;
                case "Medium Format":
                    l_circle_confusion = 0.05;
                    break;
                case "1 - inch":
                    l_circle_confusion = 0.01;
                    break;
                default:
                    // Default to full-frame
                    l_circle_confusion = 0.03;
                    break;
            }

            /*
            m = s / f

            Where:

            m is the magnification,
            s is the length of the extension tube,
            f is the focal length of the lens.
            */
            double l_magnification_factor = l_macro_tube_size / l_focal_length;

            /*
            DoF = 2 * N * C * (m + 1)

            Where:

            N is the f-number (aperture),
            C is the circle of confusion.
            */
            m_dof = 2 * l_aperture * l_circle_confusion * (1 + l_magnification_factor);

            double l_shots_required = Math.Ceiling(l_subject_depth / (m_dof / 2));

            double l_steps_distance = l_subject_depth / l_shots_required;

            labelDOF.Text = m_dof.ToString("##0.000") + " mm Depth of Field";
            labelShotsRequired.Text = l_shots_required.ToString("###0") + " shots recomended";
            labelStepSizeRequired.Text = l_steps_distance.ToString("##0.000") + "mm per step";
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsDialog settings = new SettingsDialog();

            settings.Pitch = m_pitch;
            settings.ThreadStarts = m_thread_starts;
            settings.Microsteps = m_microsteps;
            settings.GearRatio = m_gear_ratio;
            settings.StepsMM = m_steps_mm;
            settings.MaxSpeed = m_max_speed;

            if (settings.ShowDialog() == DialogResult.OK)
            {
                m_pitch = settings.Pitch;
                m_thread_starts = settings.ThreadStarts;
                m_microsteps = settings.Microsteps;
                m_gear_ratio = settings.GearRatio;
                m_steps_mm = settings.StepsMM;
                m_max_speed = settings.MaxSpeed;

                Properties.Settings.Default.thread_pitch = m_pitch;
                Properties.Settings.Default.thread_starts = m_thread_starts;
                Properties.Settings.Default.microsteps = m_microsteps;
                Properties.Settings.Default.gear_ratio = m_gear_ratio;
                Properties.Settings.Default.steps_mm = m_steps_mm;
                Properties.Settings.Default.max_speed = m_max_speed;
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs saveAsDialog = new SaveAs(m_project_name, m_project_description, m_project_version, m_project_filename);
            if (saveAsDialog.ShowDialog() == DialogResult.OK)
            {
                m_project_filename = saveAsDialog.Filename;

                m_project_name = saveAsDialog.ProjectName;
                m_project_description = saveAsDialog.Description;
                m_project_version = saveAsDialog.Version;
                saveProject();
                m_project_saved = true;
                saveToolStripMenuItem.Enabled = true;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveProject();
        }

        private void saveProject()
        {
            Status("Saving project");

            Project.Camera camera = new Project.Camera()
            {
                Compression = (string)comboBoxCompression.SelectedItem,
                ShutterSpeed = (string)comboBoxShutterSpeed.SelectedItem,
                Apeture = (string)comboBoxApeture.SelectedItem,
                AutoISO = (bool)checkBoxAutoISO.Checked,
                SensitivityISO = (string)comboBoxSensitivity.SelectedItem,
                FlashSyncTime = (string)comboBoxFlashSyncTime.SelectedItem,
                FlashSlowLimit = (string)comboBoxFlashSlowLimit.SelectedItem,
                ExposureDelay = (bool)checkBoxExposureDelay.Checked,
                EnableCopyright = (bool)checkBoxEnableCopyright.Checked,
                ArtistsName = (string)textBoxArtistsName.Text,
                Copyright = (string)textBoxCopyrightInfo.Text
            };

            Project.MacroRail macroRail = new Project.MacroRail()
            {
                JogSpeed = uint.Parse(textBoxJogSpeed.Text),
                ProjectDirectory = m_project_directory,
                NoShooting = (bool)checkBoxNoShooting.Checked,
                DelayBeforeShooting = uint.Parse(textBoxDelayBeforeShooting.Text),
                StepCount = m_shoot_shots_required,
                StepDistance = m_shoot_steps_distance
            };

            Project project = new Project
            {
                Name = m_project_name,
                Description = m_project_description,
                Version = m_project_version,
                camera = camera,
                macroRail = macroRail
            };

            try
            {
                var json = JsonConvert.SerializeObject(project, Formatting.Indented);

                File.WriteAllText(m_project_filename, json);

                saveToolStripMenuItem.Enabled = true;

                Status("Project saved");
            }
            catch (IOException ex)
            {
                Status($"An IO error occured: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Status($"You do not have permission to write to this file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Status($"A JSON error occurred: {ex.Message}");
            }
            catch (Exception ex)
            {
                Status($"An error occurred: {ex.Message}");
            }
        }


        private void openProject()
        {
            Status("Opening project");

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Macro Slider Project (*.proj)|*.proj|Any|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    m_project_filename = dialog.FileName;

                    string json = File.ReadAllText(dialog.FileName);

                    Project project = JsonConvert.DeserializeObject<Project>(json);

                    // Camera Settings
                    comboBoxCompression.SelectedItem = project.camera.Compression;
                    comboBoxShutterSpeed.SelectedItem = project.camera.ShutterSpeed;
                    comboBoxApeture.SelectedItem = project.camera.Apeture;
                    checkBoxAutoISO.Checked = project.camera.AutoISO;
                    comboBoxSensitivity.SelectedItem = project.camera.SensitivityISO;
                    comboBoxFlashSyncTime.SelectedItem = project.camera.FlashSyncTime;
                    comboBoxFlashSlowLimit.SelectedItem = project.camera.FlashSlowLimit;
                    checkBoxExposureDelay.Checked = project.camera.ExposureDelay;
                    checkBoxEnableCopyright.Checked = project.camera.EnableCopyright;
                    textBoxArtistsName.Text = project.camera.ArtistsName;
                    textBoxCopyrightInfo.Text = project.camera.Copyright;

                    textBoxJogSpeed.Text = project.macroRail.JogSpeed.ToString();
                    m_project_directory = project.macroRail.ProjectDirectory;
                    checkBoxNoShooting.Checked = project.macroRail.NoShooting;
                    textBoxStepCount.Text = project.macroRail.StepCount.ToString();
                    textBoxStepSize.Text = project.macroRail.StepDistance.ToString();
                    textBoxDelayBeforeShooting.Text = project.macroRail.DelayBeforeShooting.ToString();

                    m_project_name = project.Name;
                    m_project_description = project.Description;
                    m_project_version = project.Version;

                    m_shoot_shots_required = project.macroRail.StepCount;
                    m_shoot_steps_distance = project.macroRail.StepDistance;
                    m_shoot_delay_before = project.macroRail.DelayBeforeShooting;

                    m_project_saved = true;

                    saveToolStripMenuItem.Enabled = true;

                    Status("Project opened");
                }
                catch (FileNotFoundException ex)
                {
                    Status($"The file was not found: '{ex.FileName}'");
                }
                catch (IOException ex)
                {
                    Status($"An IO error occured: {ex.Message}");
                }
                catch (JsonException ex)
                {
                    Status($"Invalid JSON format: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Status($"An error occured: {ex.Message}");
                }
            }
        }

        private void exitApplicationToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FormClosingEventArgs fce = new System.Windows.Forms.FormClosingEventArgs(CloseReason.UserClosing, false);
            this.Main_FormClosing(sender, fce);
        }

        private void buttonTICDeEnergize_Click(object sender, EventArgs e)
        {
            StatusTIC("DeEnergizing TIC stepper controller");
            m_tic.deenergize();
            StatusTIC("DeEnergized");
        }

        private void buttonGoStart_Click(object sender, EventArgs e)
        {
            Status("Moving to start position");
            m_tic.exit_safe_start();
            m_tic.set_target_position(m_start);
        }

        private void textBoxStartPosition_TextChanged(object sender, EventArgs e)
        {

        }

        private void buttonSetStart_Click(object sender, EventArgs e)
        {
            m_tic.halt_and_set_position(0);
        }


        private void buttonStart_Click(object sender, EventArgs e)
        {
            // Show the settings
            StartShooting dialog = new StartShooting();
            dialog.ShootName = m_shoot_name;
            dialog.ShootVersion = m_shoot_version;
            if (m_shoot_directory == "")
            {
                m_shoot_directory = m_project_directory;
            }
            dialog.ShootDirectory = m_shoot_directory;
            dialog.ShootStepCount = m_shoot_shots_required.ToString("###0");
            dialog.ShootStepSize = m_shoot_steps_distance.ToString("###0.000");
            dialog.Camera = m_camera;
            dialog.CameraShutterSpeed = m_shutter_speed;
            dialog.CameraApeture = m_apeture;
            dialog.CameraSensitivity = m_sensitivity;
            dialog.ShowDialog();
            if (dialog.DialogResult == DialogResult.OK)
            {
                m_shoot_name = dialog.ShootName;
                m_shoot_version = dialog.ShootVersion;
                m_shoot_directory = dialog.ShootDirectory;
                if (m_project_directory == "") m_project_directory = m_shoot_directory;
                m_shoot_sequence_status = shoot_status.Start;
                // Start the capture timer
                timerProject.Start();
                Status("Starting capture");
            }
        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            // Stop the timer
            if (m_shoot_sequence_status != shoot_status.Idle)
            {
                while (m_shoot_sequence_status == shoot_status.Shooting)
                {
                    Application.DoEvents();
                }

                if (buttonPause.Text == "Resume")
                {
                    Resume("user");
                }
                else
                {
                    Pause("user");
                }
            }
        }

        private void Resume(string resumed_by)
        {
            buttonPause.Text = "Pause";
            m_shoot_sequence_status = m_shoot_pause_previous_state;
            Status("Project resumed by " + resumed_by);
        }
        private void Pause(string paused_by)
        {
            buttonPause.Text = "Resume";
            m_shoot_pause_previous_state = m_shoot_sequence_status;
            m_shoot_sequence_status = shoot_status.Paused;
            Status("Project paused by " + paused_by);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            Status("Stopping capture");
            // Stop the capture
            if (m_shoot_sequence_status != shoot_status.Idle)
            {
                timerProject.Stop();
                m_shoot_sequence_status = shoot_status.Idle;

                buttonStart.Enabled = false;
                buttonPause.Enabled = false;
                buttonStop.Enabled = false;

                Status("Project stopped by user");
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openProject();
        }

        private void textBoxJogSpeed_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(textBoxJogSpeed.Text, out int _))
            {
                textBoxJogSpeed.Text = "0";
            }
            double l_jog_percent = Double.Parse(textBoxJogSpeed.Text);
            if (l_jog_percent < 0)
            {
                textBoxJogSpeed.Text = "0";
                l_jog_percent = 0;
            }
            else if (l_jog_percent > 100)
            {
                textBoxJogSpeed.Text += "100";
                l_jog_percent = 100;
            }

            m_jog_speed = (int)((double)m_max_speed * (l_jog_percent / 100));
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Shutdown motor
            if (m_tic_connected)
            {
                // Put the stepper into a safe mode
                m_tic.halt_and_hold();
                m_tic.deenergize();
                m_tic.close();
            }

            // Shutdown Camera
            liveViewTimer.Stop();
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    try
                    {
                        if (device.LiveViewEnabled)
                        {
                            device.LiveViewEnabled = false;
                        }
                    }
                    catch (NikonException ex) { }
                    catch (Exception ex) { }
                }
            }

            System.Windows.Forms.Application.Exit();
        }

        private void timerProject_Tick(object sender, EventArgs e)
        {
            shotStatus(m_shoot_current_shot, m_shoot_shots_required, m_shoot_sequence_status);

            switch (m_shoot_sequence_status)
            {
                case shoot_status.Start:
                    timerProject.Stop();

                    // Disable start button
                    buttonStart.Enabled = false;
                    // Enable Pause and Stop buttons
                    buttonPause.Enabled = true;
                    buttonStop.Enabled = true;

                    // Diable Preview
                    try
                    {
                        foreach (NikonDevice device in _devices)
                        {
                            if (device != null)
                            {
                                if (device.LiveViewEnabled)
                                {
                                    device.LiveViewEnabled = false;
                                    liveViewTimer.Stop();
                                    pictureBox.Image = null;
                                    comboBoxShutterSpeed.Enabled = true;
                                    comboBoxApeture.Enabled = true;
                                }
                            }
                        }
                    }
                    catch (NikonException ex)
                    {
                        Status("Nikon Error while enabling preview: " + ex.Message);
                    }

                    // Move to the start
                    m_current_position = 0;

                    m_tic.set_target_position(m_current_position);
                    m_tic.wait_for_move_complete();

                    m_current_position = 0;
                    m_shoot_current_shot = 1;
                    m_shoot_delay_counter = 0;
                    m_shoot_sequence_status = shoot_status.Delay;
                    timerProject.Start();

                    break;
                case shoot_status.Delay:
                    // Delay
                    if (m_shoot_delay_before > 0)
                    {
                        m_shoot_sequence_status = shoot_status.Delaying;
                        m_shoot_delay_counter = 0;
                    }
                    else
                    {
                        if (m_no_shooting)
                        {
                            // Skip delay and shooting
                            m_shoot_sequence_status = shoot_status.Downloaded;
                        }
                        else
                        {
                            m_shoot_sequence_status = shoot_status.Shoot;
                        }
                    }

                    break;
                case shoot_status.Delaying:
                    m_shoot_delay_counter++;

                    if (m_shoot_delay_counter >= m_shoot_delay_before * 10)
                    {
                        if (m_no_shooting || (!m_no_shooting && m_camera_available))
                        {
                            // Skip shooting
                            m_shoot_sequence_status = shoot_status.Downloaded;
                        }
                        else
                        {
                            m_shoot_sequence_status = shoot_status.Shoot;
                        }
                    }

                    break;
                case shoot_status.Shoot:
                    // Shoot and wait for callback
                    m_shoot_sequence_status = shoot_status.Shooting;
                    ImageCapture();

                    break;
                case shoot_status.Downloaded:
                    if (m_shoot_current_shot < m_shoot_shots_required)
                    {
                        // Increment the shot counter and restart at the begining
                        m_shoot_current_shot++;
                        m_shoot_sequence_status = shoot_status.Move;
                    }
                    else
                    {
                        // End the sequence
                        m_shoot_sequence_status = shoot_status.End;
                    }

                    break;
                case shoot_status.Move:
                    // Move to position and wait for callback
                    int steps_to_move = (int)(m_steps_mm * m_shoot_steps_distance);
                    m_current_position -= steps_to_move;
                    m_shoot_sequence_status = shoot_status.Moving;

                    timerProject.Stop();

                    m_tic.set_target_position(m_current_position);
                    m_tic.wait_for_move_complete();

                    timerProject.Start();

                    m_shoot_sequence_status = shoot_status.Delay;

                    break;
                case shoot_status.End:
                    // Project ended. Confirm to user and Clean-up
                    m_shoot_sequence_status = shoot_status.Idle;
                    timerProject.Stop();

                    m_tic.set_target_position(0);
                    m_tic.wait_for_move_complete();

                    buttonStart.Enabled = true;
                    buttonStop.Enabled = false;
                    buttonPause.Enabled = false;

                    SystemSounds.Beep.Play();
                    MessageBox.Show("Project Completed", "Project Completed", MessageBoxButtons.OK);

                    break;
                case shoot_status.Paused:
                    // Do nothing
                    break;
                case shoot_status.Idle:
                    // Stop this timer
                    timerProject.Stop();

                    break;
                default:
                    break;

            }
        }

        private void shotStatus(int count, int shots, shoot_status status)
        {
            labelRunStatus.Text = "Shot " + count.ToString() + " of " + shots.ToString() + " - " + status.ToString();
        }

        private void checkBoxStackPreview_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void textBoxDelayBeforeShooting_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                m_shoot_delay_before = UInt32.Parse(textBoxDelayBeforeShooting.Text);
            }
            catch (Exception ex)
            {
                e.Cancel = true;
            }
        }

        private void textBoxStepSize_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                m_shoot_steps_distance = double.Parse(textBoxStepSize.Text);
            }
            catch (Exception ex)
            {
                e.Cancel = true;
            }
        }

        private void textBoxStepCount_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                m_shoot_shots_required = int.Parse(textBoxStepCount.Text);
            }
            catch (Exception ex)
            {
                e.Cancel = true;
            }
        }

        private void textBoxJogSpeed_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int l_jogSpeed = int.Parse((string)textBoxJogSpeed.Text);
                // Check if it within range
                if (l_jogSpeed > 100 || l_jogSpeed < 0)
                {
                    e.Cancel = true;
                }
                else
                {
                    m_jog_speed = l_jogSpeed * (int)(m_max_speed / 100);
                }
            }
            catch (Exception ex)
            {
                e.Cancel = true;
            }
        }

        private void textBoxLensFocalLength_Validating(object sender, CancelEventArgs e)
        {
            uint val;

            if (!uint.TryParse(textBoxLensFocalLength.Text, out val))
            {
                e.Cancel = true;
            }
        }

        private void textBoxMacroTubeSize_Validating(object sender, CancelEventArgs e)
        {
            uint val;

            if (!uint.TryParse(textBoxMacroTubeSize.Text, out val))
            {
                e.Cancel = true;
            }
        }

        private void textBoxDOFAperture_Validating(object sender, CancelEventArgs e)
        {
            uint val;

            if (!uint.TryParse(textBoxDOFAperture.Text, out val))
            {
                e.Cancel = true;
            }
        }

        private void checkBoxNoShooting_CheckedChanged(object sender, EventArgs e)
        {
            m_no_shooting = checkBoxNoShooting.Checked;
            if (m_no_shooting && m_tic_connected)
            {
                buttonStart.Enabled = true;
            }
            else if (buttonStart.Enabled && !m_camera_available)
            {
                buttonStart.Enabled = false;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
             AboutBox1 aboutBox = new AboutBox1();
            aboutBox.ShowDialog();
        }
    }
}