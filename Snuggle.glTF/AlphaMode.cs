using System.Text.Json.Serialization;

namespace Snuggle.glTF;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlphaMode {
    [JsonPropertyName("OPAQUE")] Opaque,
    [JsonPropertyName("MASK")] Mask,
    [JsonPropertyName("BLEND")] Blend,
}
