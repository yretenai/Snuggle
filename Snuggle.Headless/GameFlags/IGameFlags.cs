using DragonLib.CLI;

namespace Snuggle.Headless.GameFlags;

public abstract record IGameFlags : ICLIFlags {
    public abstract object ToOptions();
}
