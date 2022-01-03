using System;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Core.IO;

[PublicAPI]
public class FileStreamHandler : IFileHandler {
    public static Lazy<FileStreamHandler> Instance { get; } = new();

    public Stream OpenFile(object tag) {
        var path = tag switch {
            FileInfo fi => fi.FullName,
            string str => str,
            _ => throw new NotSupportedException($"{tag.GetType().FullName} is not supported"),
        };

        return File.OpenRead(path);
    }

    public Stream OpenSubFile(object parent, object tag, SnuggleCoreOptions options) {
        var path = GetSubFilePath(parent, tag, options);
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        return File.Open(path, FileMode.OpenOrCreate);
    }

    public bool FileCreated(object parent, object tag, SnuggleCoreOptions options) => File.Exists(GetSubFilePath(parent, tag, options));

    public object GetTag(object baseTag, object parent) => baseTag;

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    private static string GetSubFilePath(object parent, object tag, SnuggleCoreOptions options) {
        var name = tag switch {
            FileInfo fi => fi.Name,
            string str => Path.GetFileName(str),
            _ => throw new NotSupportedException($"{tag.GetType().FullName} is not supported"),
        };
        var parentName = parent switch {
            FileInfo fi => fi.FullName,
            string str => str,
            _ => throw new NotSupportedException($"{tag.GetType().FullName} is not supported"),
        };
        var root = options.CacheDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./";
        if (!Path.IsPathRooted(options.CacheDirectory)) {
            root = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, root);
        }

        var hash = CRC.GetDigest(Path.GetDirectoryName(parentName)!).ToString("x8");
        return Path.Combine(root, hash, name);
    }
}
