using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snuggle.Core.Extensions;

public class JsonMemoryConverter<T> : JsonConverter<Memory<T>> {
    public override Memory<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, Memory<T> value, JsonSerializerOptions options) {
        writer.WriteStartArray();
        foreach (var entry in value.Span) {
            JsonSerializer.Serialize(writer, entry, options);
        }

        writer.WriteEndArray();
    }
}
