using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Implementations;

// [ObjectImplementation(UnityClassId.Animator)]
public class Animator : Behaviour {
    public Animator(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        Avatar = PPtr<SerializedObject>.FromReader(reader, serializedFile);
        Controller = PPtr<RuntimeAnimatorController>.FromReader(reader, serializedFile);
        CullingMode = (CullingMode) reader.ReadInt32();
        UpdateMode = (UpdateMode) reader.ReadInt32();

        ApplyRootMotion = reader.ReadBoolean();
        LinearVelocityBlending = reader.ReadBoolean();
        if (serializedFile.Version >= UnityVersionRegister.Unity2020_2) {
            StabilizeFeet = reader.ReadBoolean();
        }

        reader.Align();

        HasTransformHierarchy = reader.ReadBoolean();
        AllowConstantClipSamplingOptimization = reader.ReadBoolean();
        if (serializedFile.Version >= UnityVersionRegister.Unity2018_1) {
            KeepAnimatorControllerStateOnDisable = reader.ReadBoolean();
        }

        reader.Align();
    }

    public Animator(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    public PPtr<SerializedObject> Avatar { get; set; } = PPtr<SerializedObject>.Null; // TODO(naomi): Avatar
    public PPtr<RuntimeAnimatorController> Controller { get; set; } = PPtr<RuntimeAnimatorController>.Null;
    public CullingMode CullingMode { get; set; }
    public UpdateMode UpdateMode { get; set; }
    public bool ApplyRootMotion { get; set; }
    public bool LinearVelocityBlending { get; set; }
    public bool StabilizeFeet { get; set; }
    public bool HasTransformHierarchy { get; set; }
    public bool AllowConstantClipSamplingOptimization { get; set; }
    public bool KeepAnimatorControllerStateOnDisable { get; set; }
}
