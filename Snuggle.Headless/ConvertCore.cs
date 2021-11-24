using Snuggle.Core;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ClearMemory(AssetCollection assetCollection) {
        ClearTexMemory();
        foreach (var (_, file) in assetCollection.Files) {
            file.Free();
        }

        AssetCollection.Collect();
    }
}
