using System;
using System.Runtime.InteropServices;


namespace MacroRail
{
    internal static class LibRaw
    {
        [DllImport("libraw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libraw_init(int options);

        // Define other LibRaw functions here...

        [DllImport("libraw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libraw_unpack(IntPtr libraw_data);

        [DllImport("libraw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libraw_dcraw_process(IntPtr libraw_data);

        [DllImport("libraw.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libraw_dcraw_ppm_tiff_writer(IntPtr libraw_data, string outputFileName);
    }
}

