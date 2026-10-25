$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'build\windows'
Remove-Item -Recurse -Force $out -ErrorAction SilentlyContinue
dotnet publish (Join-Path $root 'windows\CallBridge.App\CallBridge.App.csproj') `
	-c Release -r win-x64 --self-contained `
	-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None `
	-o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Windows app: $(Join-Path $out 'CallBridge.exe')"
