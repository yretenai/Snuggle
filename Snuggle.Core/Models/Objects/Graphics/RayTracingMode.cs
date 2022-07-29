namespace Snuggle.Core.Models.Objects.Graphics;

public enum RayTracingMode {
    Off = 0,
    Static = 1,
    DynamicTransform = 2,
    DynamicGeometry = 3,
}

public enum ReflectionProbeUsage {
    Off = 0,
    BlendProbes = 1,
    BlendProbesAndSkybox = 2,
    Simple = 3,
}

public enum LightProbeUsage {
    Off = 0,
    BlendProbes = 1,
    UseProxyVolume = 2,
    ExplicitIndex = 3,
    Custom = 4,
}
