namespace Snuggle.Core.Options.Game;

public interface IUnityGameOptions {
    public int Version { get; set; }
    public IUnityGameOptions Migrate();
}
