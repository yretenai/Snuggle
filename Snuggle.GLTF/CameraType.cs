using System.Text.Json.Serialization;

namespace Snuggle.glTF;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CameraType {
    [JsonPropertyName("perspective")] Perspective,
    [JsonPropertyName("orthographic")] Orthographic,
}
