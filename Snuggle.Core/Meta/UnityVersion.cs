using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Snuggle.Core.Models;

namespace Snuggle.Core.Meta;

[PublicAPI]
public record struct UnityVersion(int Major, int Minor = 0, int Build = 0, UnityBuildType Type = UnityBuildType.Release, int Revision = 0, string? ExtraVersion = null) : IComparable<UnityVersion>, IComparable<int> {
    public static UnityVersion MaxValue { get; } = new(int.MaxValue, int.MaxValue, int.MaxValue);
    public static UnityVersion MinValue { get; } = new(0);
    public static UnityVersion Default { get; } = new(5);

    public int CompareTo(int value) => Major > value ? 1 : Minor + Build == 0 ? 1 : -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(UnityVersion value) => Major != value.Major ? Major > value.Major ? 1 : -1 : Minor != value.Minor ? Minor > value.Minor ? 1 : -1 : Build != value.Build ? Build > value.Build ? 1 : -1 : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(object? version) {
        return version switch {
            UnityVersion v => CompareTo(v),
            int v => CompareTo(v),
            _ => 1,
        };
    }

    public static implicit operator UnityVersion(int version) => new(version);

    public static bool operator <(UnityVersion v1, UnityVersion v2) => v1.CompareTo(v2) < 0;

    public static bool operator <=(UnityVersion v1, UnityVersion v2) => v1.CompareTo(v2) <= 0;

    public static bool operator >(UnityVersion v1, UnityVersion v2) => v2 < v1;

    public static bool operator >=(UnityVersion v1, UnityVersion v2) => v2 <= v1;

    public static UnityVersion Parse(string? input) {
        if (string.IsNullOrEmpty(input)) {
            return MinValue;
        }

        var minor = 0;
        var build = 0;
        var revision = 0;
        var extra = default(string);
        var typeChar = '\u0000';

        var major = ExtractVersionPart(input, out var end);
        if (end < input.Length) {
            input = input[end..];
            minor = ExtractVersionPart(input, out end);
            if (end < input.Length) {
                input = input[end..];
                (build, typeChar, revision) = ExtractRevisionPart(input, out end);
                if (end < input.Length) {
                    extra = input[end..];
                }
            }
        }

        return new UnityVersion(major, minor, build, (UnityBuildType) typeChar, revision, extra);
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

        var charEnd2 = input[charEnd..].TakeWhile(char.IsDigit).Count() + charEnd;
        if (!int.TryParse(input[charEnd..charEnd2], out var revision)) {
            revision = 0;
            end = charEnd;
        } else {
            end = charEnd2;
        }

        return (build, typeChar, revision);
    }

    public static bool TryParse(string? input, out UnityVersion version) {
        try {
            version = Parse(input);
            return true;
        } catch {
            version = Default;
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

    public override string ToString() {
        var builder = new StringBuilder(32);
        builder.Append($"{Major}.{Minor}.{Build}");

        if (Type is UnityBuildType.None or UnityBuildType.Release) {
            return builder.ToString();
        }

        builder.Append((char) Type);

        if (Revision == 0) {
            return builder.ToString();
        }

        builder.Append(Revision);

        if (ExtraVersion == null) {
            return builder.ToString();
        }

        builder.Append(ExtraVersion);
        return builder.ToString();
    }

    public string ToStringSafe() => new(ToString().Select(x => x is '*' or '.' or '+' || char.IsLetterOrDigit(x) ? x : '.').ToArray());
}
