using System;
using System.Collections.Generic;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.RuntimeAnimatorController)]
public class RuntimeAnimatorController : NamedObject {
    public RuntimeAnimatorController(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) { }

    public RuntimeAnimatorController(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    public virtual List<PPtr<SerializedObject>> GetAnimationClips() => new();

    public virtual AnimatorController? GetController() => null;

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new NotImplementedException();
    }
}
