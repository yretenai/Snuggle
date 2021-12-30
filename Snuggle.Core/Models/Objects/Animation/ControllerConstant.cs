using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

[PublicAPI]
public record ControllerConstant(List<LayerConstant> LayerArray, List<StateMachineConstant> StateMachineArray, List<ValueConstant> Values, ValueArray DefaultValues) {
    public static ControllerConstant FromReader(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        // TODO(naomi): ControllerConstant
        throw new NotImplementedException();
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        // TODO(naomi): ControllerConstant
        throw new NotImplementedException();
    }
}
