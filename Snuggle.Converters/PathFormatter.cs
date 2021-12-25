using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DragonLib;
using Snuggle.Core.Implementations;

namespace Snuggle.Converters;

public static class PathFormatter {
    private static readonly Regex Pattern = new(@"\{(\w+)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static string Format(string template, string ext, SerializedObject asset) {
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
                case "GAME":
                    builder.Append(asset.SerializedFile.Options.Game.ToString());
                    break;
                default:
                    builder.Append(match.Captures[0].Value.SanitizeDirname());
                    break;
            }
        }

        if (template.Length > gap) {
            builder.Append(templateSpan[gap..]);
        }

        return builder.ToString().TrimStart('/', '\\');
    }
}
