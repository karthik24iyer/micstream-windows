@echo off
REM MicStream Installation Verification Script
REM Checks if VB-Cable and MicStream are installed correctly

echo.
echo ╔═══════════════════════════════════════════════════════╗
echo ║     MicStream Installation Verification              ║
echo ╚═══════════════════════════════════════════════════════╝
echo.

echo [1/3] Checking VB-Cable installation...
reg query "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\VB-Audio Virtual Cable" >nul 2>&1
if %errorlevel% equ 0 (
    echo [✓] VB-Cable is installed
) else (
    echo [✗] VB-Cable NOT found
    echo     Install from: https://vb-audio.com/Cable/
)

echo.
echo [2/3] Checking audio devices...
powershell -Command "Get-WmiObject Win32_SoundDevice | Select-Object Name | Format-Table -HideTableHeaders" | findstr /C:"CABLE" >nul 2>&1
if %errorlevel% equ 0 (
    echo [✓] VB-Cable audio devices detected
    powershell -Command "Get-WmiObject Win32_SoundDevice | Select-Object Name | findstr CABLE"
) else (
    echo [⚠] VB-Cable devices not detected
    echo     Try restarting your PC
)

echo.
echo [3/3] Checking MicStream Receiver...
if exist "%~dp0MicStreamReceiver.exe" (
    echo [✓] MicStreamReceiver.exe found
) else (
    echo [✗] MicStreamReceiver.exe NOT found
)

echo.
echo ═══════════════════════════════════════════════════════
echo.

if %errorlevel% equ 0 (
    echo ✓ Installation looks good!
    echo.
    echo Next steps:
    echo   1. Restart PC if you just installed VB-Cable
    echo   2. Run MicStreamReceiver.exe
    echo   3. Press [S] to start listening
    echo   4. Connect from Android app
) else (
    echo ⚠ Some issues detected - see messages above
)

echo.
echo Press any key to close...
pause >nul
