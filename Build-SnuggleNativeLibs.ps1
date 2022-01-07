# Initialize dev env
$installPath = &"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -version 17.0 -property installationpath
Import-Module (Join-Path $installPath "Common7\Tools\Microsoft.VisualStudio.DevShell.dll")
Enter-VsDevShell -VsInstallPath $installPath -SkipAutomaticLocation

# Build Texture2DDecoderNative
msbuild Library/AssetStudio/Texture2DDecoderNative/Texture2DDecoderNative.vcxproj /P:Configuration=Release /P:Platform=Win32
msbuild Library/AssetStudio/Texture2DDecoderNative/Texture2DDecoderNative.vcxproj /P:Configuration=Release /P:Platform=x64
msbuild Library/AssetStudio/Texture2DDecoderNative/Texture2DDecoderNative.vcxproj /P:Configuration=Debug /P:Platform=Win32
msbuild Library/AssetStudio/Texture2DDecoderNative/Texture2DDecoderNative.vcxproj /P:Configuration=Debug /P:Platform=x64
