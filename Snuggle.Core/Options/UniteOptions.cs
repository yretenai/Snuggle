using System.ComponentModel;
using JetBrains.Annotations;

namespace Snuggle.Core.Options
{
    [PublicAPI]
    public record UniteOptions(UniteVersion GameVersion) : IUnityGameOptions {
        public UniteOptions() : this(UniteVersion.Version1_1) {}
        
        public const int LatestVersion = 1;
        public int Version { get; set; } = LatestVersion;
        public static UniteOptions Default { get; } = new();
        
        public IUnityGameOptions Migrate() {
            return this;
        }
    }

    public enum UniteVersion {
        [Description("1.1")]
        Version1_1,
        [Description("1.2")]
        Version1_2,
    }
}
