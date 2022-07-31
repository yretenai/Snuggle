using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(TypeInfoDumper.BuildInfo.Description)]
[assembly: AssemblyDescription(TypeInfoDumper.BuildInfo.Description)]
[assembly: AssemblyCompany(TypeInfoDumper.BuildInfo.Company)]
[assembly: AssemblyProduct(TypeInfoDumper.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + TypeInfoDumper.BuildInfo.Author)]
[assembly: AssemblyTrademark(TypeInfoDumper.BuildInfo.Company)]
[assembly: AssemblyVersion(TypeInfoDumper.BuildInfo.Version)]
[assembly: AssemblyFileVersion(TypeInfoDumper.BuildInfo.Version)]
[assembly: MelonInfo(typeof(TypeInfoDumper.TypeInfoDumper), TypeInfoDumper.BuildInfo.Name, TypeInfoDumper.BuildInfo.Version, TypeInfoDumper.BuildInfo.Author, TypeInfoDumper.BuildInfo.DownloadLink)]
[assembly: MelonColor()]

// Create and Setup a MelonGame Attribute to mark a Melon as Universal or Compatible with specific Games.
// If no MelonGame Attribute is found or any of the Values for any MelonGame Attribute on the Melon is null or empty it will be assumed the Melon is Universal.
// Values for MelonGame Attribute can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame(null, null)]
