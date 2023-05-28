using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroRail
{

    internal enum shoot_status : int
    {
        Idle = 0,       // The project is not running
        Start = 1,      // The project sequence has started
        Move = 2,       // Move to the shot position
        Moving = 3,     // Wait until camera moved
        Delay = 4,      // Make sure the flashes have recharged & the platform has stopped shaking from the move
        Delaying = 5,   // Wait until delay completed
        Shoot = 6,      // Take the shot
        Shooting = 7,   // Wait until shoot completed
        ManualShoot = 8,// Take the shot manually
        Downloaded = 9, // Download the images
        Paused = 10,    // Paused by the user
        End = 11        // The project sequence has ended
    }

    internal class Shoot
    {
        public Shoot()
        {
            tic = new Tic();
        }

        public Tic tic;
        public string tic_name = "";
        public bool tic_connected = false;
        public bool moving = false;
        public bool homing = false;
        public bool decelerating;
        public int current_position = 0;

        public int start = 0;

        public double dof = 0;

        public shoot_status sequence_status = shoot_status.Idle;
        public shoot_status pause_previous_state;

        public string name = "shoot1";
        public string version = "1.0.0";

        public int current_shot = 0;
        public int delay_counter = 0;

        public bool downloaded_nef = false;
        public bool downloaded_jpeg = false;
        public bool downloaded_tiff = false;

        // Logging
        public StreamWriter? log;
        public bool log_open;
    }
}
