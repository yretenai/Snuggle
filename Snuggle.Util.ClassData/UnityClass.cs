using System.Collections.Generic;

namespace Snuggle.Util.ClassData;

public class UnityClass {
    public string Name { get; set; } = null!;
    public string Namespace { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Module { get; set; } = null!;
    public int TypeID { get; set; }
    public string Base { get; set; } = null!;
    public List<string> Derived { get; set; } = null!;
    public uint DescendantCount { get; set; }
    public int Size { get; set; }
    public uint TypeIndex { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public bool IsEditorOnly { get; set; }
    public bool IsStripped { get; set; }
    public UnityNode? EditorRootNode { get; set; }
    public UnityNode? ReleaseRootNode { get; set; }
}
