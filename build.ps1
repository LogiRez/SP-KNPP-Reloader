# Builds the launcher and mod DLL.
param([string]$ReferenceDirectory,[string]$HarmonyDirectory)
$ErrorActionPreference='Stop'
$package=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$refs=@('mscorlib.dll','netstandard.dll','System.dll','System.Core.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.CoreModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.IMGUIModule.dll')|ForEach-Object {'/reference:'+(Join-Path $ReferenceDirectory $_)}
$resources=@('0Harmony','Mono.Cecil','MonoMod.Utils','MonoMod.RuntimeDetour')|ForEach-Object {'/resource:'+(Join-Path $HarmonyDirectory ($_+'.dll'))+',RCLI.Harmony.'+$_+'.dll'}
$files=@('Tool.cs','PackReload.cs','CarRebuild.cs','Entry.cs','BundledHarmony.cs','AttachmentLobby.cs','RcliSettings.cs')|ForEach-Object {Join-Path $PSScriptRoot $_}
& $compiler /nologo /noconfig /target:library /optimize+ /nostdlib+ ('/out:'+(Join-Path $package 'SP-KNPP.Mod.dll')) $refs $resources $files
if($LASTEXITCODE -ne 0){throw 'Tool build failed'}
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ('/out:'+(Join-Path $package 'SP-KNPP Reloader.exe')) (Join-Path $PSScriptRoot 'Injector.cs') (Join-Path $PSScriptRoot 'LauncherUI.cs') (Join-Path $PSScriptRoot 'RcliSettings.cs')
if($LASTEXITCODE -ne 0){throw 'Launcher build failed'}
