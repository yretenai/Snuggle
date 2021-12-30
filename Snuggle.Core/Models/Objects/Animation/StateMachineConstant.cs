using System;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

[PublicAPI]
public record StateMachineConstant {
    public static StateMachineConstant FromReader(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        // TODO(naomi): StateMachineConstant
        throw new NotImplementedException();
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        // TODO(naomi): StateMachineConstant
        throw new NotImplementedException();
    }
}
