using System;
using System.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Bundle;
using Snuggle.Core.Options;

namespace Snuggle.Core.Interfaces;

public interface IFileHandler : IDisposable {
    public Stream OpenFile(object tag);
    public Stream OpenSubFile(object parent, object tag, SnuggleCoreOptions options);
    public bool FileCreated(object parent, object tag, SnuggleCoreOptions options);
    public object GetTag(object baseTag, object parent);

    public static string? UnpackTagToString(object? tag) {
        while (true) {
            switch (tag) {
                case FileInfo fi:
                    return fi.Name;
                case string str:
                    return str;
                case OffsetInfo mmi: {
                    tag = mmi.Tag;
                    continue;
                }
                case UnityBundleBlock ubb:
                    return ubb.Path;
                case null:
                    return null;
                default:
                    return tag.ToString();
            }
        }
    }

    public static string? UnpackTagToName(object? tag) {
        var str = UnpackTagToString(tag);
        if (string.IsNullOrEmpty(str)) {
            return null;
        }

        return Path.GetExtension(str) == ".split0" ? Path.GetFileNameWithoutExtension(str) : Path.GetFileName(str);
    }

    public static string? UnpackTagToNameWithoutExtension(object? tag) {
        var str = UnpackTagToString(tag);
        return string.IsNullOrEmpty(str) ? null : Path.GetFileNameWithoutExtension(str);
    }
}
