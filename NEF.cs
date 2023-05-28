using System.Buffers.Binary;

namespace MacroRail
{
    public struct TIFF_Header
    {
        public ushort byte_order;
        public ushort fourty_two;
        public uint ifd_start;
    }

    public struct IFD
    {
        public ushort tag;
        public ushort type;
        public uint count;

        public uint value_offset;
    }

    public enum IFD_TAG : uint
    {
        NewSubFileType = 0x00FE,
        ImageWidth = 0x100,
        ImageLength = 0x101,
        BitsPerSample = 0x102,
        Compression = 0x103,
        PhotometricInterpretation = 0x106,
        ImageDescription = 0x10E,
        Make = 0x10F,
        Model = 0x110,
        StripOffsets = 0x111,
        Orientation = 0x112,
        SamplesPerPixel = 0x115,
        RowsPerStrip = 0x116,
        StripByteCounts = 0x117,
        XResolution = 0x11A,
        YResolution = 0x11B,
        PlanarConfiguration = 0x11C,
        ResolutionUnit = 0x128,
        Software = 0x131,
        DateTime = 0x132,
        Artist = 0x13B,
        SubIFDs = 0x14A,
        ReferenceBlackWhite = 0x214,
        XMP = 0x2BC,
        Copyright = 0x8298,
        ExifIFDPointer = 0x8769,
        GPSInfoIFDPointer = 0x8825,
        DateTimeOriginal = 0x9003,
        TIFF_EPStandardID = 0x9216
    }

    public enum IFD_TYPE : ushort
    {
        Byte = 0x1,
        ASCII = 0x2,
        SHORT = 0x3,
        LONG = 0x4,
        RATIONAL = 0x5,
        UNDEFINED = 0x7,
        SSHORT = 0x8,
        SLONG = 0x9,
        SRATIONAL = 0x0A
    }

    public enum SUB_IFD_1_TAG : ushort
    {
        NewSubFileType = 0x00FE,
        Compression = 0x0103,
        XResolution = 0x011A,
        YResolution = 0x011B,
        ResolutionUnit = 0x0128,
        JPEGInterchangeFormat = 0x0201,
        JPEGInterchangeFormatLength = 0x0202,
        YCbCrPositioning = 0x0213
    }

    public enum SUB_IFD_2_TAG : ushort
    {
        NewSubFileType = 0x00FE,
        ImageWidth = 0x0100,
        ImageLength = 0x0101,
        BitsPerSample = 0x0102,
        Compression = 0x0103,
        PhotometricInterpretation = 0x0106,
        StripOffsets = 0x0111,
        Orientation = 0x0112,
        SamplesPerPixel = 0x0115,
        RowsPerStrip = 0x0116,
        StripByteCounts = 0x0117,
        XResolution = 0x011A,
        YResolution = 0x011B,
        PlanarConfiguration = 0x011C,
        ResolutionUnit = 0x0128,
        CFARepeatPatternDim = 0x828D,
        CFAPattern = 0x828E,
        SensingMethod = 0x9217,
        PrivateDataTag = 0xC7D5,
        ValidatePrivateDataTag = 0xC7D6
    }

    public enum SUB_IFD_3_TAG : ushort
    {
        NewSubFileType = 0x00FE,
        Compression = 0x0103,
        XResolution = 0x011A,
        YResolution = 0x011B,
        ResolutionUnit = 0x0128,
        JPEGInterchangeFormat = 0x0201,
        JPEGInterchangeFormatLength = 0x0202,
        YCbCrPositioning = 0x0213
    }

    public enum PhotometricInterpretation : ushort
    {
        WhiteIsZero = 0x0000,
        BlackIsZero = 0x0001,
        RGB = 0x0002,
        RGB_Palette = 0x0003,
        Transparency_Mask = 0x0004,
        CMYK = 0x0005,
        YCbCr = 0x0006,
        CIELab = 0x0008,
        CFA = 0x8023,
        Vendor_Unique = 0x8000 // Values >= 0x8000
    }

    public enum Compression : ushort
    {
        Uncompressed = 0x0001,
        RAW = 0x8799,
        JPEG = 0x0006
    }

    public class NEF_Image
    {
        public NEF_Image()
        {
            // Initialise
            m_ifd = new IFD[1];
            m_sub1 = new IFD[1];
            m_sub3 = new IFD[1];
        }

        TIFF_Header m_tiff_header;
        IFD[] m_ifd;
        uint m_ifd_count = 0;


        IFD[] m_sub1;
        IFD[] m_sub3;

        bool m_big_endian = false;
        int pointer = 0;

        byte[]? m_jpeg_preview;
        int m_jpeg_preview_start = 0;
        int m_jpeg_preview_length = 0;

        byte[]? m_jpeg_monitor;
        int m_jpeg_monitor_start = 0;
        int m_jpeg_monitor_length = 0;

        public TIFF_Header TIFF_Header
        {
            get
            {
                return m_tiff_header;
            }
            set
            {
                m_tiff_header = value;
            }
        }

        public IFD[] IFD
        {
            get
            {
                return m_ifd;
            }
            set
            {
                m_ifd = value;
            }
        }

        public void Load(byte[] value)
        {
            try
            {
                Memory<byte> bytes = value;

                m_tiff_header.byte_order = BitConverter.ToUInt16(bytes.Slice(0, 2).ToArray());

                // Is little endian?
                if (m_tiff_header.byte_order == 0x4D4D)
                {
                    m_big_endian = true;
                }
                // Is Big Endian?
                else if (m_tiff_header.byte_order == 0x4949)
                {
                    m_big_endian = false;
                }
                // Don't know. Assume little endian.
                else
                {
                    m_big_endian = false;
                }

                m_tiff_header.fourty_two = ConvertEndian(BitConverter.ToUInt16(bytes.Slice(2, 2).ToArray()));
                m_tiff_header.ifd_start = ConvertEndian(BitConverter.ToUInt32(bytes.Slice(4, 4).ToArray()));

                // Get the number of IFD entries
                Memory<byte> mem = bytes.Slice((int)m_tiff_header.ifd_start, 2);

                ushort start = BitConverter.ToUInt16(mem.ToArray());
                m_ifd_count = ConvertEndian(start);

                // Load each IFD looking for "Sub IFD" (D100 & D2H) or Sub IFD1 (all other cameras)
                // These IFD's contain the JPEG preview data

                m_ifd = new IFD[m_ifd_count];

                pointer = (int)m_tiff_header.ifd_start + 2;

                for (int x = 0; x < m_ifd_count; x++)
                {
                    m_ifd[x].tag = ConvertEndian(BitConverter.ToUInt16(bytes.Slice(pointer, 2).ToArray()));
                    pointer += 2;
                    m_ifd[x].type = ConvertEndian(BitConverter.ToUInt16(bytes.Slice(pointer, 2).ToArray()));
                    pointer += 2;
                    m_ifd[x].count = ConvertEndian(BitConverter.ToUInt32(bytes.Slice(pointer, 4).ToArray()));
                    pointer += 4;
                    m_ifd[x].value_offset = ConvertEndian(BitConverter.ToUInt32(bytes.Slice(pointer, 4).ToArray()));
                    pointer += 4;

                    IFD_TAG tag = (IFD_TAG)m_ifd[x].tag;
                    IFD_TYPE type = (IFD_TYPE)m_ifd[x].type;
                    int offset = (int)m_ifd[x].value_offset;
                    int sub_count = (int)m_ifd[x].count;

                    // If the count is 2 we have Sud IFD 1 & 2 and if 3 we also have Sub IFD 3
                    // Sub 1 is the preview and Sub 3 is the monitor data.

                    if (tag == IFD_TAG.SubIFDs && (sub_count == 2 || sub_count == 3))
                    {
                        int sub_tag_pointer = (int)ConvertEndian(BitConverter.ToUInt32(bytes.Slice(offset, 4).ToArray()));

                        // Get the count of sub ifd fields
                        int m_sub_tag_count = (int)ConvertEndian(BitConverter.ToUInt16(bytes.Slice(sub_tag_pointer, 2).ToArray()));
                        m_sub1 = new IFD[m_sub_tag_count];

                        // Move to the first field
                        sub_tag_pointer += 2;

                        for (int y = 0; y < m_sub_tag_count; y++)
                        {
                            m_sub1[y].tag = ConvertEndian(BitConverter.ToUInt16(bytes.Slice(sub_tag_pointer, 2).ToArray()));
                            sub_tag_pointer += 2;
                            m_sub1[y].type = ConvertEndian(BitConverter.ToUInt16(bytes.Slice(sub_tag_pointer, 2).ToArray()));
                            sub_tag_pointer += 2;
                            m_sub1[y].count = ConvertEndian(BitConverter.ToUInt32(bytes.Slice(sub_tag_pointer, 4).ToArray()));
                            sub_tag_pointer += 4;
                            m_sub1[y].value_offset = ConvertEndian(BitConverter.ToUInt32(bytes.Slice(sub_tag_pointer, 4).ToArray()));
                            sub_tag_pointer += 4;

                            if ((SUB_IFD_1_TAG)m_sub1[y].tag == SUB_IFD_1_TAG.JPEGInterchangeFormat)
                            {
                                m_jpeg_preview_start = (int)m_sub1[y].value_offset;
                            }
                            else if ((SUB_IFD_1_TAG)m_sub1[y].tag == SUB_IFD_1_TAG.JPEGInterchangeFormatLength)
                            {
                                m_jpeg_preview_length = (int)m_sub1[y].value_offset;
                            }
                        }
                    }
                    // If we have Sub IFD 3 we'll also retrieve the monitor jpeg
                    if (tag == IFD_TAG.SubIFDs && sub_count == 3)
                    {
                        int sub_tag_pointer = (int)ConvertEndian(BitConverter.ToUInt32(bytes.Slice(offset + 8, 4).ToArray()));

                        // Get the count of sub ifd fields
                        int m_sub_tag_count = (int)ConvertEndian(BitConverter.ToUInt16(bytes.Slice(sub_tag_pointer, 2).ToArray()));
                        m_sub3 = new IFD[m_sub_tag_count];

                        // Move to the first field
                        sub_tag_pointer += 2;

                        for (int y = 0; y < m_sub_tag_count; y++)
                        {
                            m_sub3[y].tag = ConvertEndian(BitConverter.ToUInt16(bytes.Slice(sub_tag_pointer, 2).ToArray()));
                            sub_tag_pointer += 2;
                            m_sub3[y].type = ConvertEndian(BitConverter.ToUInt16(bytes.Slice(sub_tag_pointer, 2).ToArray()));
                            sub_tag_pointer += 2;
                            m_sub3[y].count = ConvertEndian(BitConverter.ToUInt32(bytes.Slice(sub_tag_pointer, 4).ToArray()));
                            sub_tag_pointer += 4;
                            m_sub3[y].value_offset = ConvertEndian(BitConverter.ToUInt32(bytes.Slice(sub_tag_pointer, 4).ToArray()));
                            sub_tag_pointer += 4;

                            if ((SUB_IFD_3_TAG)m_sub3[y].tag == SUB_IFD_3_TAG.JPEGInterchangeFormat)
                            {
                                m_jpeg_monitor_start = (int)m_sub3[y].value_offset;
                            }
                            else if ((SUB_IFD_1_TAG)m_sub3[y].tag == SUB_IFD_1_TAG.JPEGInterchangeFormatLength)
                            {
                                m_jpeg_monitor_length = (int)m_sub3[y].value_offset;
                            }
                        }
                    }
                }
                // Retrieve the preview image if it's available
                if (m_jpeg_preview_start > 0 && m_jpeg_preview_length > 0)
                {
                    m_jpeg_preview = bytes.Slice(m_jpeg_preview_start, m_jpeg_preview_length).ToArray();
                }

                // Retrieve the monitor image if it's available
                if (m_jpeg_monitor_start > 0 && m_jpeg_monitor_length > 0)
                {
                    m_jpeg_monitor = bytes.Slice(m_jpeg_monitor_start, m_jpeg_monitor_length).ToArray();
                }
            }
            catch { }
        }

        public uint IFD_start
        {
            get
            {
                return ConvertEndian(m_tiff_header.ifd_start);
            }
        }

        public byte[]? JpegPreview
        {
            get { return m_jpeg_preview; }
        }

        public byte[]? JpegMonitor
        {
            get { return m_jpeg_monitor; }
        }

        public uint ConvertEndian(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return ConvertEndian<uint>(bytes);
        }

        public ushort ConvertEndian(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return ConvertEndian<ushort>(bytes);
        }

        public T ConvertEndian<T>(byte[] value)
        {
            // We'll need to know what T is to be able to correctly interpret the value.
            if (typeof(T) == typeof(ushort))
            {
                if (value.Length < 2)
                {
                    throw new ArgumentException("Value must be at least 2 bytes for ushort conversion.", nameof(value));
                }

                ushort result = m_big_endian ? BinaryPrimitives.ReadUInt16BigEndian(value) : BinaryPrimitives.ReadUInt16LittleEndian(value);
                return (T)Convert.ChangeType(result, typeof(T));
            }
            else if (typeof(T) == typeof(uint))
            {
                if (value.Length < 4)
                {
                    throw new ArgumentException("Value must be at least 4 bytes for uint conversion.", nameof(value));
                }

                uint result = m_big_endian ? BinaryPrimitives.ReadUInt32BigEndian(value) : BinaryPrimitives.ReadUInt32LittleEndian(value);
                return (T)Convert.ChangeType(result, typeof(T));
            }
            else
            {
                throw new ArgumentException($"Cannot convert to type {typeof(T)}.", nameof(T));
            }
        }
    }
}
