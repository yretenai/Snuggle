using System.Reflection;
using System.Runtime.InteropServices;

namespace Snuggle.Native;

internal static class Helper {
    private static bool Loaded { get; } = false;

    private static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
        string rid, ext, prefix;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            rid = "linux";
            ext = "so";
            prefix = "lib";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            rid = "osx";
            ext = "dylib";
            prefix = "lib";
        } else {
            rid = "win";
            ext = "dll";
            prefix = string.Empty;
        }

        var libPath = Path.Combine("runtimes", $"{rid}-{(Environment.Is64BitProcess ? "x64" : "x86")}", "native", $"{prefix}{libraryName}.{ext}");
        return NativeLibrary.Load(libPath);
    }

    public static void Register() {
        if (Loaded) {
            return;
        }

        NativeLibrary.SetDllImportResolver(typeof(Helper).Assembly, Resolver);
    }
}
