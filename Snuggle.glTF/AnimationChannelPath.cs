using System.Text.Json.Serialization;

namespace Snuggle.glTF;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnimationChannelPath {
    translation,
    rotation,
    scale,
    weights,
}
