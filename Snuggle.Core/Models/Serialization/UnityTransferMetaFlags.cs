using System;

namespace Snuggle.Core.Models.Serialization;

[Flags]
public enum UnityTransferMetaFlags : uint {
    None = 0x0,
    HideInEditor = 0x1,
    NotEditable = 0x10,
    StrongPPtr = 0x40,
    TreatIntegerValueAsBoolean = 0x100,
    SimpleEditor = 0x800,
    DebugProperty = 0x1000,
    AlignBytes = 0x4000,
    AnyChildUsesAlignBytesFlag = 0x8000,
    IgnoreWithInspectorUndo = 0x10000,
    EditorDisplaysCharacterMap = 0x40000,
    IgnoreInMetaFiles = 0x80000,
    TransferAsArrayEntryNameInMetaFiles = 0x100000,
    TransferUsingFlowMappingStyle = 0x200000,
    GenerateBitwiseDifferences = 0x400000,
    DontAnimate = 0x800000,
    TransferHex64 = 0x1000000,
    CharPropertyMask = 0x2000000,
    DontValidateUTF8 = 0x4000000,
    FixedBuffer = 0x8000000,
    DisallowSerializedPropertyModification = 0x10000000,
}
