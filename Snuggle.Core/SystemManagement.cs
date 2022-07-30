using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Serilog;

namespace Snuggle.Core;

public static class SystemManagement {
    public static void DescribeLog() {
        var fv = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        Log.Information("{Name} (ﾉ◕ヮ◕)ﾉ*:･ﾟ✧ {Version}", fv.ProductName, fv.ProductVersion);
        Log.Information("Host: {Platform}", Environment.OSVersion);
        Log.Information("Framework: {Framework}", RuntimeInformation.FrameworkDescription);
        Log.Information("Runtime: {Runtime}", RuntimeInformation.RuntimeIdentifier);
    }
}
