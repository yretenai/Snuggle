using JetBrains.Annotations;

namespace Equilibrium.Extensions {
    [PublicAPI]
    public static class ValueExtensions {
        public static long ToTebiBit(this int value) => value * 1024 * 1024 * 1024 * 1024;

        public static long ToGibiBit(this int value) => value * 1024 * 1024 * 1024;

        public static long ToMebiBit(this int value) => value * 1024 * 1024;

        public static long ToKibiBit(this int value) => value * 1024;
    }
}
