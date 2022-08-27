using System;
using System.IO;
using System.Reflection;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Core.IO;

public class FileStreamHandler : IFileHandler {
    public virtual Stream OpenFile(object tag) {
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
    public bool SupportsCreation => true;

    public object GetTag(object baseTag, object parent) => baseTag;

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    private static string GetSubFilePath(object parent, object tag, SnuggleCoreOptions options) {
        var name = Path.GetFileName(IFileHandler.UnpackTagToString(tag));
        if (name == null) {
            throw new NullReferenceException();
        }

        var parentName = IFileHandler.UnpackTagToString(parent);
        if (parentName == null) {
            throw new NullReferenceException();
        }

        var root = options.CacheDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./";
        if (!Path.IsPathRooted(options.CacheDirectory)) {
            root = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, root);
        }

        var hash = CRC.GetDigest(Path.GetDirectoryName(parentName)!).ToString("x8");
        return Path.Combine(root, hash, name);
    }
}
