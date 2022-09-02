# Snuggle [![Snuggle](https://github.com/yretenai/Snuggle/actions/workflows/dotnet.yml/badge.svg)](https://github.com/yretenai/Snuggle/actions/workflows/dotnet.yml)
Unity Processor

Snuggle is NOT feature complete and there is NO ETA on when it will be. Use [AssetRipper](https://github.com/AssetRipper/AssetRipper/) or [AssetStudio](https://github.com/Perfare/AssetStudio) as both are more feature rich and stable.

## Snuggle and it's contributors are not affiliated with, or sponsored by, or authorized by, Unity Technologies.

The "[Snuggle](Snuggle/Snuggle.ico)" icon is generated with help from Midjourney.

### Feature Level Comparison 

Snuggle's only benefit over other systems is the ability load large games without running out of memory due to Snuggle having deferred loading. Heavy assets such as textures and models are not fully loaded until they're accessed.


|         Feature         |                 Snuggle                 |       AssetStudio       |               AssetRipper                |        UABEA        |
| :---------------------: | :-------------------------------------: | :---------------------: | :--------------------------------------: | :-----------------: |
|  Minimum Unity Version  |                  5.0.0                  |          3.4.0          |                3.4.0[^1]                 |        5.0.0        |
|  Maximum Unity Version  |                 2021.1                  |         2022.1          |                2022.1[^2]                |       2021.2[^2]    |
|        Platforms        |                Mixed[^3]                |         Windows         |           Windows, Linux, Mac            | Windows, Linux, Mac |
|         License         |                   MIT                   |           MIT           |                  GPLv3                   |         MIT         |
|         Texture         |                    ✔️                 |           ✔️         |                    ✔️                    |         ✔️         |
|         Sprite          |                    ✔️                 |           ✔️         |                    ✔️                    |         ❌         |
|          Mesh           |                    ✔️                 |           ✔️         |                    ✔️                    |         ❌         |
|      MonoBehaviour      |                    ✔️                 |           ✔️         |                    ✔️                    |       ❌[^4]       |
|        FontAsset        |                    ❌                   |           ✔️         |                    ✔️                    |         ❌         |
|      MovieTexture       |                    ❌                   |           ✔️         |                    ✔️                    |         ❌         |
|        VideoClip        |                    ❌                   |           ✔️         |                    ✔️                    |         ❌         |
|        TextAsset        |                    ✔️                 |           ✔️         |                    ✔️                    |         ✔️         |
|      AnimationClip      |                    ❌                   |         ✔️[^5]       |                    ✔️                    |         ❌         |
|         Shader          |                    ❌                   |         ✔️[^6]       |                    ✔️                    |         ❌         |
|        AudioClip        |                    ✔️                 |           ✔️         |                    ✔️                    |         ❌         |
|         Terrain         |                    ❌                   |           ❌           |                    ✔️                    |         ❌         |
|    TypeTree Dumping     |                    ❌                   |           ✔️         |                    ✔️                    |         ❌         |
|   Shader Disassembly    |                    ❌                   |         ✔️[^6]       |                    ✔️                    |         ❌         |
|   IL2CPP Integration    |                    ✔️[^7]             |           ❌          |                  ✔️[^7]                  |         ❌         |
| Game Specific Framework |                    ✔️                 |           ❌           |                    ❌                    |         ❌         |
|      Plugin System      |                    ❌                   |           ❌           |                    ✔️                    |         ✔️         |
|    Deferred Loading     |                    ✔️                 |           ❌           |                    ❌                    |         ❌         |
|  VFS Container Caching  |                    ✔️                 |           ❌           |                    ❌                    |         ✔️         |
|    Rebuilding Assets    |                  ✔️[^8]               |           ❌           |                  ❌[^9]                  |       ✔️[^10]      |
|     Scene Hierarchy     |                    ❌                   |           ✔️           |                    ✔️                    |         ✔️         |
|    Previewing Assets    | Texture, Sprite, Mesh, GameObject Scene, Audio |  Texture, Sprite, Mesh, Audio, Font  |             Texture, Sprite, Audio              |        None         |
|     General Format      |                  json                   |          json           |                    yaml                  |         json        |
|     Texture Formats     |                png, dds                 |   png, tga, jpeg, bmp   | bmp, gif, jpeg, png, pbm, tiff, tga, raw |      png, tga       |
|      Mesh Formats       |                 glTF 2                  |        obj, fbx         |               glTF 2 (glb)               |                     |
|      Audio Formats      |              wav, ogg, fsb              | mp3, ogg, wav, m4a, fsb |       wav, ogg, m4a, at9, vag, fsb       |                     |

[^1]: AssetRipper has limited support for 3.0.0~3.3.0.
[^2]: These tools are designed to easily update for new Unity versions.
[^3]: Core Library and Command Line are supported on Windows, Mac, and Linux. GUI is Windows only.
[^4]: Support is added in AssetTools.NET.
[^5]: Humanoid Animations are not supported.
[^6]: Only SPIR-V and GLSL.
[^7]: Through Cpp2IL.
[^8]: Serialized Files and UnityFS Bundles only, assets are not rebuilt.
[^9]: Asset editing is a planned feature for AssetRipper.
[^10]: Bundles are not supported.

### Special Thanks

- [Perfare's AssetStudio](https://github.com/Perfare/AssetStudio/) for reference code for meshes, sprites, and textures- including the texture decoder.
- [DaZombieKiller's TypeTreeDumper](https://github.com/DaZombieKiller/TypeTreeDumper) for generating class definitions
- [AssetRipper's TypeTreeDumps](https://github.com/AssetRipper/TypeTreeDumps) for hosting type tree dumps

See [ATTRIBUTION.txt](ATTRIBUTION.txt)
