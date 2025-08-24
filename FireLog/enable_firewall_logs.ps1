# === Enable Firewall Logging ===
try {
    $logPath = "C:\Windows\System32\LogFiles\Firewall\pfirewall.log"

    Set-NetFirewallProfile -Profile Domain,Public,Private `
                           -LogFileName $logPath `
                           -LogAllowed "True" `
                           -LogBlocked "True"

    Write-Host "`n✅ Firewall logging has been enabled at $logPath`n"
}
catch {
    Write-Host "❌ Failed to enable firewall logging: $_"
    exit 1
}
