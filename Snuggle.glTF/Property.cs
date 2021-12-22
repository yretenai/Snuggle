using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snuggle.glTF;

public record Property {
    /// <summary>
    /// JSON object with extension-specific objects.
    /// </summary>
    [JsonPropertyName("extensions")]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
    
    /// <summary>
    /// Although `extras` <b>MAY</b> have any type, it is common for applications to store and access custom data as key/value pairs.
    /// Therefore, `extras` <b>SHOULD</b> be a JSON object rather than a primitive value for best portability.
    /// </summary>
    [JsonPropertyName("extras")]
    public Dictionary<string, JsonElement>? Extras { get; set; }
}
