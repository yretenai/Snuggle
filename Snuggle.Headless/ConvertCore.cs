using Snuggle.Converters;
using Snuggle.Core;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ClearMemory(AssetCollection assetCollection) {
        foreach (var (_, file) in assetCollection.Files) {
            file.Free();
        }

        assetCollection.ClearCaches();

        SnuggleTextureFile.ClearMemory();
        SnuggleSpriteFile.ClearMemory();

        AssetCollection.Collect();
    }
}
