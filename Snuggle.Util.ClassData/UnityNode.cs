using System.Collections.Generic;

namespace Snuggle.Util.ClassData;

public class UnityNode {
    public string TypeName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public byte Level { get; set; }
    public int ByteSize { get; set; }
    public int Index { get; set; }
    public short Version { get; set; }
    public byte TypeFlags { get; set; }
    public int MetaFlag { get; set; }
    public List<UnityNode> SubNodes { get; set; } = null!;
}
