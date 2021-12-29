using System.Collections.Generic;
using System.Linq;
using Snuggle.Core.IO;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Implementations;

public class AnimatorOverrideController : RuntimeAnimatorController {
    public AnimatorOverrideController(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        Controller = PPtr<AnimatorController>.FromReader(reader, serializedFile);

        var count = reader.ReadInt32();
        Overrides.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            Overrides[PPtr<SerializedObject>.FromReader(reader, serializedFile)] = PPtr<SerializedObject>.FromReader(reader, serializedFile);
        }
    }

    public AnimatorOverrideController(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

    public PPtr<AnimatorController> Controller { get; set; } = PPtr<AnimatorController>.Null;
    public Dictionary<PPtr<SerializedObject>, PPtr<SerializedObject>> Overrides { get; set; } = new(); // TODO(naomi): AnimationClip

    public override AnimatorController? GetController() => Controller.Value;

    public override List<PPtr<SerializedObject>> GetAnimationClips() {
        var controller = GetController();
        if (controller == null) {
            return Overrides.Values.ToList();
        }

        var overrides = Overrides.ToDictionary(x => x.Key.GetCompositeId(), x => x.Value);

        var overridenClips = new List<PPtr<SerializedObject>>();
        var originalClips = controller.GetAnimationClips();
        overridenClips.EnsureCapacity(originalClips.Count);
        foreach (var clip in originalClips) {
            overridenClips.Add(overrides.TryGetValue(clip.GetCompositeId(), out var overridden) ? overridden : clip);
        }

        return overridenClips;
    }
}
