using JetBrains.Annotations;

namespace Snuggle.Core.Options; 

[PublicAPI]
public interface IUnityGameOptions {
    public int Version { get; set; }
    public IUnityGameOptions Migrate();
}