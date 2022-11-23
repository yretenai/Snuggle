using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Serilog;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Bundle;
using Snuggle.Core.Options;

namespace Snuggle.Core;

public class Bundle : IAssetBundle {
    private static Dictionary<string, (UnityFormat, UnityGame)>? _NonStandardLookup;

    public Bundle(Stream dataStream, object tag, IFileHandler fileHandler, SnuggleCoreOptions options, bool leaveOpen = false) {
        try {
            using var reader = new BiEndianBinaryReader(dataStream, true, leaveOpen);

            Options = options;

            Header = UnityBundle.FromReader(reader, options);
            Container = Header.Format switch {
                UnityFormat.FS => UnityFS.FromReader(reader, Header, options),
                UnityFormat.Archive => throw new NotImplementedException(),
                UnityFormat.Web => UnityRaw.FromReader(reader, Header, options),
                UnityFormat.Raw => UnityRaw.FromReader(reader, Header, options),
                _ => throw new NotSupportedException($"Unity Bundle format {Header.Signature} is not supported"),
            };

            DataStart = dataStream.Position;
            Handler = fileHandler;
            Tag = fileHandler.GetTag(tag, this);

            if (Options.CacheData || Options.CacheDataIfLZMA && Container.BlockInfos.Any(x => (UnityCompressionType) (x.Flags & UnityBundleBlockInfoFlags.CompressionMask) == UnityCompressionType.LZMA)) {
                SaveContainers(reader);
            }
        } finally {
            if (!leaveOpen) {
                dataStream.Close();
            }
        }
    }

    public UnityBundle Header { get; init; }
    public UnityContainer Container { get; init; }
    public long DataStart { get; set; }

    public static IReadOnlyDictionary<string, (UnityFormat Format, UnityGame Game)> NonStandardLookup {
        get {
            if (_NonStandardLookup == null) {
                _NonStandardLookup = new Dictionary<string, (UnityFormat, UnityGame)>();
                var bundleIdsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "bundleIds.csv");
                if (File.Exists(bundleIdsPath)) {
                    foreach (var line in File.ReadLines(bundleIdsPath)) {
                        var trimmed = (line.Contains(';') ? line[..line.IndexOf(';')] : line).Trim();
                        // minimum length is: 1 for each value and 1 for each separator.
                        if (trimmed.Length < 5) {
                            continue;
                        }

                        var parts = trimmed.Split(',', 3, StringSplitOptions.TrimEntries);
                        if (parts.Any(string.IsNullOrEmpty)) {
                            continue;
                        }

                        if (!Enum.TryParse<UnityFormat>(parts[1], out var type)) {
                            type = (UnityFormat) parts[1][0];
                        }

                        if (!Enum.TryParse<UnityGame>(parts[2], out var game)) {
                            game = UnityGame.Default;
                        }

                        _NonStandardLookup[parts[1]] = (type, game);
                    }
                }
            }

            return new ReadOnlyDictionary<string, (UnityFormat, UnityGame)>(_NonStandardLookup);
        }
    }

    public long Length => Container.Length;
    public SnuggleCoreOptions Options { get; init; }

    public void Dispose() {
        Handler.Dispose();
        GC.SuppressFinalize(this);
    }

    public object Tag { get; set; }
    public IFileHandler Handler { get; set; }

    public UnityVersion Version => Header.Version ?? UnityVersion.Default;

    public Stream OpenFile(string path) {
        var block = Container.Blocks.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));
        return block == null ? Stream.Null : OpenFile(block);
    }

    public Stream OpenFile(UnityBundleBlock block) {
        if (Handler.SupportsCreation && Handler.FileCreated(Tag, block.Path, Options)) {
            return Handler.OpenSubFile(Tag, block.Path, Options);
        }

        var reader = new BiEndianBinaryReader(Handler.OpenFile(Tag), true);
        reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);
        var data = Container.OpenFile(block, reader);
        reader.Dispose();
        data.Seek(0, SeekOrigin.Begin);
        return data;
    }

    public IEnumerable<UnityBundleBlock> GetBlocks() => Container.Blocks;

    public bool ToStream(UnityBundleBlock[] blocks, Stream dataStream, BundleSerializationOptions serializationOptions, Stream outputStream) {
        try {
            using var writer = new BiEndianBinaryWriter(outputStream, true, true);
            var start = outputStream.Position;
            Header.ToWriter(writer, Options);
            if (serializationOptions.TargetFormatVersion < 0) {
                serializationOptions = serializationOptions.MutateWithBundle(this);
            }

            Container.ToWriter(writer, Header, Options, blocks, dataStream, serializationOptions, start);
            outputStream.Seek(0, SeekOrigin.Begin);
            return true;
        } catch (Exception e) {
            Log.Error(e, "Failed to serialize bundle");
            return false;
        }
    }

    public static IAssetBundle[] OpenBundleSequence(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, int align = 1, bool leaveOpen = false) {
        var bundles = new List<IAssetBundle>();
        while (dataStream.Position < dataStream.Length) {
            var start = dataStream.Position;
            if (!IsBundleFile(dataStream)) {
                break;
            }

            var bundle = new Bundle(new OffsetStream(dataStream), new OffsetInfo(tag, start, 0), handler, options, true);
            if (bundle.Length == -1 || start + bundle.Length > dataStream.Length) {
                // skip partial bundles
                break;
            }

            bundles.Add(bundle);
            dataStream.Seek(start + bundle.Container.Length, SeekOrigin.Begin);

            if (align > 1) {
                if (dataStream.Position % align == 0) {
                    continue;
                }

                var delta = (int) (align - dataStream.Position % align);
                dataStream.Seek(delta, SeekOrigin.Current);
            }
        }

        if (!leaveOpen) {
            dataStream.Dispose();
        }

        return bundles.ToArray();
    }

    public void SaveContainers(BiEndianBinaryReader reader) {
        if (!Handler.SupportsCreation) {
            return;
        }

        Stream? data = null;
        foreach (var block in Container.Blocks) {
            if (Handler.FileCreated(Tag, block.Path, Options)) {
                continue;
            }

            Log.Information("Caching {Path}", block.Path);

            using var stream = Handler.OpenSubFile(Tag, block.Path, Options);
            data ??= Container.OpenFile(new UnityBundleBlock(0, Container.BlockInfos.Select(x => x.Size).Sum(), 0, string.Empty), reader);
            using var offset = new OffsetStream(data, block.Offset, block.Size, true);
            offset.CopyTo(stream);
        }

        data?.Dispose();
    }

    public static bool IsBundleFile(Stream stream) {
        using var reader = new BiEndianBinaryReader(stream, true, true);
        return IsBundleFile(reader);
    }

    private static bool IsBundleFile(BiEndianBinaryReader reader) {
        var pos = reader.BaseStream.Position;
        try {
            var prefix = reader.ReadNullString(0x10);
            if (prefix.Contains("UnityFS") || prefix.Contains("UnityWeb") || prefix.Contains("UnityRaw") || prefix.Contains("UnityArchive")) {
                return true;
            }

            return NonStandardLookup.ContainsKey(prefix);
        } catch {
            return false;
        } finally {
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
        }
    }
}
