namespace Snuggle.Converters.DXGI;

public unsafe struct DDSImageHeader {
    public int Magic { get; set; }
    public int Size { get; set; }
    public int Flags { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public int LinearSize { get; set; }
    public int Depth { get; set; }
    public int MipmapCount { get; set; }
    public fixed int Reserved1[11];
    public DDSPixelFormat Format { get; set; }
    public int Caps1 { get; set; }
    public int Caps2 { get; set; }
    public int Caps3 { get; set; }
    public int Caps4 { get; set; }
    public int Reserved2 { get; set; }
    public int DXGIFormat { get; set; }
    public DDSResourceDimension Dimension { get; set; }
    public int Misc { get; set; } // cube = 0x4
    public int MapSize { get; set; } // number of maps, 1
    public int Misc2 { get; set; } // alpha mode, 0
}
