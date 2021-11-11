using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using JetBrains.Annotations;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Options
{
    [PublicAPI]
    public record UnityGameOptions {
        public static UnityGameOptions Default { get; }

        static UnityGameOptions() {
            Default = new UnityGameOptions().Migrate();
        }
        
        public const int LatestVersion = 1;
        public int Version { get; set; } = LatestVersion;
        public Dictionary<UnityGame, JsonElement> StorageMap { get; set; } = new();

        public bool TryGetOptionsObject<T>(UnityGame game, [MaybeNullWhen(false)] out T options) where T : IUnityGameOptions, new() {
            options = default;
            if (!StorageMap.TryGetValue(game, out var anonymousOptionsObject)) {
                return false;
            }

            if (anonymousOptionsObject.ValueKind != JsonValueKind.Object) {
                return false;
            }

            options = anonymousOptionsObject.Deserialize<T>(SnuggleOptions.JsonOptions);
            if (options == null) {
                return false;
            }
            
            options = (T) options.Migrate();
            return true;
        }

        public void SetOptions(UnityGame game, object options) {
            StorageMap[game] = JsonSerializer.SerializeToElement(options, options.GetType(), SnuggleOptions.JsonOptions);
        }

        public void MigrateOptions<T>(UnityGame game) where T : IUnityGameOptions, new() {
            SetOptions(game, !TryGetOptionsObject<T>(game, out var options) ? new T() : options.Migrate());
        }

        public UnityGameOptions Migrate() {
            MigrateOptions<UniteOptions>(UnityGame.PokemonUnite);
            return this;
        }
    }
}
