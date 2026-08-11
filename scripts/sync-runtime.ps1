$ErrorActionPreference = "Continue"

$root = Split-Path -Parent $PSScriptRoot
$payloadBin = Join-Path $root "payload\bin"
$distBin = Join-Path $root "dist\bin"
$distLists = Join-Path $root "dist\lists"
$distStrategies = Join-Path $root "dist\strategies"
$rt = "C:\ProgramData\DpiTray"
$errLog = Join-Path $rt "logs\sync-last-error.txt"
New-Item -ItemType Directory -Force -Path (Join-Path $rt "logs") | Out-Null

function Write-ErrLog([string]$msg) {
  $msg | Out-File -FilePath $errLog -Encoding utf8
  Write-Host $msg
}

function Copy-SafeFile {
  param(
    [string]$Source,
    [string]$Destination,
    [int]$Retries = 5
  )

  if (-not (Test-Path $Source)) { return $true }

  $destDir = Split-Path -Parent $Destination
  if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null
  }

  # Skip overwrite if identical
  if (Test-Path $Destination) {
    $s = Get-Item $Source
    $d = Get-Item $Destination
    if ($s.Length -eq $d.Length -and $s.LastWriteTimeUtc -le $d.LastWriteTimeUtc.AddSeconds(1)) {
      return $true
    }
  }

  for ($i = 1; $i -le $Retries; $i++) {
    try {
      Copy-Item -LiteralPath $Source -Destination $Destination -Force -ErrorAction Stop
      return $true
    }
    catch {
      if ($i -eq $Retries) {
        # Locked driver file: keep existing destination if present
        if (Test-Path $Destination) {
          Write-Host ("  [warn] locked, keep existing: " + (Split-Path -Leaf $Destination))
          return $true
        }
        Write-Host ("  [warn] copy failed: " + (Split-Path -Leaf $Destination) + " :: " + $_.Exception.Message)
        return $false
      }
      Start-Sleep -Milliseconds (200 * $i)
    }
  }
  return $false
}

function Sync-Dir {
  param([string]$SourceDir, [string]$DestDir)
  if (-not (Test-Path $SourceDir)) { return }
  New-Item -ItemType Directory -Force -Path $DestDir | Out-Null
  Get-ChildItem -LiteralPath $SourceDir -File -Force | ForEach-Object {
    Copy-SafeFile -Source $_.FullName -Destination (Join-Path $DestDir $_.Name) | Out-Null
  }
}

Write-Host "[INFO] Sync runtime -> dist + ProgramData"

New-Item -ItemType Directory -Force -Path $distBin, $distLists, $distStrategies | Out-Null
New-Item -ItemType Directory -Force -Path "$rt\bin", "$rt\lists", "$rt\strategies", "$rt\logs" | Out-Null

Sync-Dir $payloadBin $distBin
Sync-Dir (Join-Path $root "payload\lists") $distLists
Sync-Dir (Join-Path $root "payload\strategies") $distStrategies

# ProgramData is the live ASCII runtime used by DpiTray
Sync-Dir $payloadBin "$rt\bin"
Sync-Dir $distBin "$rt\bin"
Sync-Dir (Join-Path $root "payload\lists") "$rt\lists"
Sync-Dir (Join-Path $root "payload\strategies") "$rt\strategies"

$required = @("winws.exe", "cygwin1.dll", "WinDivert.dll", "WinDivert64.sys")
$missing = @()
foreach ($name in $required) {
  $pRt = Join-Path "$rt\bin" $name
  $pDist = Join-Path $distBin $name
  if (-not (Test-Path $pRt) -or (Get-Item $pRt).Length -le 0) { $missing += "ProgramData\$name" }
  if (-not (Test-Path $pDist) -or (Get-Item $pDist).Length -le 0) { $missing += "dist\bin\$name" }
}

if ($missing.Count -gt 0) {
  Write-ErrLog ("Missing files:`r`n - " + ($missing -join "`r`n - "))
  if (-not [Environment]::UserInteractive) { exit 1 }
  Write-Host ""
  Write-Host "ERROR: missing runtime files. Window kept open."
  Write-Host "Log: $errLog"
  try { $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") } catch { Start-Sleep 30 }
  exit 1
}

Write-Host "[OK] runtime synced"
if (Test-Path $errLog) { Remove-Item $errLog -Force -ErrorAction SilentlyContinue }
