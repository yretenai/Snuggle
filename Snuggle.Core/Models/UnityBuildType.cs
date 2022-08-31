namespace Snuggle.Core.Models;

public enum UnityBuildType {
    None = 0,
    Candidate = 'r', // rc
    Alpha = 'a',
    Beta = 'b',
    Final = 'f', // public
    Patch = 'p',
    Experimental = 'x',
}
