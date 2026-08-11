$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$destDirs = @(
  (Join-Path $root "payload\tgproxy"),
  (Join-Path $root "dist\tgproxy"),
  "C:\ProgramData\DpiTray\tgproxy"
)

# Только официальный релиз Flowseal/tg-ws-proxy (не сторонние "сборки" с возможными RAT)
$url = "https://github.com/Flowseal/tg-ws-proxy/releases/download/v1.9.1/TgWsProxy_windows.exe"
$expectedSha = "2823fcda2cdd209eb595cba75587c0fdbe40d6d863eed482794af1cca0c1b6fc"

$tmp = Join-Path $env:TEMP ("TgWsProxy_" + [guid]::NewGuid().ToString("N") + ".exe")
Write-Host "[INFO] Downloading official TgWsProxy..."
Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing

$sha = (Get-FileHash -Path $tmp -Algorithm SHA256).Hash.ToLowerInvariant()
if ($sha -ne $expectedSha) {
  Remove-Item $tmp -Force -ErrorAction SilentlyContinue
  throw "SHA256 mismatch for TgWsProxy_windows.exe. Got $sha expected $expectedSha"
}

foreach ($dir in $destDirs) {
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  $dest = Join-Path $dir "TgWsProxy_windows.exe"
  Copy-Item -LiteralPath $tmp -Destination $dest -Force
  Write-Host ("  [ok] " + $dest)
}

@"
Official source: https://github.com/Flowseal/tg-ws-proxy/releases/tag/v1.9.1
SHA256: $expectedSha
Downloaded: $(Get-Date -Format o)
"@ | Set-Content (Join-Path $destDirs[0] "SOURCE.txt") -Encoding UTF8

Remove-Item $tmp -Force -ErrorAction SilentlyContinue
Write-Host "[OK] TgWsProxy ready"
