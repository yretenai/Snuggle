using JetBrains.Annotations;

namespace Snuggle.Core.Extensions;

[PublicAPI]
public static class ValueExtensions {
    public static long ToTebiByte(this int value) => value * 1024 * 1024 * 1024 * 1024;

    public static long ToGibiByte(this int value) => value * 1024 * 1024 * 1024;

    public static long ToMebiByte(this int value) => value * 1024 * 1024;

    public static long ToKibiByte(this int value) => value * 1024;
}
