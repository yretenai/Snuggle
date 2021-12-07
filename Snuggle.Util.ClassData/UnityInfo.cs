using System.Collections.Generic;

namespace Snuggle.Util.ClassData;

public class UnityInfo {
    public string Version { get; set; } = null!;
    public List<UnityString> Strings { get; set; } = null!;
    public List<UnityClass> Classes { get; set; } = null!;
}
