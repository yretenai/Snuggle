using System;
using System.IO;
using JetBrains.Annotations;
using Snuggle.Core.Options;

namespace Snuggle.Core.Interfaces;

[PublicAPI]
public interface IFileHandler : IDisposable {
    public Stream OpenFile(object tag);
    public Stream OpenSubFile(object parent, object tag, SnuggleCoreOptions options);
    public bool FileCreated(object parent, object tag, SnuggleCoreOptions options);
    public object GetTag(object baseTag, object parent);
}
