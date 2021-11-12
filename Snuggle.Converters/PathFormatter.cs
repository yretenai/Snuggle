using System.Globalization;
using DragonLib;
using Snuggle.Core.Implementations;

namespace Snuggle.Converters;

public static class PathFormatter {
    public static string Format(string template, string ext, SerializedObject asset) {
        template = template.Replace("{Id}", asset.PathId.ToString("D", CultureInfo.InvariantCulture));
        template = template.Replace("{Type}", asset.ClassId.ToString());
        template = template.Replace("{Size}", asset.Size.ToString("D", CultureInfo.InvariantCulture));
        template = template.Replace("{Container}", asset.ObjectContainerPath.Replace('{', '\0'));
        template = template.Replace("{Ext}", ext.Replace('{', '\0'));
        template = template.Replace("{Name}", asset.ToString().Replace('{', '\0'));
        return template.Replace('\0', '{').SanitizeDirname();
    }
}
