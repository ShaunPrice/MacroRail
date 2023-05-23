/*
 * Copyright (c) 2023 Shaun Price
This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail).
MacroRail is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published
by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
MacroRail is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of 
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with Foobar. If not, see <https://www.gnu.org/licenses/>.
*/
namespace MacroRail
{
    internal class Project
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public Camera camera { get; set; }
        public MacroRail macroRail { get; set; }

        public class Camera
        {
            public string Compression { get; set; }
            public string ShutterSpeed { get; set; }
            public string Apeture { get; set; }
            public bool AutoISO { get; set; }
            public string SensitivityISO { get; set; }
            public string FlashSyncTime { get; set; }
            public string FlashSlowLimit { get; set; }
            public bool ExposureDelay { get; set; }
            public bool EnableCopyright { get; set; }
            public string ArtistsName { get; set; }
            public string Copyright { get; set; }

            public Camera() { }
        }

        public class MacroRail
        {
            public uint JogSpeed { get; set; }
            public string ProjectDirectory { get; set; }
            public bool NoShooting { get; set; }
            public uint DelayBeforeShooting { get; set; }
            public int StepCount { get; set; }
            public double StepDistance { get; set; }
            public MacroRail() { }
        }
        public Project() { }
    }
}
