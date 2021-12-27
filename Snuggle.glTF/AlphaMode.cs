using System.Text.Json.Serialization;

namespace Snuggle.glTF;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlphaMode {
    OPAQUE,
    MASK,
    BLEND,
}
