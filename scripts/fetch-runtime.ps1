$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$binDir = Join-Path $root "payload\bin"
New-Item -ItemType Directory -Force -Path $binDir | Out-Null

$base = "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/main/bin"
$files = @(
  "winws.exe",
  "cygwin1.dll",
  "WinDivert.dll",
  "WinDivert64.sys",
  "quic_initial_www_google_com.bin",
  "tls_clienthello_www_google_com.bin",
  "tls_clienthello_4pda_to.bin",
  "tls_clienthello_max_ru.bin",
  "ACTIVE_DISCORD_UDP.bin",
  "stun.bin",
  "stun2.bin"
)

foreach ($name in $files) {
  $dest = Join-Path $binDir $name
  if ((Test-Path $dest) -and ((Get-Item $dest).Length -gt 0)) {
    Write-Host "  [skip] $name"
    continue
  }

  $url = "$base/$name"
  Write-Host "  [get ] $name"
  Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
}

$required = @("winws.exe", "cygwin1.dll", "WinDivert.dll", "WinDivert64.sys")
foreach ($name in $required) {
  $path = Join-Path $binDir $name
  if (-not (Test-Path $path) -or (Get-Item $path).Length -le 0) {
    throw "Missing required runtime file: $name"
  }
}

Write-Host "[OK] runtime ready: $binDir"
