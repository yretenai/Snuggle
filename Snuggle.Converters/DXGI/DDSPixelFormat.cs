namespace Snuggle.Converters.DXGI;

public struct DDSPixelFormat {
    public int Size { get; set; }
    public int Flags { get; set; }
    public int FourCC { get; set; }
    public int BitCount { get; set; }
    public uint RedMask { get; set; }
    public uint GreenMask { get; set; }
    public uint BlueMask { get; set; }
    public uint AlphaMask { get; set; }
}
