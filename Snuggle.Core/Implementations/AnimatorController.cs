using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Snuggle.Core.IO;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Animation;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

// [ObjectImplementation(UnityClassId.AnimatorController)]
public class AnimatorController : RuntimeAnimatorController {
    public AnimatorController(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        var controllerSize = reader.ReadInt32();
        ControllerStart = reader.BaseStream.Position;
        reader.BaseStream.Seek(controllerSize, SeekOrigin.Current);

        var count = reader.ReadInt32();
        Skeleton.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            Skeleton.Add(reader.ReadUInt32(), reader.ReadString32());
        }

        count = reader.ReadInt32();
        AnimationClips.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            AnimationClips.Add(PPtr<SerializedObject>.FromReader(reader, serializedFile));
        }

        StateMachine = StateMachineBehaviourVectorDescription.FromReader(reader, serializedFile);

        count = reader.ReadInt32();
        StateMachineBehaviours.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            StateMachineBehaviours.Add(PPtr<MonoBehaviour>.FromReader(reader, serializedFile));
        }

        MultiThreaded = reader.ReadBoolean();
        reader.Align();
    }

    public AnimatorController(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    private long ControllerStart { get; } = -1;

    public ControllerConstant? Controller { get; set; }
    public Dictionary<uint, string> Skeleton { get; set; } = new();
    public List<PPtr<SerializedObject>> AnimationClips { get; set; } = new(); // TODO(naomi): AnimationClip
    public StateMachineBehaviourVectorDescription StateMachine { get; set; } = StateMachineBehaviourVectorDescription.Default;
    public List<PPtr<MonoBehaviour>> StateMachineBehaviours { get; set; } = new();
    public bool MultiThreaded { get; set; }

    private bool ShouldDeserializeController => ControllerStart > -1 && Controller == null;

    [JsonIgnore]
    public override bool ShouldDeserialize => ShouldDeserializeController;

    public override AnimatorController GetController() => this;

    public override List<PPtr<SerializedObject>> GetAnimationClips() => AnimationClips;

    public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        base.Deserialize(reader, options);

        if (ShouldDeserializeController) {
            Controller = ControllerConstant.FromReader(reader, options);
        }
    }

    public override void Free() {
        base.Free();

        StateMachine.Free();
        Controller = null;
    }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new NotImplementedException();
    }
}
