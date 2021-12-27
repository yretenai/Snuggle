using System.Text.Json.Serialization;

namespace Snuggle.glTF;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnimationInterpolation {
    LINEAR,
    STEP,
    CUBICSPLINE,
}
