using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Equilibrium.Models;
using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI]
    public class UnityVersion : ICloneable, IComparable, IComparable<UnityVersion?>, IEquatable<UnityVersion?>, IComparable<Version?>, IEquatable<Version?>, ISpanFormattable {
        public UnityVersion(int major, int minor = 0, int build = 0, int revision = 0, UnityBuildType type = UnityBuildType.None, int extraVersion = 0) {
            Major = major;
            Minor = minor;
            Build = build;
            Type = type;
            Revision = revision;
            ExtraVersion = extraVersion;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Build { get; }
        public UnityBuildType Type { get; }
        public int Revision { get; }
        public int ExtraVersion { get; }

        public object Clone() => new UnityVersion(Major, Minor, Build, Revision, Type, ExtraVersion);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(object? version) {
            return version switch {
                Version v => CompareTo(v),
                UnityVersion v => CompareTo(v),
                _ => 1,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Version? value) {
            return value is null ? 1 : ((Version) this).CompareTo(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(UnityVersion? value) {
            if (value is null) {
                return 1;
            }

            if (ReferenceEquals(value, this)) {
                return 0;
            }

            return
                Major != value.Major ? Major > value.Major ? 1 : -1 :
                Minor != value.Minor ? Minor > value.Minor ? 1 : -1 :
                Build != value.Build ? Build > value.Build ? 1 : -1 :
                Revision != value.Revision ? Revision > value.Revision ? 1 : -1 :
                0;
        }

        public bool Equals(UnityVersion? other) {
            if (other == null) {
                return false;
            }

            return other.Major == Major && other.Minor == Minor && other.Build == Build && other.Type == Type && other.Revision == Revision && other.ExtraVersion == ExtraVersion;
        }

        public bool Equals(Version? other) {
            if (other == null) {
                return false;
            }

            return other == (Version) this;
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten);

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Build, Revision, Type, ExtraVersion);

        public override string ToString() {
            var str = $"{Major}.{Minor}.{Build}";
            if (Type != UnityBuildType.None) {
                str += (char) Type;
            }

            if (Revision != 0 ||
                ExtraVersion != 0) {
                str += Revision;
            }

            if (ExtraVersion != 0) {
                str += $"\n{ExtraVersion}";
            }

            return str;
        }

        private bool TryFormat(Span<char> destination, out int charsWritten) {
            var totalCharsWritten = 0;
            for (var i = 0; i < 6; i++) {
                if (destination.IsEmpty) {
                    charsWritten = 0;
                    return false;
                }

                switch (i) {
                    case 5 when ExtraVersion == 0:
                    case 4 when ExtraVersion == 0 && Revision == 0:
                    case 3 when Type == UnityBuildType.None:
                        continue;
                }

                switch (i) {
                    case 0:
                    case 1:
                        destination[0] = '.';
                        destination = destination[1..];
                        totalCharsWritten++;
                        break;
                    case 3:
                        destination[0] = (char) Type;
                        destination = destination[1..];
                        totalCharsWritten++;
                        continue;
                    case 4:
                        break;
                    case 5:
                        destination[0] = '\n';
                        destination = destination[1..];
                        totalCharsWritten++;
                        break;
                }

                var value = i switch {
                    0 => Major,
                    1 => Minor,
                    2 => Build,
                    4 => Revision,
                    _ => ExtraVersion,
                };

                if (!value.TryFormat(destination, out var valueCharsWritten)) {
                    charsWritten = 0;
                    return false;
                }

                totalCharsWritten += valueCharsWritten;
                destination = destination[valueCharsWritten..];
            }

            charsWritten = totalCharsWritten;
            return true;
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }

            return obj switch {
                Version version => Equals(version),
                UnityVersion unityVersion => Equals(unityVersion),
                _ => false,
            };
        }

        public static implicit operator Version(UnityVersion version) => new(version.Major, version.Minor, version.Build, version.Revision);

        public static implicit operator UnityVersion(Version version) => new(version.Major, version.Minor, version.Build, version.Revision);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UnityVersion? v1, UnityVersion? v2) {
            if (v2 is null) {
                // ReSharper disable once RedundantTernaryExpression
                return v1 is null ? true : false;
            }

            // ReSharper disable once SimplifyConditionalTernaryExpression
            return ReferenceEquals(v2, v1) ? true : v2.Equals(v1);
        }

        public static bool operator !=(UnityVersion? v1, UnityVersion? v2) => !(v1 == v2);

        public static bool operator <(UnityVersion? v1, UnityVersion? v2) {
            if (v1 is null) {
                return v2 is not null;
            }

            return v1.CompareTo(v2) < 0;
        }

        public static bool operator <=(UnityVersion? v1, UnityVersion? v2) {
            if (v1 is null) {
                return true;
            }

            return v1.CompareTo(v2) <= 0;
        }

        public static bool operator >(UnityVersion? v1, UnityVersion? v2) => v2 < v1;

        public static bool operator >=(UnityVersion? v1, UnityVersion? v2) => v2 <= v1;

        public static UnityVersion? Parse(string? input) {
            if (string.IsNullOrEmpty(input)) {
                return null;
            }

            var minor = 0;
            var build = 0;
            var revision = 0;
            var extra = 0;
            var typeChar = '\u0000';

            var major = ExtractVersionPart(input, out var end);
            if (end < input.Length) {
                input = input[end..];
                minor = ExtractVersionPart(input, out end);
                if (end < input.Length) {
                    input = input[end..];
                    (build, typeChar, revision) = ExtractRevisionPart(input, out end);
                    if (end < input.Length) {
                        input = input[end..];
                        extra = ExtractVersionPart(input, out _);
                    }
                }
            }

            return new UnityVersion(major, minor, build, revision, (UnityBuildType) typeChar, extra);
        }

        private static int ExtractVersionPart(string input, out int end) {
            end = input.IndexOf('.');
            if (end == -1) {
                end = input.Length;
            }

            if (!int.TryParse(input[..end], out var value)) {
                value = 0;
            }

            end += 1;

            return value;
        }

        private static (int build, char typeChar, int revision) ExtractRevisionPart(string input, out int end) {
            end = input.TakeWhile(char.IsLetterOrDigit).Count();
            if (end == -1) {
                end = input.Length;
            }

            input = input[..end];
            end += 1;

            var charEnd = input.TakeWhile(char.IsDigit).Count();
            if (charEnd == 0) {
                return (0, '\u0000', 0);
            }

            if (!int.TryParse(input[..charEnd], out var build)) {
                build = 0;
            }

            var typeChar = input[charEnd];
            charEnd += 1;

            input = input[charEnd..];
            if (!int.TryParse(input, out var revision)) {
                revision = 0;
            }

            return (build, typeChar, revision);
        }

        public static bool TryParse(string? input, out UnityVersion? version) {
            try {
                version = Parse(input);
                return true;
            } catch {
                version = null;
                return false;
            }
        }

        public static UnityVersion? ParseSafe(string? input) {
            try {
                return Parse(input);
            } catch {
                return null;
            }
        }
    }
}
