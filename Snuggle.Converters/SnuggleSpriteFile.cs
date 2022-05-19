using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Options;
using Path = System.IO.Path;
using Vector2 = Snuggle.Core.Models.Objects.Math.Vector2;

namespace Snuggle.Converters;

public static class SnuggleSpriteFile {
    private static ConcurrentDictionary<(long, string), (ReadOnlyMemory<byte>, Size, TextureFormat)> CachedData { get; set; } = new();

    // Perfare's Asset Studio - SpriteHelper.cs.
    public static (Memory<byte> RGBA, Size Size, TextureFormat baseFormat) ConvertSprite(Sprite sprite, ObjectDeserializationOptions options, bool useDirectXTex, bool useTextureDecoder) {
        var (memory, size, format) = CachedData.GetOrAdd(
            sprite.GetCompositeId(),
            static (_, arg) => {
                var (sprite, options, useDirectXTex, useTextureDecoder) = arg;
                if (sprite.SpriteAtlas.Value?.RenderDataMap != null && sprite.SpriteAtlas.Value.RenderDataMap.TryGetValue(sprite.RenderDataKey, out var spriteAtlasData) && spriteAtlasData.Texture.Value is not null) {
                    spriteAtlasData.Texture.Value.Deserialize(options);
                    using var image = ConvertSprite(sprite, spriteAtlasData.Texture.Value!, spriteAtlasData.TextureRect, spriteAtlasData.TextureRectOffset, spriteAtlasData.Settings, useDirectXTex, useTextureDecoder);
                    if (image != null) {
                        return (image.ToRGBA(), image.Size(), spriteAtlasData.Texture.Value.TextureFormat);
                    }
                }

                if (sprite.RenderData.Texture.Value != null) {
                    sprite.RenderData.Texture.Value.Deserialize(options);
                    using var image = ConvertSprite(sprite, sprite.RenderData.Texture.Value, sprite.RenderData.TextureRect, sprite.RenderData.TextureRectOffset, sprite.RenderData.Settings, useDirectXTex, useTextureDecoder);
                    if (image != null) {
                        return (image.ToRGBA(), image.Size(), sprite.RenderData.Texture.Value.TextureFormat);
                    }
                }

                return (ReadOnlyMemory<byte>.Empty, Size.Empty, TextureFormat.None);
            },
            (sprite, options, useDirectXTex, useTextureDecoder));

        var newMemory = new Memory<byte>(new byte[memory.Length]);
        memory.CopyTo(newMemory);
        return (newMemory, size, format);
    }

    public static void ClearMemory() {
        CachedData.Clear();
        CachedData = new ConcurrentDictionary<(long, string), (ReadOnlyMemory<byte>, Size, TextureFormat)>();
        Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
    }

    private static Image<Rgba32>? ConvertSprite(Sprite sprite, ITexture texture, Rect textureRect, Vector2 textureOffset, SpriteSettings settings, bool useDirectXTex, bool useTextureDecoder) {
        using var originalImage = SnuggleTextureFile.ConvertImage(texture, false, useDirectXTex, useTextureDecoder);
        var rectX = (int) Math.Floor(textureRect.X);
        var rectY = (int) Math.Floor(textureRect.Y);
        var rectRight = (int) Math.Ceiling(textureRect.X + textureRect.W);
        var rectBottom = (int) Math.Ceiling(textureRect.Y + textureRect.H);
        rectRight = Math.Min(rectRight, texture.Width);
        rectBottom = Math.Min(rectBottom, texture.Height);
        var rect = new Rectangle(rectX, rectY, rectRight - rectX, rectBottom - rectY);
        var spriteImage = originalImage.Clone(x => x.Crop(rect));
        if (settings.Packed == 1) {
            switch (settings.Rotation) {
                case SpritePackingRotation.FlipHorizontal:
                    spriteImage.Mutate(x => x.Flip(FlipMode.Horizontal));
                    break;
                case SpritePackingRotation.FlipVertical:
                    spriteImage.Mutate(x => x.Flip(FlipMode.Vertical));
                    break;
                case SpritePackingRotation.Rotate180:
                    spriteImage.Mutate(x => x.Rotate(180));
                    break;
                case SpritePackingRotation.Rotate90:
                    spriteImage.Mutate(x => x.Rotate(270));
                    break;
                case SpritePackingRotation.None:
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(settings.Rotation));
            }
        }

        if (settings.Mode == SpritePackingMode.Tight) {
            var triangles = GetTriangles(sprite.RenderData);
            var polygons = triangles.Select(x => (IPath) new Polygon(new LinearLineSegment(x.Select(y => new PointF(y.X, y.Y)).ToArray()))).ToArray();
            IPathCollection path = new PathCollection(polygons);
            var matrix = Matrix3x2.CreateScale(sprite.PixelsToUnits);
            var (oX, oY) = textureOffset;
            matrix *= Matrix3x2.CreateTranslation(sprite.Rect.W * sprite.Pivot.X - oX, sprite.Rect.H * sprite.Pivot.Y - oY);
            path = path.Transform(matrix);
            var options = new DrawingOptions { GraphicsOptions = new GraphicsOptions { AlphaCompositionMode = PixelAlphaCompositionMode.DestOut } };
            var rectP = new RectangularPolygon(0, 0, rect.Width, rect.Height);
            spriteImage.Mutate(x => x.Fill(options, Color.Red, rectP.Clip(path)));
        }

        spriteImage.Mutate(x => x.Flip(FlipMode.Vertical));
        return spriteImage;
    }

    private static IEnumerable<Vector2[]> GetTriangles(SpriteRenderData renderData) {
        if (renderData.Vertices is { IsEmpty: false }) { //5.6 down
            var vertices = renderData.Vertices.Value.Span;
            var indices = renderData.Indices!.Value.Span;
            var triangleCount = indices.Length / 3;
            var triangles = new Vector2[triangleCount][];
            for (var i = 0; i < triangleCount; i++) {
                var (aX, aY, _) = vertices[indices[i * 3]];
                var (bX, bY, _) = vertices[indices[i * 3 + 1]];
                var (cX, cY, _) = vertices[indices[i * 3 + 2]];
                var triangle = new[] { new Vector2(aX, aY), new Vector2(bX, bY), new Vector2(cX, cY) };
                triangles[i] = triangle;
            }

            return triangles;
        } else {
            var vertexData = renderData.VertexData;
            var buffers = MeshConverter.GetVBO(vertexData.Data!.Value, vertexData.VertexCount, vertexData.Channels, out var strides);

            var triangles = new Vector2[renderData.Submeshes.Sum(x => x.IndexCount / 3)][];
            var triangleBase = 0;
            var indices = MemoryMarshal.Cast<byte, ushort>(renderData.Indices!.Value.Span);
            foreach (var submesh in renderData.Submeshes) {
                var channel = vertexData.Channels[VertexChannel.Vertex];
                var offset = channel.Offset + submesh.FirstVertex * strides[channel.Stream];
                var buffer = buffers[channel.Stream].Span;

                var vertices = new Vector2[submesh.VertexCount];
                for (var v = 0; v < submesh.VertexCount; v++) {
                    var value = channel.Unpack(buffer[offset..]);
                    offset += strides[channel.Stream];
                    var floatValues = value.Select(Convert.ToSingle).Concat(new float[4]).Take(2).ToArray();
                    vertices[v] = new Vector2(floatValues[0], floatValues[1]);
                }

                var submeshIndices = indices.Slice((int) submesh.FirstByte / 2, (int) submesh.IndexCount).ToArray();

                for (var i = 0; i < submesh.IndexCount / 3; ++i) {
                    triangles[triangleBase + i] = new[] { vertices[submeshIndices[i * 3]], vertices[submeshIndices[i * 3 + 1]], vertices[submeshIndices[i * 3 + 2]] };
                }

                triangleBase += (int) submesh.IndexCount / 3;
            }

            return triangles.ToArray();
        }
    }

    public static string Save(Sprite sprite, string path, ObjectDeserializationOptions options, SnuggleExportOptions exportOptions) {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        path = Path.ChangeExtension(path, ".png");
        var (data, (width, height), _) = ConvertSprite(sprite, options, exportOptions.UseDirectTex, exportOptions.UseTextureDecoder);
        var image = Image.WrapMemory<Rgba32>(data, width, height);
        image.SaveAsPng(path);

        return path;
    }
}
