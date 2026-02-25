#!/bin/bash
# Build Windows executable for MicStream Receiver
# Creates self-contained single-file .exe

set -e

echo "╔═══════════════════════════════════════════════════════╗"
echo "║    MicStream Receiver - Windows Build Script         ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo

cd "$(dirname "$0")/MicStreamReceiver"

# Clean previous builds
echo "[1/4] Cleaning previous builds..."
rm -rf bin/Release/net8.0/win-x64/publish 2>/dev/null || true

# Publish as self-contained single-file executable
echo "[2/4] Building self-contained Windows executable..."
dotnet publish -c Release -r win-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    /p:PublishTrimmed=false

# Create deployment package
echo "[3/4] Creating deployment package..."
DEPLOY_DIR="bin/Release/micstream-receiver-windows"
rm -rf "$DEPLOY_DIR" 2>/dev/null || true
mkdir -p "$DEPLOY_DIR"

# Copy executable
cp bin/Release/net8.0/win-x64/publish/MicStreamReceiver.exe "$DEPLOY_DIR/"

# Copy documentation
cp README.md "$DEPLOY_DIR/"
cp ../PHASE2_SUMMARY.md "$DEPLOY_DIR/" 2>/dev/null || true

# Create installation instructions
cat > "$DEPLOY_DIR/INSTALL.txt" << 'EOF'
MicStream Receiver - Installation Instructions
==============================================

INSTALLATION:
1. Copy MicStreamReceiver.exe to any folder on your Windows PC
   (e.g., C:\Program Files\MicStream\)

2. (Optional) Install VB-Cable for virtual microphone:
   - Download from: https://vb-audio.com/Cable/
   - Run VBCABLE_Setup_x64.exe as Administrator
   - Restart PC after installation

3. (Optional) Allow through Windows Firewall:
   - Windows may prompt when you first run the app
   - Click "Allow access" for Private networks

RUNNING:
- Double-click MicStreamReceiver.exe
- Or run from Command Prompt/PowerShell

FIRST TIME SETUP:
1. Start the application
2. Press [S] to start listening
3. Open Android MicStream app
4. Scan for devices or connect manually
5. Start streaming!

CONTROLS:
  [S] - Start Listening (+ mDNS Advertisement)
  [T] - Stop Listening
  [D] - Show Audio Devices
  [M] - Show mDNS Discovery Info
  [I] - Show Statistics
  [H] - Show Help Menu
  [Q] - Quit

TROUBLESHOOTING:
- Port 5005 must be available (not used by another app)
- For Discord: Set input to "CABLE Output" (if VB-Cable installed)
- For direct playback: Audio goes through default speakers

REQUIREMENTS:
- Windows 10/11 (64-bit)
- No .NET installation required (self-contained)
- Same WiFi network as Android device

For more details, see README.md

Project: https://github.com/karthik/micstream
EOF

# Create quick start batch file
cat > "$DEPLOY_DIR/Start-MicStream.bat" << 'EOF'
@echo off
echo Starting MicStream Receiver...
echo.
MicStreamReceiver.exe
pause
EOF

echo "[4/4] Packaging complete!"
echo
echo "═══════════════════════════════════════════════════════"
echo "✓ Build Complete!"
echo "═══════════════════════════════════════════════════════"
echo
echo "Deployment package location:"
echo "  $(pwd)/$DEPLOY_DIR/"
echo
echo "Contents:"
ls -lh "$DEPLOY_DIR/"
echo
echo "To deploy to Windows PC:"
echo "  1. Copy the entire folder to Windows"
echo "  2. Run MicStreamReceiver.exe"
echo "  3. Or use Start-MicStream.bat"
echo
echo "Executable size:"
du -h "$DEPLOY_DIR/MicStreamReceiver.exe"
echo "═══════════════════════════════════════════════════════"
