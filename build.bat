@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul
cd /d "%~dp0"

echo ============================================
echo  DpiTray — сборка своего EXE
echo ============================================
echo.

where winget >nul 2>&1
where dotnet >nul 2>&1
if errorlevel 1 (
  echo [INFO] Устанавливаю .NET 8 SDK через winget...
  winget install --id Microsoft.DotNet.SDK.8 -e --accept-source-agreements --accept-package-agreements
  if errorlevel 1 (
    echo [ERROR] Не удалось установить .NET 8 SDK
    exit /b 1
  )
  for /f "usebackq tokens=*" %%i in (`powershell -NoProfile -Command "[Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')"`) do set "PATH=%%i"
)

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ERROR] dotnet не найден. Откройте новый терминал и запустите build.bat снова.
  exit /b 1
)

echo [INFO] dotnet: 
dotnet --version
echo.

echo [INFO] Скачиваю runtime-бинарники ^(winws / WinDivert / payloads^)...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\fetch-runtime.ps1"
if errorlevel 1 (
  echo [ERROR] Не удалось скачать runtime-файлы
  exit /b 1
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
  exit /b 1
)

echo.
echo [INFO] Копирую runtime в dist...
if not exist "dist\bin" mkdir "dist\bin"
if not exist "dist\lists" mkdir "dist\lists"
if not exist "dist\strategies" mkdir "dist\strategies"

xcopy /E /I /Y /Q "payload\bin\*" "dist\bin\" >nul
xcopy /E /I /Y /Q "payload\lists\*" "dist\lists\" >nul
xcopy /E /I /Y /Q "payload\strategies\*" "dist\strategies\" >nul
if exist "README.md" copy /Y "README.md" "dist\README.md" >nul

echo.
echo ============================================
echo  ГОТОВО: %cd%\dist\DpiTray.exe
echo ============================================
exit /b 0
