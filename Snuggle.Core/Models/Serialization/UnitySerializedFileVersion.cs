using JetBrains.Annotations;

namespace Snuggle.Core.Models.Serialization;

[PublicAPI]
public enum UnitySerializedFileVersion : uint {
    Invalid = 0,
    InitialVersion = 1,
    VariableCount = 2,
    VariableCountRemoved = 3,
    TypeTreeMeta = 4,
    TypeTreeIndex = 4,
    ExternalGuid = 5,
    UserInformation = 5,
    ExternalExtraPath = 6,
    UnityVersion = 7,
    BigId = 7,
    TargetPlatform = 8,
    HeaderContentAtFront = 9,
    TypeTreeBlobBeta = 10,
    ScriptTypeIndex = 11,
    ObjectDestroyedRemoved = 11,
    TypeTreeBlob = 12,
    TypeTreeEnabledSwitch = 13,
    TypeTreeHash = 13,
    BigIdAlwaysEnabled = 14,
    StrippedObject = 15,
    NewClassId = 16,
    StrippedType = 16,
    NewTypeData = 17,
    ShareableTypeTree = 18,
    TypeFlags = 19,
    RefObject = 20,
    TypeDependencies = 21,
    LargeFiles = 22,
    Latest = LargeFiles,
}
