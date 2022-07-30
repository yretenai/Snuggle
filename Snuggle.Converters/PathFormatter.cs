using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLib;
using Snuggle.Core;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;

namespace Snuggle.Converters;

public static class PathFormatter {
    private static readonly Regex Pattern = new(@"\{(\w+)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static string Format(string template, string ext, ISerializedObject asset) {
        var builder = new StringBuilder();
        var gap = 0;
        var templateSpan = template.AsSpan();
        var matches = Pattern.Matches(template);
        for (var i = 0; i < matches.Count; ++i) {
            var match = matches[i];
            if (match.Index > gap) {
                builder.Append(templateSpan.Slice(gap, match.Index - gap));
            }

            gap = match.Index + match.Length;

            var op = match.Captures[0].Value.ToUpperInvariant()[1..^1];

            switch (op) {
                case "ID":
                    builder.Append(asset.PathId.ToString("D", CultureInfo.InvariantCulture));
                    break;
                case "TYPE":
                    builder.Append(asset.ClassId);
                    break;
                case "SIZE":
                    builder.Append(asset.Size.ToString("D", CultureInfo.InvariantCulture));
                    break;
                case "CONTAINER":
                    builder.Append(asset.ObjectContainerPath.SanitizeDirname());
                    break;
                case "NAME":
                    builder.Append(asset.ToString().SanitizeDirname());
                    break;
                case "CONTAINERORNAME":
                    builder.Append(string.IsNullOrWhiteSpace(asset.ObjectContainerPath) ? asset.ToString().SanitizeDirname() : asset.ObjectContainerPath.SanitizeDirname());
                    break;
                case "CONTAINERORNAMEWITHEXT": {
                    var str = string.IsNullOrWhiteSpace(asset.ObjectContainerPath) ? asset.ToString().SanitizeDirname() : asset.ObjectContainerPath.SanitizeDirname();
                    if (!Path.HasExtension(str)) {
                        str += $".{ext}";
                    }

                    builder.Append(str);
                    break;
                }
                case "CONTAINERORNAMEWITHOUTEXT": {
                    var str = string.IsNullOrWhiteSpace(asset.ObjectContainerPath) ? asset.ToString().SanitizeDirname() : asset.ObjectContainerPath.SanitizeDirname();
                    if (Path.HasExtension(str)) {
                        str = Path.ChangeExtension(str, null);
                    }

                    builder.Append(str);
                    break;
                }
                case "EXT":
                    builder.Append(ext);
                    break;
                case "COMPANY":
                    builder.Append(asset.SerializedFile.Assets?.PlayerSettings?.CompanyName.SanitizeDirname());
                    break;
                case "ORGANIZATION":
                    builder.Append(asset.SerializedFile.Assets?.PlayerSettings?.OrganizationId.SanitizeDirname());
                    break;
                case "PROJECT":
                    builder.Append(asset.SerializedFile.Assets?.PlayerSettings?.ProjectName.SanitizeDirname());
                    break;
                case "PRODUCT":
                    builder.Append(asset.SerializedFile.Assets?.PlayerSettings?.ProductName.SanitizeDirname());
                    break;
                case "PRODUCTORPROJECT":
                    builder.Append(asset.SerializedFile.Assets?.PlayerSettings?.Name.SanitizeDirname());
                    break;
                case "VERSION":
                    builder.Append(asset.SerializedFile.Assets?.PlayerSettings?.BundleVersion.SanitizeDirname());
                    break;
                case "TAG":
                    builder.Append(Utils.GetNameFromTag(asset.SerializedFile.Tag) ?? string.Empty);
                    break;
                case "BUNDLE":
                    builder.Append(Utils.GetNameFromTagWithoutExtension(asset.SerializedFile.GetBundle()?.Tag) ?? string.Empty);
                    break;
                case "BUNDLEORTAG":
                    builder.Append(Utils.GetNameFromTagWithoutExtension(asset.SerializedFile.GetBundle()?.Tag) ?? Utils.GetNameFromTag(asset.SerializedFile.Tag) ?? string.Empty);
                    break;
                case "SCRIPT":
                    if (asset is MonoBehaviour monoBehaviour && monoBehaviour.Script.Value is not null) {
                        builder.Append(monoBehaviour.Script.Value);
                    }

                    break;
                default:
                    builder.Append(match.Captures[0].Value.SanitizeDirname());
                    break;
            }
        }

        if (template.Length > gap) {
            builder.Append(templateSpan[gap..]);
        }

        var result = builder.ToString().TrimStart('/', '\\');

        if (!Path.HasExtension(result)) {
            result += $".{ext}";
        }

        return result;
    }
}
