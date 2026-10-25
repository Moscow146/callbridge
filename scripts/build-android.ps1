param([switch]$Module)

$root = Split-Path -Parent $PSScriptRoot
$gradle = Join-Path $root 'android\gradlew.bat'
& $gradle -p (Join-Path $root 'android') assembleRelease
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$apk = Join-Path $root 'android\app\build\outputs\apk\release\app-release.apk'
Write-Host "APK: $apk"
if (-not $Module) { return }

$stage = Join-Path $root 'build\magisk'
Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
Copy-Item -Recurse (Join-Path $root 'magisk') $stage
$dir = Join-Path $stage 'system\priv-app\CallBridge'
New-Item -ItemType Directory -Force $dir | Out-Null
Copy-Item $apk (Join-Path $dir 'CallBridge.apk')

# Compress-Archive on Windows PowerShell 5.1 writes backslashes into entry names, which Android unzip rejects
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = Join-Path $root 'build\callbridge-magisk.zip'
Remove-Item -Force $zip -ErrorAction SilentlyContinue
$archive = [IO.Compression.ZipFile]::Open($zip, 'Create')
try {
	Get-ChildItem $stage -Recurse -File | ForEach-Object {
		$name = $_.FullName.Substring($stage.Length + 1).Replace('\', '/')
		[void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $name)
	}
}
finally {
	$archive.Dispose()
}
Write-Host "Magisk module: $zip"
