namespace Snuggle.Core.Models.Objects.Graphics;

public enum CullingType {
    AlwaysAnimate = 0,
    BasedOnRenderers = 1,
    BasedOnClipBounds = 2,
    BasedOnUserBounds = 3,
}
