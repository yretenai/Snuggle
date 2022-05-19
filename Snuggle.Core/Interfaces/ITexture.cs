using System;
using Snuggle.Core.Models.Objects.Graphics;

namespace Snuggle.Core.Interfaces;

public interface ITexture : ISerializedResource, ISerializedObject {
    public int Width { get; set; }
    public int Height { get; set; }
    public int Depth { get; set; }
    public TextureFormat TextureFormat { get; set; }
    public int MipCount { get; set; }
    public Memory<byte>? TextureData { get; set; }
    public TextureUsageMode UsageMode { get; set; }
}
