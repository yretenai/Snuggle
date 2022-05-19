using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Snuggle.Core.Interfaces;

public interface ISerializedObject {
    public long PathId { get; init; }

    public object ClassId { get; init; }

    public Dictionary<object, object> ExtraContainers { get; }

    public long Size { get; set; }

    public SerializedFile SerializedFile { get; init; }

    public bool IsMutated { get; set; }

    public string ObjectComparableName { get; }

    public string ObjectContainerPath { get; set; }

    public bool NeedsLoad { get; init; }
    public bool HasContainerPath => !string.IsNullOrWhiteSpace(ObjectContainerPath);

    public (long, string) GetCompositeId();

    public T GetExtraContainer<T>(object classId) where T : ISerialized, new();
    public bool TryGetExtraContainer(object classId, [MaybeNullWhen(false)] out ISerialized container);
    public string ToString();
}
