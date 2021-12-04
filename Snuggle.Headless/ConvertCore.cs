using Snuggle.Converters;
using Snuggle.Core;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ClearMemory(AssetCollection assetCollection) {
        SnuggleTextureFile.ClearMemory();
        foreach (var (_, file) in assetCollection.Files) {
            file.Free();
        }

        AssetCollection.Collect();
    }
}
