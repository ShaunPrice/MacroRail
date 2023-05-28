using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroRail
{
    internal class Rail
    {
        public uint pitch = 2;
        public uint thread_starts = 4;
        public uint steps_rev = 200;
        public uint microsteps = 32;
        public uint gear_ratio = 1;
        public uint steps_mm = 800; // Set-up in tools settings
        public uint max_speed = 100000000;
        public uint jog_speed = 20000000;
        public Rail() { }
    }
}
