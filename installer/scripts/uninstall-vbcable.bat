@echo off
REM Uninstall VB-Cable if needed

echo.
echo ╔═══════════════════════════════════════════════════════╗
echo ║           Uninstall VB-Cable                          ║
echo ╚═══════════════════════════════════════════════════════╝
echo.
echo This will remove the VB-Cable virtual audio driver.
echo.
echo WARNING: This will affect any apps using VB-Cable!
echo.
echo Press Ctrl+C to cancel, or
pause

echo.
echo Launching VB-Cable uninstaller...
echo.

REM Check if VB-Cable is installed
reg query "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\VB-Audio Virtual Cable" >nul 2>&1
if %errorlevel% neq 0 (
    echo VB-Cable is not installed.
    goto :end
)

REM Find VB-Cable uninstaller
set "VBCABLE_UNINSTALL="
for /f "tokens=2*" %%a in ('reg query "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\VB-Audio Virtual Cable" /v UninstallString 2^>nul') do (
    set "VBCABLE_UNINSTALL=%%b"
)

if defined VBCABLE_UNINSTALL (
    echo Running: %VBCABLE_UNINSTALL%
    start "" %VBCABLE_UNINSTALL%
) else (
    echo Could not find VB-Cable uninstaller.
    echo Uninstall manually from Windows Settings → Apps
)

:end
echo.
echo After uninstalling, restart your PC.
echo.
pause
