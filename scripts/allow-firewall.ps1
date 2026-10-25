$admin = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $admin.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
	Start-Process powershell -Verb RunAs -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`""
	return
}

Remove-NetFirewallRule -DisplayName 'CallBridge' -ErrorAction SilentlyContinue
New-NetFirewallRule -DisplayName 'CallBridge' -Direction Inbound -Action Allow -Profile Private `
	-Protocol TCP -LocalPort 47800 | Out-Null
New-NetFirewallRule -DisplayName 'CallBridge' -Direction Inbound -Action Allow -Profile Private `
	-Protocol UDP -LocalPort 47801 | Out-Null
Write-Host 'Allowed TCP 47800 and UDP 47801 on private networks.'
Start-Sleep 3
