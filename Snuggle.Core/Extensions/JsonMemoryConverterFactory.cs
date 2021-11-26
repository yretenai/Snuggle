using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snuggle.Core.Extensions;

public class JsonMemoryConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) {
        if (!typeToConvert.IsConstructedGenericType) {
            return false;
        }

        return typeToConvert.GetGenericTypeDefinition() == typeof(Memory<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var subType = typeToConvert.GetGenericArguments()[0];
        var generic = typeof(JsonMemoryConverter<>);
        var constructed = generic.MakeGenericType(subType);
        return Activator.CreateInstance(constructed) as JsonConverter;
    }
}
