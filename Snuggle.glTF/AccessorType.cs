using System.Text.Json.Serialization;

namespace Snuggle.glTF;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccessorType {
    [JsonPropertyName("SCALAR")] Scalar,
    [JsonPropertyName("VEC2")] Vector2,
    [JsonPropertyName("VEC3")] Vector3,
    [JsonPropertyName("VEC4")] Vector4,
    [JsonPropertyName("MAT2")] Matrix2x2,
    [JsonPropertyName("MAT3")] Matrix3x3,
    [JsonPropertyName("MAT4")] Matrix4x4,
}
