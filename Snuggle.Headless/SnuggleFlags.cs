using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DragonLib.CommandLine;
using Snuggle.Core.Meta;

namespace Snuggle.Headless;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public record SnuggleFlags : CommandLineFlags {
    [Flag("no-mesh", Aliases = new[] { "m" }, Category = "General Options", Default = false, Help = "Do not export rigid meshes (can still export through game objects)")]
    public bool NoMesh { get; set; }

    [Flag("no-rigged-meshes", Aliases = new[] { "s" }, Category = "General Options", Default = false, Help = "Do not export rigged meshes (can still export through game objects)")]
    public bool NoSkinnedMesh { get; set; }

    [Flag("no-game-object", Aliases = new[] { "b" }, Category = "General Options", Default = false, Help = "Do not export game objects")]
    public bool NoGameObject { get; set; }

    [Flag("no-texture", Aliases = new[] { "T" }, Category = "General Options", Default = false, Help = "Do not export textures")]
    public bool NoTexture { get; set; }

    [Flag("no-text", Aliases = new[] { "t" }, Category = "General Options", Default = false, Help = "Do not export text assets")]
    public bool NoText { get; set; }

    [Flag("no-sprite", Aliases = new[] { "S" }, Category = "General Options", Default = false, Help = "Do not export sprite assets")]
    public bool NoSprite { get; set; }

    [Flag("no-audio", Aliases = new[] { "A" }, Category = "General Options", Default = false, Help = "Do not export audio clip assets")]
    public bool NoAudio { get; set; }

    [Flag("no-materials", Aliases = new[] { "M" }, Category = "General Options", Default = false, Help = "Do not export materials")]
    public bool NoMaterials { get; set; }

    [Flag("no-vertex-color", Aliases = new[] { "c" }, Category = "General Options", Default = false, Help = "Do not write vertex colors")]
    public bool NoVertexColor { get; set; }

    [Flag("no-morphs", Aliases = new[] { "O" }, Category = "General Options", Default = false, Help = "Do not write morphs")]
    public bool NoMorphs { get; set; }

    [Flag("no-script", Aliases = new[] { "B" }, Category = "General Options", Default = false, Help = "Do not export MonoBehaviour data")]
    public bool NoScript { get; set; }

    [Flag("no-cab", Category = "General Options", Default = false, Help = "Do not export ICABPathProvider data")]
    public bool NoCAB { get; set; }

    [Flag("data", Category = "General Options", Default = false, Help = "Do not convert, export serialization data instead")]
    public bool DataOnly { get; set; }

    [Flag("prefab", Category = "General Options", Default = false, Help = "Only export GameObjects as prefabs")]
    public bool GameObjectOnly { get; set; }

    [Flag("dump", Category = "General Options", Default = false, Help = "Dump SerializedFile Info")]
    public bool DumpSerializedInfo { get; set; }

    [Flag("dont-scan-up", Category = "General Options", Default = false, Help = "Do not scan for game object hierarchy ancestors")]
    public bool NoGameObjectHierarchyUp { get; set; }

    [Flag("dont-scan-down", Category = "General Options", Default = false, Help = "Do not scan for game object hierarchy descendants")]
    public bool NoGameObjectHierarchyDown { get; set; }

    [Flag("dds", Category = "General Options", Default = false, Help = "Export textures to DDS when possible, otherwise use PNG")]
    public bool WriteNativeTextures { get; set; }

    [Flag("use-astex", Category = "General Options", Default = false, Help = "Use Asset Studio's Texture Decoder when possible (only on windows)")]
    public bool UseTextureDecoder { get; set; }

    [Flag("fsb", Category = "General Options", Default = false, Help = "Write original audio file formats")]
    public bool WriteNativeAudio { get; set; }

    [Flag("low-memory", Category = "General Options", Default = false, Help = "Low memory mode, at the cost of performance")]
    public bool LowMemory { get; set; }

    [Flag("loose-meshes", Category = "General Options", Default = false, Help = "Export mesh even if they are not part of a renderer")]
    public bool LooseMeshes { get; set; }

    [Flag("loose-materials", Category = "General Options", Default = false, Help = "Export materials even if they are not part of a renderer")]
    public bool LooseMaterials { get; set; }

    [Flag("loose-textures", Category = "General Options", Default = false, Help = "Export textures even if they are not part of a material")]
    public bool LooseTextures { get; set; }

    [Flag("recursive", Aliases = new[] { "R" }, Category = "General Options", Default = false, Help = "Scan directories recursively for assets")]
    public bool Recursive { get; set; }

    [Flag("game", Aliases = new[] { "g" }, Category = "General Options", Default = UnityGame.Default, Help = "Game specific modifications")]
    public UnityGame Game { get; set; }

    [Flag("output-format", Aliases = new[] { "f" }, Category = "General Options", Default = "{Type}/{Container}/{Id}_{Name}.{Ext}", Help = "Output path format")]
    public string OutputFormat { get; set; } = null!;

    [Flag("containerless-output-format", Aliases = new[] { "F" }, Category = "General Options", Default = null, Help = "Output path format for objects without a container")]
    public string? ContainerlessOutputFormat { get; set; }

    [Flag("output", Aliases = new[] { "o", "out" }, Category = "General Options", Help = "Path to output files to", IsRequired = true)]
    public string OutputPath { get; set; } = null!;

    [Flag("overwrite", Category = "General Options", Default = false, Help = "Overwrite files if they already exist")]
    public bool Overwrite { get; set; }

    [Flag("name", Category = "General Options", Help = "Game Object Name/Container Path Filters", Extra = RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    public List<Regex> NameFilters { get; set; } = null!;

    [Flag("script", Category = "General Options", Help = "Script Class Filters", Extra = RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    public List<Regex> ScriptFilters { get; set; } = null!;

    [Flag("assembly", Category = "General Options", Help = "Script Assembly Filters", Extra = RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    public List<Regex> AssemblyFilters { get; set; } = null!;

    [Flag("id", Category = "General Options", Help = "Path ID Filters")]
    public List<long> PathIdFilters { get; set; } = null!;

    [Flag("only-cab", Category = "General Options", Default = false, Help = "Only export objects with CAB paths")]
    public bool OnlyCAB { get; set; }

    [Flag("paths", Category = "General Options", Positional = 0, Help = "Paths to load", IsRequired = true)]
    public List<string> Paths { get; set; } = null!;

    [Flag("ignore", Category = "General Options", Help = "ClassIds to Ignore")]
    public HashSet<string> IgnoreClassIds { get; set; } = null!;

    [Flag("exclusive", Category = "General Options", Help = "ClassIds to deserialize")]
    public HashSet<string> ExclusiveClassIds { get; set; } = null!;

    [Flag("keepxpos", Category = "General Options", Default = false, Help = "Do not mirror mesh X position")]
    public bool KeepXPos { get; set; }

    [Flag("keepxnorm", Category = "General Options", Default = false, Help = "Do not mirror mesh X normal")]
    public bool KeepXNorm { get; set; }

    [Flag("keepxtan", Category = "General Options", Default = false, Help = "Do not mirror mesh X tangent")]
    public bool KeepXTan { get; set; }

    public override string ToString() {
        var sb = new StringBuilder();
        sb.AppendLine($"{nameof(SnuggleFlags)} {{");
        sb.AppendLine($"  {nameof(NoMesh)} = {(NoMesh ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoSkinnedMesh)} = {(NoSkinnedMesh ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoGameObject)} = {(NoGameObject ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoTexture)} = {(NoTexture ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoText)} = {(NoText ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoSprite)} = {(NoSprite ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoAudio)} = {(NoAudio ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoMaterials)} = {(NoMaterials ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoVertexColor)} = {(NoVertexColor ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoMorphs)} = {(NoMorphs ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoScript)} = {(NoScript ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoCAB)} = {(NoCAB ? "True" : "False")},");
        sb.AppendLine($"  {nameof(DataOnly)} = {(DataOnly ? "True" : "False")},");
        sb.AppendLine($"  {nameof(GameObjectOnly)} = {(GameObjectOnly ? "True" : "False")},");
        sb.AppendLine($"  {nameof(DumpSerializedInfo)} = {(DumpSerializedInfo ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoGameObjectHierarchyUp)} = {(NoGameObjectHierarchyUp ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NoGameObjectHierarchyDown)} = {(NoGameObjectHierarchyDown ? "True" : "False")},");
        sb.AppendLine($"  {nameof(WriteNativeTextures)} = {(WriteNativeTextures ? "True" : "False")},");
        sb.AppendLine($"  {nameof(UseTextureDecoder)} = {(UseTextureDecoder ? "True" : "False")},");
        sb.AppendLine($"  {nameof(WriteNativeAudio)} = {(WriteNativeAudio ? "True" : "False")},");
        sb.AppendLine($"  {nameof(LowMemory)} = {(LowMemory ? "True" : "False")},");
        sb.AppendLine($"  {nameof(LooseMeshes)} = {(LooseMeshes ? "True" : "False")},");
        sb.AppendLine($"  {nameof(LooseMaterials)} = {(LooseMaterials ? "True" : "False")},");
        sb.AppendLine($"  {nameof(LooseTextures)} = {(LooseTextures ? "True" : "False")},");
        sb.AppendLine($"  {nameof(Recursive)} = {(Recursive ? "True" : "False")},");
        sb.AppendLine($"  {nameof(Game)} = {Game:G},");
        sb.AppendLine($"  {nameof(OutputFormat)} = {OutputFormat},");
        sb.AppendLine($"  {nameof(ContainerlessOutputFormat)} = {ContainerlessOutputFormat ?? "null"},");
        sb.AppendLine($"  {nameof(OutputPath)} = {OutputPath},");
        sb.AppendLine($"  {nameof(Overwrite)} = {(Overwrite ? "True" : "False")},");
        sb.AppendLine($"  {nameof(NameFilters)} = [{string.Join(", ", NameFilters.Select(x => x.ToString()))}],");
        sb.AppendLine($"  {nameof(ScriptFilters)} = [{string.Join(", ", ScriptFilters.Select(x => x.ToString()))}],");
        sb.AppendLine($"  {nameof(AssemblyFilters)} = [{string.Join(", ", AssemblyFilters.Select(x => x.ToString()))}],");
        sb.AppendLine($"  {nameof(PathIdFilters)} = [{string.Join(", ", PathIdFilters.Select(x => x.ToString()))}],");
        sb.AppendLine($"  {nameof(OnlyCAB)} = {(OnlyCAB ? "True" : "False")},");
        sb.AppendLine($"  {nameof(Paths)} = [{string.Join(", ", Paths)}],");
        sb.AppendLine($"  {nameof(IgnoreClassIds)} = [{string.Join(", ", IgnoreClassIds)}],");
        sb.AppendLine($"  {nameof(ExclusiveClassIds)} = [{string.Join(", ", ExclusiveClassIds)}],");
        sb.AppendLine($"  {nameof(KeepXPos)} = {(KeepXPos ? "True" : "False")},");
        sb.AppendLine($"  {nameof(KeepXNorm)} = {(KeepXNorm ? "True" : "False")},");
        sb.AppendLine($"  {nameof(KeepXTan)} = {(KeepXTan ? "True" : "False")}");
        sb.Append('}');
        return sb.ToString();
    }
}
