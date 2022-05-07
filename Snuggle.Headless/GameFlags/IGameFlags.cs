using DragonLib.CommandLine;

namespace Snuggle.Headless.GameFlags;

public abstract record GameFlags : CommandLineFlags {
    public abstract object ToOptions();
}
