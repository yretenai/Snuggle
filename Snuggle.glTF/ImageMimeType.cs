using System.Text.Json.Serialization;

namespace Snuggle.glTF;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageMimeType {
    [JsonPropertyName("image/jpeg")] Jpeg,
    [JsonPropertyName("image/png")] Png,
}
