using System.Collections.Generic;
using Snuggle.Core.IO;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Implementations;

// [ObjectImplementation(UnityClassId.Animation)]
public class Animation : Behaviour {
    public Animation(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        AnimationClip = PPtr<SerializedObject>.FromReader(reader, serializedFile);

        var count = reader.ReadInt32();
        AnimationClips.EnsureCapacity(count);
        for (var i = 0; i < AnimationClips.Count; ++i) {
            AnimationClips.Add(PPtr<SerializedObject>.FromReader(reader, serializedFile));
        }

        WrapMode = (AnimationWrapMode) reader.ReadInt32();

        PlayAutomatically = reader.ReadBoolean();
        AnimatePhysics = reader.ReadBoolean();
        reader.Align();

        CullingType = (CullingType) reader.ReadInt32();
    }

    public Animation(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    public PPtr<SerializedObject> AnimationClip { get; set; } = PPtr<SerializedObject>.Null; // TODO(naomi): AnimationClip
    public List<PPtr<SerializedObject>> AnimationClips { get; set; } = new(); // TODO(naomi): AnimationClip
    public AnimationWrapMode WrapMode { get; set; }
    public bool PlayAutomatically { get; set; }
    public bool AnimatePhysics { get; set; }
    public CullingType CullingType { get; set; }
}
