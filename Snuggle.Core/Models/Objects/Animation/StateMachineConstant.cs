using System;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

public record StateMachineConstant {
    public static StateMachineConstant FromReader(BiEndianBinaryReader reader, ObjectDeserializationOptions options) =>
        throw
            // TODO(naomi): StateMachineConstant
            new NotImplementedException();

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        // TODO(naomi): StateMachineConstant
        throw new NotImplementedException();
    }
}
