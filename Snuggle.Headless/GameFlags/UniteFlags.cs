using System.Text;
using DragonLib.CLI;
using Snuggle.Core.Meta;
using Snuggle.Core.Options.Game;

namespace Snuggle.Headless.GameFlags;

[GameFlags(UnityGame.PokemonUnite)]
public record UniteFlags : IGameFlags {
    [CLIFlag("--game-version", Aliases = new[] { "gv" }, Category = "Unite Options", Default = UniteVersion.Version1_2, Help = "Pokemon UNITE Game Version")]
    public UniteVersion Version { get; set; }

    public override object ToOptions() => UniteOptions.Default with { GameVersion = Version };

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append($"{nameof(UniteFlags)} {{ ");
        sb.Append($"{nameof(Version)} = {Version:G}");
        sb.Append(" }");
        return sb.ToString();
    }
}
