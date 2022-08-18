using System.IO;
using Snuggle.Core.Interfaces;

namespace Snuggle.Core.IO;

public class SplitFileStreamHandler : FileStreamHandler {
    public override Stream OpenFile(object tag) {
        var path = IFileHandler.UnpackTagToString(tag);

        return new SplitFileStream(path!);
    }
}
