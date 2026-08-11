@echo off
setlocal EnableExtensions
chcp 65001 >nul
cd /d "%~dp0"

set "ROOT=%CD%"
set "LOG=%ProgramData%\DpiTray\logs\launch-last.txt"
mkdir "%ProgramData%\DpiTray\logs" >nul 2>&1

net session >nul 2>&1
if errorlevel 1 (
  echo Нужны права администратора - сейчас UAC...
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

:menu
cls
echo ============================================
echo  DpiTray
echo ============================================
echo.
echo  1^) Запустить DpiTray
echo  2^) Починить Discord
echo  3^) Открыть логи
echo  0^) Выход
echo.
set /p "CHOICE=Выбор: "

if "%CHOICE%"=="1" goto launch
if "%CHOICE%"=="2" goto discord
if "%CHOICE%"=="3" goto logs
if "%CHOICE%"=="0" exit /b 0
goto menu

:launch
echo [%DATE% %TIME%] launch> "%LOG%"
call :stop_core
call :sync_runtime
if errorlevel 1 goto fail
if not exist "%ROOT%\DpiTray.exe" (
  echo [ERROR] Нет DpiTray.exe рядом с START.bat
  goto fail
)
echo Запускаю DpiTray...
start "" "%ROOT%\DpiTray.exe"
echo OK started>> "%LOG%"
echo.
echo Готово. В трее: стратегия -^> Старт.
echo Для Discord лучше: Discord (рабочая).
pause
exit /b 0

:discord
echo [%DATE% %TIME%] discord-fix> "%LOG%"
call :stop_core
taskkill /F /IM Discord.exe >> "%LOG%" 2>&1
taskkill /F /IM Update.exe >> "%LOG%" 2>&1
timeout /t 2 /nobreak >nul
call :sync_runtime
if errorlevel 1 goto fail

> "%ProgramData%\DpiTray\config.json" (
  echo {
  echo   "selectedStrategy": "discord",
  echo   "autoStart": false,
  echo   "autoStartStrategy": true
  echo }
)

set "D=%APPDATA%\discord"
if exist "%D%\Cache" rd /s /q "%D%\Cache"
if exist "%D%\Code Cache" rd /s /q "%D%\Code Cache"
if exist "%D%\GPUCache" rd /s /q "%D%\GPUCache"

if not exist "%ROOT%\DpiTray.exe" goto fail
start "" "%ROOT%\DpiTray.exe"
echo.
echo В трее выбрано Discord (рабочая) - нажми Старт, потом Discord.
echo.
pause
exit /b 0

:logs
if exist "%ProgramData%\DpiTray\logs\winws-last.log" (
  start "" notepad "%ProgramData%\DpiTray\logs\winws-last.log"
) else if exist "%LOG%" (
  start "" notepad "%LOG%"
) else (
  echo Логов пока нет.
  pause
)
exit /b 0

:stop_core
taskkill /F /IM DpiTray.exe >> "%LOG%" 2>&1
taskkill /F /IM winws.exe >> "%LOG%" 2>&1
taskkill /F /IM TgWsProxy_windows.exe >> "%LOG%" 2>&1
sc stop WinDivert >> "%LOG%" 2>&1
timeout /t 1 /nobreak >nul
exit /b 0

:sync_runtime
echo Готовлю runtime...
mkdir "C:\ProgramData\DpiTray\bin" >nul 2>&1
mkdir "C:\ProgramData\DpiTray\lists" >nul 2>&1
mkdir "C:\ProgramData\DpiTray\strategies" >nul 2>&1
mkdir "C:\ProgramData\DpiTray\logs" >nul 2>&1
mkdir "C:\ProgramData\DpiTray\tgproxy" >nul 2>&1
if exist "%ROOT%\bin\*" xcopy /Y /Q "%ROOT%\bin\*" "C:\ProgramData\DpiTray\bin\" >> "%LOG%" 2>&1
if exist "%ROOT%\lists\*" xcopy /Y /Q "%ROOT%\lists\*" "C:\ProgramData\DpiTray\lists\" >> "%LOG%" 2>&1
if exist "%ROOT%\strategies\*" xcopy /Y /Q "%ROOT%\strategies\*" "C:\ProgramData\DpiTray\strategies\" >> "%LOG%" 2>&1
if exist "%ROOT%\tgproxy\*" xcopy /Y /Q "%ROOT%\tgproxy\*" "C:\ProgramData\DpiTray\tgproxy\" >> "%LOG%" 2>&1
if not exist "C:\ProgramData\DpiTray\bin\winws.exe" (
  echo [ERROR] Нет winws.exe. Сначала build.bat.
  exit /b 1
)
exit /b 0

:fail
echo.
echo ОШИБКА. Лог: %LOG%
pause
exit /b 1