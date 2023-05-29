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
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Media;

namespace MacroRail
{

    // Overload of NikonImageType to add TIFF
    public enum ImageType
    {
        Raw = 1,
        Jpeg = 5,
        Tiff = 6
    }
    public partial class Main : Form
    {
        private System.Windows.Forms.Timer liveViewTimer;
        private int m_status_timer = 0;


        List<NikonManager> _managers;
        List<NikonDevice> _devices;

        bool m_camera_available = false;
        string m_project_filename = "";

        bool m_preview_loaded = false;

        // If the project is opened from a file or saved as was executed we can use Save
        bool m_project_saved = false;

        Rail rail;
        Shoot shoot;
        Project project;

        public Main()
        {
            InitializeComponent();

            Status("Loading");

            project = new Project();

            rail = new Rail();

            // Load the application settings
            rail.pitch = Properties.Settings.Default.thread_pitch;
            rail.thread_starts = Properties.Settings.Default.thread_starts;
            rail.steps_mm = Properties.Settings.Default.steps_mm;
            rail.microsteps = Properties.Settings.Default.microsteps;
            rail.gear_ratio = Properties.Settings.Default.gear_ratio;
            rail.steps_mm = Properties.Settings.Default.steps_mm;
            rail.max_speed = Properties.Settings.Default.max_speed;
            rail.jog_speed = Properties.Settings.Default.jog_speed;

            shoot = new Shoot();

            // Set up defaults
            comboBoxCameraSensorSize.SelectedIndex = 0;
            textBoxJogSpeed.Text = (((double)rail.jog_speed / (double)rail.max_speed) * 100).ToString("###0");

            // Disable buttons
            ToggleButtons(false);

            // Set Start, Stop, Pause buttons to grayed out
            buttonStart.BackColor = Color.LightGray;
            buttonStop.BackColor = Color.LightGray;
            buttonPause.BackColor = Color.LightGray;

            // Initialize live view timer
            liveViewTimer = new System.Windows.Forms.Timer();
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

                    var directory_name = Path.GetDirectoryName(md3);

                    if (directory_name is not null)
                    {
                        string requiredDllPath = System.IO.Path.Combine(directory_name, requiredDllFile);

                        if (!File.Exists(requiredDllPath))
                        {
                            Status("Warning: Couldn't find " + requiredDllFile + " in " + System.IO.Path.GetDirectoryName(md3) + ". The library will not work properly without it!");
                        }
                    }
                    else
                    {
                        Status("MD3 dirtectory path does not exist.");
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
            shoot.tic = new Tic();
        }

        private void Status(string message)
        {
            toolStripStatusLabel1.Text = message;
        }
        private void StatusLog(string message, bool timestamp)
        {
            // Log to shoot log
            try
            {
                if (shoot is not null && shoot.log is not null)
                {
                    if (timestamp)
                    {
                        shoot.log.WriteLine(DateTime.Now.ToString("hh:mm:ss") + ": " + message);
                    }
                    else
                    {
                        shoot.log.WriteLine(message);
                    }
                }
            }
            catch { }
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
            project.camera.Name = device.Name;
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

                    if (en is not null)
                    {
                        for (int x = 0; x < en.Length; x++)
                        {
                            comboBoxShutterSpeed.Items.Add(en.GetEnumValueByIndex(x));
                        }

                        comboBoxShutterSpeed.SelectedIndex = en.Index;

                        project.camera.ShutterSpeed = (string)FromNullable(en.Value);
                    }
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

                    if (en is not null)
                    {
                        for (int x = 0; x < en.Length; x++)
                        {
                            comboBoxApeture.Items.Add(en.GetEnumValueByIndex(x));
                        }

                        comboBoxApeture.SelectedIndex = en.Index;

                        project.camera.Apeture = (string)FromNullable(en.Value);
                    }
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

                    if (en is not null)
                    {
                        for (int x = 0; x < en.Length; x++)
                        {
                            comboBoxSensitivity.Items.Add(en.GetEnumValueByIndex(x));
                        }

                        comboBoxSensitivity.SelectedIndex = en.Index;

                        project.camera.SensitivityISO = (string)FromNullable(en.Value);
                    }
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
            if (shoot.tic_connected)
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
            if (shoot.sequence_status != shoot_status.Idle)
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
                    string artistName = device.GetString(eNkMAIDCapability.kNkMAIDCapability_ArtistName);
                    textBoxArtistsName.Text = artistName;
                    break;
                case eNkMAIDCapability.kNkMAIDCapability_CopyrightInfo:
                    string copyright = device.GetString(eNkMAIDCapability.kNkMAIDCapability_CopyrightInfo);
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

                    if (en is not null)
                    {
                        string value = StringFromNullable(en.Value);

                        switch (capability)
                        {
                            case eNkMAIDCapability.kNkMAIDCapability_CompressionLevel:
                                comboBoxCompression.SelectedIndex = en.Index;
                                project.camera.Compression = value;
                                break;
                            case eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed:
                                comboBoxShutterSpeed.SelectedIndex = en.Index;
                                project.camera.ShutterSpeed = value;
                                break;
                            case eNkMAIDCapability.kNkMAIDCapability_Aperture:
                                comboBoxApeture.SelectedIndex = en.Index;
                                project.camera.Apeture = value;
                                break;
                            case eNkMAIDCapability.kNkMAIDCapability_Sensitivity:
                                comboBoxSensitivity.SelectedIndex = en.Index;
                                project.camera.SensitivityISO = value;
                                break;
                            case eNkMAIDCapability.kNkMAIDCapability_FlashSyncTime:
                                comboBoxFlashSyncTime.SelectedIndex = en.Index;
                                project.camera.FlashSyncTime = value;
                                break;
                            case eNkMAIDCapability.kNkMAIDCapability_FlashSlowLimit:
                                comboBoxFlashSlowLimit.SelectedIndex = en.Index;
                                project.camera.FlashSlowLimit = value;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
            }
        }

        void liveViewTimer_Tick(object sender, EventArgs e)
        {
            // Get live view image
            NikonLiveViewImage? image = null;

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
            if (shoot.sequence_status == shoot_status.Shooting)
            {
                var directory = project.sequence.Directory + @"\" + shoot.name + @"\" + shoot.version + @"\";

                // Create the shoot directory if it doesn't exist
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var filename = directory + shoot.current_shot.ToString();

                switch ((ImageType)image.Type)
                {
                    case ImageType.Jpeg:
                        SaveImageDirectory(image, filename + ".jpg");
                        shoot.downloaded_jpeg = true;
                        break;
                    case ImageType.Raw:
                        SaveImageDirectory(image, filename + ".NEF");
                        shoot.downloaded_nef = true;
                        break;
                    case ImageType.Tiff:
                        SaveImageDirectory(image, filename + ".tif");
                        shoot.downloaded_tiff = true;
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

            string filter = "*.*|All";

            switch ((ImageType)image.Type)
            {
                case ImageType.Jpeg:
                    filter = "Jpeg Image (*.jpg)|*.jpg|All (*.*)|*.*";
                    break;
                case ImageType.Raw:
                    filter = "RAW/NEF Image (*.nef)|*.nef|All (*.*)|*.*";
                    break;
                case ImageType.Tiff:
                    filter = "TIFF-RGB Image (*.tif)|*.tif|All (*.*)|*.*";
                    break;
                default:
                    filter = "All (*.*)|*.*|All (*.*)|*.*";
                    break;
            }

            dialog.Filter = filter;

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
            Bitmap bitmap = BlankBitmap(1024, 768, Color.Black, Brushes.White, "No Image", 32, "Arial");

            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    stream.Write(image.Buffer, 0, image.Buffer.Length);

                }

                if ((ImageType)image.Type == ImageType.Jpeg)
                {
                    using (var ms = new MemoryStream(image.Buffer))
                    {
                        bitmap = new Bitmap(ms);
                    }

                    pictureBox.Image = bitmap;
                    m_preview_loaded = true;
                }
                else if ((ImageType)image.Type == ImageType.Raw)
                {
                    NEF_Image raw = new NEF_Image();
                    raw.Load(image.Buffer);
                    if (raw.JpegMonitor is not null)
                    {
                        using (var mon = new MemoryStream(raw.JpegMonitor))
                        {
                            bitmap = new Bitmap(mon);
                        }

                        pictureBox.Image = bitmap;
                        m_preview_loaded = true;
                    }
                    else if (raw.JpegPreview is not null)
                    {
                        using (var pre = new MemoryStream(raw.JpegPreview))
                        {
                            bitmap = new Bitmap(pre);
                        }

                        pictureBox.Image = bitmap;
                        m_preview_loaded = true;
                    }
                }
                else if ((ImageType)image.Type == ImageType.Tiff)
                {
                    using (var ms = new MemoryStream(image.Buffer))
                    {
                        Bitmap tiff = new Bitmap(ms);

                        // Get the first frame
                        FrameDimension dimension = new FrameDimension(tiff.FrameDimensionsList[0]);
                        bitmap = tiff.Clone(new Rectangle(0, 0, tiff.Width, tiff.Height), tiff.PixelFormat);

                        // Dispose the original Bitmap
                        tiff.Dispose();
                    }

                    pictureBox.Image = bitmap;
                    m_preview_loaded = true;
                }
            }
            catch (Exception ex)
            {
                Status("Error: " + ex.Message);
                return false;
            }

            if (!m_preview_loaded)
            {
                pictureBox.Image = bitmap;
            }

            return true;
        }

        void device_CaptureComplete(NikonDevice sender, int data)
        {
            // Re-enable buttons when the capture completes
            ToggleButtons(true);
            Status("Photo complete");

            // If shoot sequence
            if (shoot.sequence_status == shoot_status.Shooting)
            {
                // See if all the shots have saved
                if ((!(project.camera.Compression.ToLower().Contains("jpeg") ^ shoot.downloaded_jpeg)
                    ^ !(project.camera.Compression.ToLower().Contains("raw") ^ shoot.downloaded_nef)
                    ^ !(project.camera.Compression.ToLower().Contains("tiff") ^ shoot.downloaded_tiff)))
                {

                    shoot.sequence_status = shoot_status.Downloaded;
                }
            }
        }

        Bitmap BlankBitmap(int width, int height, Color background_color, Brush text_color, string text, int font_size, string font_name)
        {
            Bitmap bitmap = new Bitmap(width, height);

            // create a Graphics object from the Bitmap
            using (Graphics gfx = Graphics.FromImage(bitmap))
            {
                // fill the Bitmap with black
                gfx.Clear(background_color);

                // choose a font
                Font font = new Font(font_name, font_size);

                // measure the size of the text
                SizeF textSize = gfx.MeasureString(text, font);

                // calculate the position to center the text
                float x = (width - textSize.Width) / 2;
                float y = (height - textSize.Height) / 2;

                // choose a color for the text
                Brush brush = text_color;

                // draw the text
                gfx.DrawString(text, font, brush, x, y);
            }

            return bitmap;
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

            m_preview_loaded = false;

            Bitmap bitmap = BlankBitmap(1024, 768, Color.Black, Brushes.White, "Capturing Image", 32, "Arial");

            pictureBox.Image = bitmap;

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

        public void comboBoxCompression_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel);

                    if (en is not null)
                    {
                        int index = ((ComboBox)sender).SelectedIndex;
                        en.Index = index;
                        device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel, en);
                        project.camera.Compression = (string)FromNullable(en.Value);
                        break;
                    }
                }
            }
        }

        public void comboBoxShutterSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);

                    if (en is not null)
                    {
                        int index = ((ComboBox)sender).SelectedIndex;
                        en.Index = index;
                        device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed, en);
                        project.camera.ShutterSpeed = (string)FromNullable(en.Value);
                        break;
                    }
                }
            }
        }

        public void comboBoxApeture_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Aperture);

                    if (en is not null)
                    {
                        int index = ((ComboBox)sender).SelectedIndex;
                        en.Index = index;
                        device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_Aperture, en);
                        project.camera.Apeture = (string)FromNullable(en.Value);
                        break;
                    }
                }
            }
        }

        public void comboBoxSensitivity_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (NikonDevice device in _devices)
            {
                if (device != null)
                {
                    NikonEnum en = device.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);

                    if (en is not null)
                    {
                        int index = ((ComboBox)sender).SelectedIndex;
                        en.Index = index;
                        device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity, en);
                        project.camera.SensitivityISO = (string)FromNullable(en.Value);
                        break;
                    }
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

                    if (en is not null)
                    {
                        int index = ((ComboBox)sender).SelectedIndex;
                        en.Index = index;
                        device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_FlashSyncTime, en);
                        break;
                    }
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

                    if (en is not null)
                    {
                        int index = ((ComboBox)sender).SelectedIndex;
                        en.Index = index;
                        device.SetEnum(eNkMAIDCapability.kNkMAIDCapability_FlashSlowLimit, en);
                        break;
                    }
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
                if (!shoot.tic_connected)
                {
                    StatusTIC("Searching for TIC controller");
                    // Iterate through all the tic versions
                    foreach (int id in Enum.GetValues(typeof(Tic.PRODUCT_ID)))
                    {
                        try
                        {
                            shoot.tic_connected = shoot.tic.open((Tic.PRODUCT_ID)id);
                            if (shoot.tic_connected)
                            {
                                var value = Enum.GetName(typeof(Tic.PRODUCT_ID), id);
                                shoot.tic_name = FromNullable(value); ;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            StatusTIC("Failled connecting to " + Enum.GetName(typeof(Tic.PRODUCT_ID), id)
                                + ". Error Message: " + ex.Message);
                        }
                    }

                    if (!shoot.tic_connected) throw (new Exception("TIC stepper motor not found"));

                    StatusTIC(shoot.tic_name + " controller found");

                    shoot.tic.reinitialize();
                    shoot.tic.energize();
                    shoot.tic.clear_driver_error();

                    shoot.tic.exit_safe_start();
                    StatusTIC("Waiting for TIC ready state");
                    shoot.tic.wait_for_device_ready();
                    shoot.tic.set_target_velocity(0);

                    StatusTIC(shoot.tic_name + " ready");
                    labelTICConnection.ForeColor = Color.Green;
                    labelTICConnection.Text = "Connected";
                    buttonTICConnect.Text = "TIC Disconnect";

                    buttonTICResume.Enabled = true;

                    buttonTICDeEnergize.Enabled = true;
                    buttonTICDeEnergize.BackColor = Color.Red;
                    buttonTICDeEnergize.ForeColor = Color.White;

                    // Only enable if the camera is available or we don't want to shoot
                    if (m_camera_available || (bool)project.sequence.NoShooting)
                    {
                        buttonStart.Enabled = true;
                    }
                    buttonStop.Enabled = false;
                    buttonPause.Enabled = false;

                    buttonSetZero.Enabled = true;
                    buttonGoStart.Enabled = true;

                    buttonJogBackward.Enabled = true;
                    buttonJogForward.Enabled = true;

                    buttonStart.BackColor = Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
                    buttonStop.BackColor = Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
                    buttonPause.BackColor = Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
                }
                else
                {
                    shoot.tic.deenergize();
                    shoot.tic.close();

                    labelTICConnection.ForeColor = Color.Red;
                    labelTICConnection.Text = "Disconnected";
                    buttonTICConnect.Text = "TIC Connect";
                    StatusTIC("Disconnected");
                    shoot.tic_connected = false;

                    buttonTICResume.Enabled = false;
                    buttonTICResume.ForeColor = SystemColors.ControlText;
                    buttonTICResume.BackColor = SystemColors.Control;

                    buttonTICDeEnergize.Enabled = false;
                    buttonTICDeEnergize.ForeColor = SystemColors.ControlText;
                    buttonTICDeEnergize.BackColor = SystemColors.Control;

                    buttonTICConnect.BackColor = SystemColors.Control;

                    buttonStart.Enabled = false;
                    buttonStop.Enabled = false;
                    buttonPause.Enabled = false;

                    buttonSetZero.Enabled = false;
                    buttonGoStart.Enabled = false;

                    buttonJogBackward.Enabled = false;
                    buttonJogForward.Enabled = false;

                    buttonStart.BackColor = SystemColors.Control;
                    buttonStop.BackColor = SystemColors.Control;
                    buttonPause.BackColor = SystemColors.Control;
                }
            }
            catch (Exception ex)
            {
                shoot.tic_connected = false;
                StatusTIC("Connect Error: " + ex.Message);
            }
        }

        private void buttonJogForward_MouseDown(object sender, MouseEventArgs e)
        {
            if (shoot.tic_connected)
            {
                Status("Jogging camera forwards");

                shoot.tic.exit_safe_start();
                shoot.tic.set_max_speed((int)rail.max_speed);

                if (!shoot.tic.set_target_velocity((int)rail.jog_speed))
                {
                    Status("Could not set target velocity");
                }
            }
        }

        private void buttonJogForward_MouseUp(object sender, MouseEventArgs e)
        {
            if (!shoot.tic.set_target_velocity(0))
            {
                Status("Could not set target velocity of 0");
            }
        }

        private void buttonJogBackward_MouseDown(object sender, MouseEventArgs e)
        {
            if (shoot.tic_connected)
            {
                Status("Jogging camera backwards");

                shoot.tic.exit_safe_start();
                shoot.tic.set_max_speed((int)rail.max_speed);

                if (!shoot.tic.set_target_velocity(-((int)rail.jog_speed)))
                {
                    Status("Could not set target velocity");
                }
            }
        }

        private void buttonJogBackward_MouseUp(object sender, MouseEventArgs e)
        {
            if (!shoot.tic.set_target_velocity(0))
            {
                Status("Could not set target velocity of 0");
            }
        }

        private void buttonTICResume_Click(object sender, EventArgs e)
        {
            StatusTIC("TIC stepper driver resume");

            shoot.tic.energize();
            shoot.tic.clear_driver_error();
            shoot.tic.exit_safe_start();
            StatusTIC("Waiting for TIC ready state");
            shoot.tic.wait_for_device_ready();

            buttonTICDeEnergize.Enabled = true;
            buttonTICDeEnergize.ForeColor = Color.White;
            buttonTICDeEnergize.BackColor = Color.Red;

            // Only enable if the camera is available or we don't want to shoot
            if (m_camera_available || (bool)project.sequence.NoShooting)
            {
                buttonStart.Enabled = true;
            }
            buttonStop.Enabled = false;
            buttonPause.Enabled = false;

            buttonSetZero.Enabled = true;
            buttonGoStart.Enabled = true;

            buttonJogBackward.Enabled = true;
            buttonJogForward.Enabled = true;

            buttonStart.BackColor = Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            buttonStop.BackColor = Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            buttonPause.BackColor = Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));

            StatusTIC("TIC stepper driver resumed");
        }

        private void timerTIC_Tick(object sender, EventArgs e)
        {
            if (shoot.tic_connected)
            {
                shoot.tic.reset_command_timeout();
                shoot.tic.get_variables();
                shoot.tic.get_status_variables();
                labelCurrentPositionValue.Text = (shoot.tic.vars.current_position / rail.steps_mm).ToString("#,##0.000");

                if (shoot.moving && shoot.tic.in_position())
                {
                    shoot.moving = false;
                }
                if (shoot.homing && shoot.tic.in_home())
                {
                    shoot.homing = false;
                }

                if (shoot.decelerating && shoot.tic.vars.current_velocity == 0)
                {
                    shoot.decelerating = false;
                }

                if (shoot.tic.status_vars.energized)
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
                    StatusTIC("TIC Status: " + shoot.tic.status_vars.operation_state.ToString());
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
            DoF = 2 * u * N * (m + 1) * C

            where:

            u is the distance to the subject
            N is the aperture (f - number)
            m is the magnification, calculated as f / u, where f is the lens focal length
            C is the circle of confusion
            When using extension tubes, the magnification increases by e / f, where e is the length of the extension tube(s). So the new magnification m' is m + e / f.
            */

            // Calculate the original magnification
            double magnification = l_focal_length / l_subject_distance;

            // Add the increase in magnification from the extension tubes
            magnification += l_macro_tube_size / l_focal_length;

            // Calculate the depth of field
            shoot.dof = 2 * l_subject_distance * l_aperture * (magnification + 1) * l_circle_confusion;

            double l_shots_required = Math.Ceiling(l_subject_depth / (shoot.dof / 2));

            double l_steps_distance = l_subject_depth / l_shots_required;

            labelDOF.Text = shoot.dof.ToString("##0.000") + " mm Depth of Field";
            labelShotsRequired.Text = l_shots_required.ToString("###0") + " shots recomended";
            labelStepSizeRequired.Text = l_steps_distance.ToString("##0.000") + "mm per step";
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsDialog settings = new SettingsDialog();

            settings.Pitch = rail.pitch;
            settings.ThreadStarts = rail.thread_starts;
            settings.StepsPerRevolution = rail.steps_rev;
            settings.Microsteps = rail.microsteps;
            settings.GearRatio = rail.gear_ratio;
            settings.StepsMM = rail.steps_mm;
            settings.MaxSpeed = rail.max_speed;
            settings.JogSpeed = rail.jog_speed;

            if (settings.ShowDialog() == DialogResult.OK)
            {
                rail.pitch = settings.Pitch;
                rail.thread_starts = settings.ThreadStarts;
                rail.steps_rev = settings.StepsPerRevolution;
                rail.microsteps = settings.Microsteps;
                rail.gear_ratio = settings.GearRatio;
                rail.steps_mm = settings.StepsMM;
                rail.max_speed = settings.MaxSpeed;
                rail.jog_speed = settings.JogSpeed;

                Properties.Settings.Default.thread_pitch = rail.pitch;
                Properties.Settings.Default.thread_starts = rail.thread_starts;
                Properties.Settings.Default.microsteps = rail.microsteps;
                Properties.Settings.Default.gear_ratio = rail.gear_ratio;
                Properties.Settings.Default.steps_mm = rail.steps_mm;
                Properties.Settings.Default.max_speed = rail.max_speed;
                Properties.Settings.Default.jog_speed = rail.jog_speed;
                Properties.Settings.Default.Save();

                textBoxJogSpeed.Text = (((double)rail.jog_speed / (double)rail.max_speed) * 100).ToString("###0");
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs saveAsDialog = new SaveAs(project.Name, project.Description, project.Version, m_project_filename);

            if (saveAsDialog.ShowDialog() == DialogResult.OK)
            {
                m_project_filename = saveAsDialog.Filename;
                project.Name = saveAsDialog.ProductName;
                project.Description = saveAsDialog.Description;
                project.Version = saveAsDialog.Version;

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

            Project.Sequence sequence = new Project.Sequence()
            {
                JogSpeed = uint.Parse(textBoxJogSpeed.Text),
                Directory = project.Directory,
                NoShooting = (bool)checkBoxNoShooting.Checked,
                ManualShooting = (bool)checkBoxManualShooting.Checked,
                DelayBeforeShooting = uint.Parse(textBoxDelayBeforeShooting.Text),
                StepCount = project.sequence.StepCount,
                StepDistance = project.sequence.StepDistance,
            };

            project.camera = camera;
            project.sequence = sequence;

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

                    Project project = FromNullable(JsonConvert.DeserializeObject<Project>(json));

                    // Camera Settings
                    if (project is not null && project.sequence is not null && project.camera is not null)
                    {
                        comboBoxCompression.SelectedItem = FromNullable(project.camera.Compression);
                        comboBoxShutterSpeed.SelectedItem = FromNullable(project.camera.ShutterSpeed);
                        comboBoxApeture.SelectedItem = FromNullable(project.camera.Apeture);
                        checkBoxAutoISO.Checked = (bool)FromNullable(project.camera.AutoISO);
                        comboBoxSensitivity.SelectedItem = FromNullable(project.camera.SensitivityISO);
                        comboBoxFlashSyncTime.SelectedItem = FromNullable(project.camera.FlashSyncTime);
                        comboBoxFlashSlowLimit.SelectedItem = FromNullable(project.camera.FlashSlowLimit);
                        checkBoxExposureDelay.Checked = (bool)FromNullable(project.camera.ExposureDelay);
                        checkBoxEnableCopyright.Checked = (bool)FromNullable(project.camera.EnableCopyright);
                        textBoxArtistsName.Text = FromNullable(project.camera.ArtistsName);
                        textBoxCopyrightInfo.Text = FromNullable(project.camera.Copyright);
                        textBoxJogSpeed.Text = FromNullable(project.sequence.JogSpeed.ToString());
                        checkBoxNoShooting.Checked = (bool)FromNullable(project.sequence.NoShooting);
                        checkBoxManualShooting.Checked = (bool)FromNullable(project.sequence.ManualShooting);
                        textBoxStepCount.Text = FromNullable(project.sequence.StepCount.ToString());
                        textBoxStepSize.Text = FromNullable(project.sequence.StepDistance.ToString());
                        textBoxDelayBeforeShooting.Text = FromNullable(project.sequence.DelayBeforeShooting.ToString());

                        m_project_saved = true;

                        saveToolStripMenuItem.Enabled = true;

                        Status("Project opened");
                    }
                    else
                    {
                        Status("Error saving project. ");
                    }
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

            shoot.tic.deenergize();

            buttonTICResume.Enabled = true;
            buttonTICResume.ForeColor = Color.White;
            buttonTICResume.BackColor = Color.Green;

            buttonTICDeEnergize.Enabled = false;
            buttonTICDeEnergize.ForeColor = SystemColors.ControlText;
            buttonTICDeEnergize.BackColor = SystemColors.Control;

            buttonStart.Enabled = false;
            buttonStop.Enabled = false;
            buttonPause.Enabled = false;

            buttonSetZero.Enabled = false;
            buttonGoStart.Enabled = false;

            buttonJogBackward.Enabled = false;
            buttonJogForward.Enabled = false;

            buttonStart.BackColor = SystemColors.Control;
            buttonStop.BackColor = SystemColors.Control;
            buttonPause.BackColor = SystemColors.Control;

            StatusTIC("DeEnergized");
        }

        private void buttonGoStart_Click(object sender, EventArgs e)
        {
            Status("Moving to start position");
            shoot.tic.exit_safe_start();
            shoot.tic.set_target_position(shoot.start);
        }

        private void textBoxStartPosition_TextChanged(object sender, EventArgs e)
        {

        }

        private void buttonSetStart_Click(object sender, EventArgs e)
        {
            shoot.tic.halt_and_set_position(0);
        }


        private void buttonStart_Click(object sender, EventArgs e)
        {
            // Show the settings
            StartShooting dialog = new StartShooting();
            dialog.ShootName = shoot.name;
            dialog.ShootVersion = shoot.version;
            if (project.sequence.Directory == "")
            {
                project.sequence.Directory = project.Directory;
            }
            dialog.ShootDirectory = FromNullable(project.sequence.Directory);
            dialog.ShootStepCount = StringFromNullable(project.sequence.StepCount, "###0");
            dialog.ShootStepSize = StringFromNullable(project.sequence.StepDistance, "###0.000");
            dialog.Camera = FromNullable(project.camera.Name);
            dialog.CameraShutterSpeed = FromNullable(project.camera.ShutterSpeed);
            dialog.CameraApeture = FromNullable(project.camera.Apeture);
            dialog.CameraSensitivity = FromNullable(project.camera.SensitivityISO);
            dialog.ShowDialog();
            if (dialog.DialogResult == DialogResult.OK)
            {
                shoot.name = dialog.ShootName;
                shoot.version = dialog.ShootVersion;
                project.sequence.Directory = dialog.ShootDirectory;
                if (project.Directory == "") project.Directory = project.sequence.Directory;
                shoot.sequence_status = shoot_status.Start;

                // Open the log file
                try
                {
                    shoot.log_open = false;

                    if (project is not null && project.sequence is not null)
                    {
                        Directory.CreateDirectory(project.sequence.Directory
                            + @"\" + shoot.name + @"\" + shoot.version);

                        shoot.log = File.CreateText(project.sequence.Directory
                            + @"\" + shoot.name + @"\" + shoot.version
                            + @"\log-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
                        shoot.log_open = true;
                        shoot.log.AutoFlush = true;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if (MessageBox.Show("The log could not be created due to an Unauthorised Access Exception. Do you want to continue with the shoot?", "Error Creating Log File", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                catch (PathTooLongException)
                {
                    MessageBox.Show("The log could not be created due as the path to the file is too long. Cancelling shoot as this also means the images will not be saved?", "Error Creating Log File", MessageBoxButtons.OK);
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("The log could not be created diue to directory not found. Cancelling shoot as this also means the images will not be saved?", "Error Creating Log File", MessageBoxButtons.OK);
                    return;
                }
                catch (Exception)
                {
                    if (MessageBox.Show("The log could not be created. Do you want to continue with the shoot?", "Error Creating Log File", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                    {
                        return;
                    }
                }

                if (shoot.log_open && project is not null && project.sequence is not null)
                {
                    StatusLog("================================================", false);
                    StatusLog("Project Name:        " + project.Name, false);
                    StatusLog("Project Description: " + project.Description, false);
                    StatusLog("Project Version:     " + project.Version, false);
                    StatusLog("Project Version:     " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), false);
                    StatusLog("------------------------------------------------", false);
                    StatusLog("Shoot Name:          " + shoot.name, false);
                    StatusLog("Shoot Version:       " + shoot.version, false);
                    StatusLog("Shots:               " + project.sequence.StepCount, false);
                    StatusLog("Step Distance:       " + project.sequence.StepDistance, false);
                    StatusLog("Camera:              " + project.camera.Name, false);
                    StatusLog("Apeture:             " + project.camera.Apeture, false);
                    StatusLog("Shutter Speed:       " + project.camera.ShutterSpeed, false);
                    StatusLog("Sensitivity/ISO:     " + project.camera.SensitivityISO, false);
                    if ((bool)project.sequence.ManualShooting)
                    {
                        StatusLog("\r\nThis shoot was shot manually.", false);
                    }
                    StatusLog("================================================", false);
                    StatusLog("", false);
                }
                // Reset shoot variables
                shoot.downloaded_jpeg = false;
                shoot.downloaded_nef = false;
                shoot.downloaded_tiff = false;

                // Reset the TIC
                shoot.tic.clear_driver_error();
                shoot.tic.exit_safe_start();
                StatusTIC("Waiting for TIC ready state");
                shoot.tic.wait_for_device_ready();
                StatusTIC("TIC stepper driver resumed");

                // Start the capture timer
                timerProject.Start();
                Status("Starting capture");
                StatusLog("Starting capture", true);
            }
        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            // Stop the timer
            if (shoot.sequence_status != shoot_status.Idle)
            {
                while (shoot.sequence_status == shoot_status.Shooting)
                {
                    System.Windows.Forms.Application.DoEvents();
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
            shoot.sequence_status = shoot.pause_previous_state;
            string message = "Project resumed by " + resumed_by;
            Status(message);
            StatusLog(message, true);
        }
        private void Pause(string paused_by)
        {
            buttonPause.Text = "Resume";
            shoot.pause_previous_state = shoot.sequence_status;
            shoot.sequence_status = shoot_status.Paused;
            string message = "Project paused by " + paused_by;
            Status(message);
            StatusLog(message, true);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            Status("Stopping capture");
            // Stop the capture
            if (shoot.sequence_status != shoot_status.Idle)
            {
                timerProject.Stop();
                shoot.sequence_status = shoot_status.Idle;

                buttonStart.Enabled = false;
                buttonPause.Enabled = false;
                buttonStop.Enabled = false;

                string message = "Project stopped by user";
                Status(message);
                StatusLog(message, true);
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

            rail.jog_speed = (uint)((double)rail.max_speed * (l_jog_percent / 100));
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Shutdown motor
            if (shoot.tic_connected)
            {
                // Put the stepper into a safe mode
                shoot.tic.halt_and_hold();
                shoot.tic.deenergize();
                shoot.tic.close();
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
                    catch (NikonException) { }
                    catch { }
                }
            }

            System.Windows.Forms.Application.Exit();
        }

        private void timerProject_Tick(object sender, EventArgs e)
        {
            int step_count = (int)FromNullable(project.sequence.StepCount);

            shotStatus(shoot.current_shot, step_count, shoot.sequence_status);

            switch (shoot.sequence_status)
            {
                case shoot_status.Start:
                    timerProject.Stop();

                    // Disable start button
                    buttonStart.Enabled = false;
                    // Enable Pause and Stop buttons
                    buttonPause.Enabled = true;
                    buttonStop.Enabled = true;

                    // Disable Preview
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
                    catch (NikonException)
                    {

                    }

                    // Move to the start
                    shoot.current_position = 0;

                    shoot.tic.set_target_position(shoot.current_position);
                    shoot.tic.wait_for_move_complete();

                    shoot.current_position = 0;
                    shoot.current_shot = 1;
                    shoot.delay_counter = 0;
                    shoot.sequence_status = shoot_status.Delay;
                    timerProject.Start();

                    string message = "Shooting sequence started.";
                    Status(message);
                    StatusLog(message, true);

                    break;
                case shoot_status.Delay:
                    // Delay
                    if (project.sequence.DelayBeforeShooting > 0)
                    {
                        shoot.sequence_status = shoot_status.Delaying;
                        shoot.delay_counter = 0;
                    }
                    else if ((bool)project.sequence.NoShooting && !(bool)project.sequence.ManualShooting)
                    {
                        // Skip shooting
                        shoot.sequence_status = shoot_status.Downloaded;
                    }
                    else if ((bool)project.sequence.NoShooting)
                    {
                        // Skip shooting
                        shoot.sequence_status = shoot_status.ManualShoot;
                    }
                    else
                    {
                        shoot.sequence_status = shoot_status.Shoot;
                    }

                    break;
                case shoot_status.Delaying:
                    shoot.delay_counter++;

                    if (shoot.delay_counter >= project.sequence.DelayBeforeShooting * 10)
                    {
                        if ((bool)project.sequence.ManualShooting)
                        {
                            shoot.sequence_status = shoot_status.ManualShoot;
                        }
                        else if ((bool)project.sequence.NoShooting && !(bool)project.sequence.ManualShooting)
                        {
                            // Skip shooting
                            shoot.sequence_status = shoot_status.Downloaded;
                        }
                        else
                        {
                            shoot.sequence_status = shoot_status.Shoot;
                        }
                    }

                    break;
                case shoot_status.Shoot:
                    // Shoot and wait for callback
                    shoot.sequence_status = shoot_status.Shooting;
                    StatusLog("Starting image capture of step " + shoot.current_shot.ToString("##0") + ".", true);

                    ImageCapture();

                    break;
                case shoot_status.ManualShoot:
                    timerProject.Stop();

                    if (MessageBox.Show("Trigger Camera", "Trigger Camera", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        // Skip delay and shooting
                        shoot.sequence_status = shoot_status.Downloaded;
                        timerProject.Start();
                    }
                    else
                    {
                        MessageBox.Show("You have selected Cancel which has cancelled the sequence.", "Sequence Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        shoot.sequence_status = shoot_status.End;
                    }

                    break;
                case shoot_status.Downloaded:

                    StatusLog("Image capture complete.", true);

                    if (shoot.current_shot < project.sequence.StepCount)
                    {
                        // Increment the shot counter and restart at the begining
                        shoot.current_shot++;
                        shoot.sequence_status = shoot_status.Move;
                    }
                    else
                    {
                        // End the sequence
                        shoot.sequence_status = shoot_status.End;
                    }

                    break;
                case shoot_status.Move:
                    // Move to position and wait for callback
                    StatusLog("Starting move.", true);
                    int steps_to_move = (int)(rail.steps_mm * project.sequence.StepDistance);
                    shoot.current_position -= steps_to_move;
                    shoot.sequence_status = shoot_status.Moving;

                    timerProject.Stop();

                    shoot.tic.set_target_position(shoot.current_position);
                    shoot.tic.wait_for_move_complete();

                    StatusLog("Move complete.", true);

                    timerProject.Start();

                    shoot.sequence_status = shoot_status.Delay;

                    break;
                case shoot_status.End:
                    // Project ended. Confirm to user and Clean-up
                    shoot.sequence_status = shoot_status.Idle;
                    timerProject.Stop();

                    StatusLog("Moving to start position.", true);
                    shoot.tic.set_target_position(0);
                    shoot.tic.wait_for_move_complete();

                    StatusLog("Moving to start position completed.", true);

                    buttonStart.Enabled = true;
                    buttonStop.Enabled = false;
                    buttonPause.Enabled = false;

                    StatusLog("Shoot sequence completed.", true);
                    if (shoot.log is not null)
                    {
                        shoot.log.Flush();
                        shoot.log.Close();
                        shoot.log_open = false;
                    }

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
                project.sequence.DelayBeforeShooting = UInt32.Parse(textBoxDelayBeforeShooting.Text);
            }
            catch (Exception)
            {
                e.Cancel = true;
            }
        }

        private void textBoxStepSize_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                project.sequence.StepDistance = double.Parse(textBoxStepSize.Text);
            }
            catch (Exception)
            {
                e.Cancel = true;
            }
        }

        private void textBoxStepCount_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                project.sequence.StepCount = int.Parse(textBoxStepCount.Text);
            }
            catch (Exception)
            {
                e.Cancel = true;
            }
        }

        private void textBoxJogSpeed_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                uint l_jogSpeed = uint.Parse((string)textBoxJogSpeed.Text);
                // Check if it within range
                if (l_jogSpeed > 100 || l_jogSpeed < 0)
                {
                    e.Cancel = true;
                }
                else
                {
                    rail.jog_speed = (uint)(l_jogSpeed * (rail.max_speed / 100));
                }
            }
            catch (Exception)
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
            project.sequence.NoShooting = checkBoxNoShooting.Checked;

            if ((bool)FromNullable(project.sequence.NoShooting) && shoot.tic_connected)
            {
                buttonStart.Enabled = true;
            }
            else if (buttonStart.Enabled && !m_camera_available)
            {
                buttonStart.Enabled = false;
            }
        }


        private void checkBoxManualShooting_CheckedChanged(object sender, EventArgs e)
        {
            project.sequence.ManualShooting = checkBoxManualShooting.Checked;

            if ((bool)FromNullable(project.sequence.ManualShooting) && shoot.tic_connected)
            {
                buttonStart.Enabled = true;
                checkBoxNoShooting.Checked = true;
                checkBoxNoShooting.Enabled = false;
            }
            else if ((bool)FromNullable(project.sequence.ManualShooting) && !shoot.tic_connected)
            {
                buttonStart.Enabled = false;
                checkBoxNoShooting.Checked = true;
                checkBoxNoShooting.Enabled = false;
            }
            else
            {
                checkBoxNoShooting.Checked = false;
                checkBoxNoShooting.Enabled = true;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 aboutBox = new AboutBox1();
            aboutBox.ShowDialog();
        }

        public static T FromNullable<T>(T value)
        {
            if (value == null)
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)"";
                }
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)false;
                }
                else
                {
                    return (T)(object)0;
                }
            }

            return value;
        }

        public static string StringFromNullable(object value)
        {
            return StringFromNullable(value, "");
        }

        public static string StringFromNullable(object value, string format = "")
        {
            if (value.GetType().IsValueType)
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Byte:
                        return ((byte)value).ToString(format);
                    case TypeCode.SByte:
                        return ((SByte)value).ToString(format);
                    case TypeCode.UInt16:
                        return ((UInt16)value).ToString(format);
                    case TypeCode.UInt32:
                        return ((UInt32)value).ToString(format);
                    case TypeCode.UInt64:
                        return ((UInt64)value).ToString(format);
                    case TypeCode.Int16:
                        return ((Int16)value).ToString(format);
                    case TypeCode.Int32:
                        return ((Int32)value).ToString(format);
                    case TypeCode.Int64:
                        return ((Int64)value).ToString(format);
                    case TypeCode.Decimal:
                        return ((Decimal)value).ToString(format);
                    case TypeCode.Double:
                        return ((Double)value).ToString(format);
                    case TypeCode.Single:
                        return ((Single)value).ToString(format);

                    default:
                        return "";
                }
            }

            return (string)value;
        }

        public static bool IsNumeric(object obj)
        {
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}