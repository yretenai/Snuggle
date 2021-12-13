namespace Snuggle.Converters.DXGI; 

public struct DXT10Header {
    public int Format { get; set; }
    public DXT10ResourceDimension Dimension { get; set; }
    public int Misc { get; set; } // cubemap = 0x4
    public int Size { get; set; } // number of maps, 1
    public int Misc2 { get; set; } // alpha mode, 0
}
