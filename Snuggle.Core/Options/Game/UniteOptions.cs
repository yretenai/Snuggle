using System.ComponentModel;

namespace Snuggle.Core.Options.Game;

public record UniteOptions(UniteVersion GameVersion) : IUnityGameOptions {
    public const int LatestVersion = 1;
    public UniteOptions() : this(UniteVersion.Version1_1) { }
    public static UniteOptions Default { get; } = new();
    public int Version { get; set; } = LatestVersion;

    public IUnityGameOptions Migrate() => this;
}

public enum UniteVersion {
    [Description("1.1")] Version1_1 = 1,

    [Description("1.2")] Version1_2 = 2,
}
