using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

[PublicAPI]
public record LayerConstant(
    uint StateMachineIndex,
    uint StateMachineMotionSetIndex,
    HumanPoseMask BodyMask,
    List<SkeletonMaskElement> SkeletonMask,
    uint Binding,
    int LayerBlendingMode,
    float DefaultWeight,
    bool IKPass,
    bool SyncedLayerAffectsTiming) {
    public static LayerConstant FromReader(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        // TODO(naomi): LayerConstant
        throw new NotImplementedException();
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        //  TODO(naomi): LayerConstant
        throw new NotImplementedException();
    }
}
