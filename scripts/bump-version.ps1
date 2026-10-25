param([Parameter(Mandatory)][string]$Version)

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must look like 1.2.3' }
$root = Split-Path -Parent $PSScriptRoot
$utf8 = New-Object Text.UTF8Encoding $false

function Edit-File([string]$path, [scriptblock]$change) {
	$text = [IO.File]::ReadAllText($path)
	[IO.File]::WriteAllText($path, (& $change $text), $utf8)
}

$gradle = Join-Path $root 'android\app\build.gradle.kts'
$code = [int]([regex]::Match([IO.File]::ReadAllText($gradle), 'versionCode = (\d+)').Groups[1].Value) + 1
Edit-File $gradle { param($t) $t -replace 'versionCode = \d+', "versionCode = $code" -replace 'versionName = "[^"]+"', "versionName = `"$Version`"" }
Edit-File (Join-Path $root 'magisk\module.prop') { param($t) $t -replace '(?m)^version=.*', "version=$Version" -replace '(?m)^versionCode=.*', "versionCode=$code" }
Edit-File (Join-Path $root 'windows\CallBridge.App\CallBridge.App.csproj') { param($t) $t -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>" }

Write-Host "Version $Version (code $code). Add a CHANGELOG entry, commit, then: git tag v$Version; git push origin main v$Version"
