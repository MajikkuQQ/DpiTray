$ErrorActionPreference = "Continue"

Write-Host "[INFO] Stopping DpiTray / winws / WinDivert..."

foreach ($name in @("DpiTray", "winws")) {
  Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
    try { Stop-Process -Id $_.Id -Force -ErrorAction Stop } catch {}
  }
  & taskkill.exe /F /IM "$name.exe" 2>$null | Out-Null
}

& sc.exe stop WinDivert 2>$null | Out-Null

# Wait until service leaves RUNNING (or disappears)
for ($i = 0; $i -lt 20; $i++) {
  $q = (& sc.exe query WinDivert 2>&1 | Out-String)
  if ($q -match "1060" -or $q -notmatch "RUNNING") { break }
  Start-Sleep -Milliseconds 250
}

Start-Sleep -Milliseconds 500
Write-Host "[OK] runtime stopped"
