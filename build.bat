@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul
cd /d "%~dp0"
set "ERRLOG=%ProgramData%\DpiTray\logs\build-last-error.txt"
mkdir "%ProgramData%\DpiTray\logs" >nul 2>&1

echo ============================================
echo  DpiTray — сборка своего EXE
echo ============================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [INFO] Устанавливаю .NET 8 SDK через winget...
  winget install --id Microsoft.DotNet.SDK.8 -e --accept-source-agreements --accept-package-agreements
  if errorlevel 1 (
    echo [ERROR] Не удалось установить .NET 8 SDK
    echo Не удалось установить .NET 8 SDK> "%ERRLOG%"
    goto :fail
  )
  for /f "usebackq tokens=*" %%i in (`powershell -NoProfile -Command "[Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')"`) do set "PATH=%%i"
)

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ERROR] dotnet не найден. Откройте новый терминал и запустите build.bat снова.
  echo dotnet не найден> "%ERRLOG%"
  goto :fail
)

echo [INFO] dotnet:
dotnet --version
echo.

echo [INFO] Останавливаю DpiTray/winws/WinDivert перед копированием...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\stop-runtime.ps1"
if errorlevel 1 (
  echo [WARN] stop-runtime вернул ошибку — продолжаю
)

echo [INFO] Скачиваю runtime-бинарники...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\fetch-runtime.ps1"
if errorlevel 1 (
  echo [ERROR] Не удалось скачать runtime-файлы
  echo fetch-runtime failed> "%ERRLOG%"
  goto :fail
)

echo.
echo [INFO] Публикую single-file EXE...
dotnet publish "src\DpiTray.csproj" -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -o "dist"
if errorlevel 1 (
  echo [ERROR] Сборка не удалась
  echo dotnet publish failed> "%ERRLOG%"
  goto :fail
)

echo.
echo [INFO] Синхронизирую runtime...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\stop-runtime.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\sync-runtime.ps1"
if errorlevel 1 (
  echo [ERROR] Не удалось синхронизировать runtime
  echo sync-runtime failed — смотри окно выше> "%ERRLOG%"
  goto :fail
)

if exist "README.md" copy /Y "README.md" "dist\README.md" >nul

echo.
echo ============================================
echo  ГОТОВО: %cd%\dist\DpiTray.exe
echo  Runtime: C:\ProgramData\DpiTray
echo  Запускай: dist\ЗАПУСК.bat
echo ============================================
echo.
echo Нажмите любую клавишу, чтобы закрыть окно...
pause >nul
exit /b 0

:fail
echo.
echo ============================================
echo  ОШИБКА — окно НЕ закроется само
echo  Лог: %ERRLOG%
echo ============================================
echo.
pause
exit /b 1
