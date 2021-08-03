using System.ComponentModel;
using JetBrains.Annotations;

namespace Equilibrium.Meta {
    /// <summary>
    ///     Exists for game specific overrides.
    /// </summary>
    [PublicAPI]
    public enum UnityGame {
        Default,
        [Description("Pokémon UNITE")]
        PokemonUnite,
    }
}
