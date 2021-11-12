using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Models.Serialization; 

// According to AssetTools.NET
// https://github.com/nesrak1/AssetsTools.NET/blob/b8caedac0bc9416b080e2d0c733fde8f3a668f98/AssetTools.NET/Standard/AssetsFileFormat/TypeField_0D.cs#L53-L59
[PublicAPI, Flags]
public enum UnityTypeArrayKind : uint {
    None = 0x0,
    Array = 0x1,
    Reference = 0x2,
    Registry = 0x4,
    ArrayOfReferences = 0x8,
}