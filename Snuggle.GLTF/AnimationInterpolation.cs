using System.Text.Json.Serialization;

namespace Snuggle.glTF;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnimationInterpolation {
    [JsonPropertyName("LINEAR")] Linear,
    [JsonPropertyName("STEP")] Step,
    [JsonPropertyName("CUBICSPLINE")] CubicSpline,
}
