using System.ComponentModel;

namespace Snuggle.Core.Meta;

/// <summary>Exists for game specific overrides.</summary>
public enum UnityGame {
    Default,

    [Description("Pokémon UNITE")] PokemonUnite,
}
