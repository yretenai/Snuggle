using System.Text.Json.Serialization;

namespace Snuggle.glTF;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnimationChannelPath {
    [JsonPropertyName("translation")] Translation,
    [JsonPropertyName("rotation")] Rotation,
    [JsonPropertyName("scale")] Scale,
    [JsonPropertyName("weights")]  Weights,
}
