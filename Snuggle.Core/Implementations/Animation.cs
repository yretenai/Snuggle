using System.Collections.Generic;
using Snuggle.Core.IO;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Implementations;

public class Animation : Behaviour {
    public Animation(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        AnimationClip = PPtr<SerializedObject>.FromReader(reader, serializedFile);

        var count = reader.ReadInt32();
        AnimationClips.EnsureCapacity(count);
        for (var i = 0; i < AnimationClips.Count; ++i) {
            AnimationClips.Add(PPtr<SerializedObject>.FromReader(reader, serializedFile));
        }

        WrapMode = reader.ReadInt32();

        PlayAutomatically = reader.ReadBoolean();
        AnimatePhysics = reader.ReadBoolean();
        reader.Align();

        CullingType = reader.ReadInt32();
    }

    public Animation(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    public PPtr<SerializedObject> AnimationClip { get; set; } = PPtr<SerializedObject>.Null; // TODO(naomi): AnimationClip
    public List<PPtr<SerializedObject>> AnimationClips { get; set; } = new(); // TODO(naomi): AnimationClip
    public int WrapMode { get; set; } // TODO(naomi): make enum
    public bool PlayAutomatically { get; set; }
    public bool AnimatePhysics { get; set; }
    public int CullingType { get; set; } // TODO(naomi): make enum
}
