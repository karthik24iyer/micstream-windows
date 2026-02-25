#!/bin/bash
# Automated installer build script for MicStream Receiver
# Prepares all files needed for Inno Setup compilation

set -e

echo "╔═══════════════════════════════════════════════════════╗"
echo "║    MicStream Installer Builder                        ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo

cd "$(dirname "$0")"

# Step 1: Build Windows executable
echo "[1/5] Building MicStream Windows executable..."
./build-windows-exe.sh

# Step 2: Prepare installer directories
echo "[2/5] Creating installer directories..."
mkdir -p installer/sources
mkdir -p installer/assets
mkdir -p installer/vbcable
mkdir -p output

# Step 3: Copy executable to installer sources
echo "[3/5] Copying MicStream executable..."
cp MicStreamReceiver/bin/Release/net8.0/win-x64/publish/MicStreamReceiver.exe installer/sources/

# Step 4: Copy documentation
echo "[4/5] Copying documentation..."
cp MicStreamReceiver/README.md installer/sources/
cp PHASE2_SUMMARY.md installer/sources/ 2>/dev/null || true

# Create LICENSE.txt if not exists
if [ ! -f "LICENSE.txt" ]; then
    cat > LICENSE.txt << 'EOF'
MicStream Receiver
Copyright (c) 2026 Karthik

This project is licensed under GPL-3.0.

This software includes:
- NAudio (MIT License)
- Zeroconf (Apache License 2.0)
- VB-Cable virtual audio driver (Donation-ware by VB-Audio Software)

VB-Cable Attribution:
VB-Cable is developed by VB-Audio Software
Website: https://vb-audio.com/Cable/
License: Free for personal use (donation-ware)
EOF
fi

cp LICENSE.txt installer/sources/

# Step 5: Check for VB-Cable installer and driver files
echo "[5/5] Checking VB-Cable driver package..."
if [ ! -f "installer/vbcable/VBCABLE_Setup_x64.exe" ]; then
    echo ""
    echo "⚠ WARNING: VB-Cable installer not found!"
    echo ""
    echo "To complete the bundled installer, you need to:"
    echo "  1. Download VB-Cable from: https://vb-audio.com/Cable/"
    echo "  2. Extract VBCABLE_Driver_Pack45.zip"
    echo "  3. Copy ALL files to: installer/vbcable/"
    echo ""
    echo "After adding VB-Cable, run this script again."
    echo ""
    exit 1
fi

# Check for critical driver files
if [ ! -f "installer/vbcable/vbMmeCable64_win10.inf" ]; then
    echo ""
    echo "⚠ WARNING: VB-Cable driver files (.inf) missing!"
    echo ""
    echo "You need ALL VB-Cable files, not just the .exe:"
    echo "  - VBCABLE_Setup_x64.exe"
    echo "  - vbMmeCable64_win10.inf"
    echo "  - vbaudio_cable64_win10.sys"
    echo "  - vbaudio_cable64_win10.cat"
    echo "  - (and other driver files)"
    echo ""
    echo "Copy ALL files from VBCABLE_Driver_Pack45/ to installer/vbcable/"
    echo ""
    exit 1
fi

echo "✓ VB-Cable driver package complete ($(ls installer/vbcable/ | wc -l) files)"

echo ""
echo "═══════════════════════════════════════════════════════"
echo "✓ Build preparation complete!"
echo "═══════════════════════════════════════════════════════"
echo ""
echo "Files ready in:"
echo "  $(pwd)/installer/"
echo ""
echo "Directory structure:"
find installer -type f | sort

echo ""
echo "═══════════════════════════════════════════════════════"
echo "Next Steps (on Windows PC):"
echo "═══════════════════════════════════════════════════════"
echo ""
echo "1. Download Inno Setup (free):"
echo "   https://jrsoftware.org/isinfo.php"
echo ""
echo "2. Copy 'installer' folder to Windows PC"
echo ""
echo "3. Open Inno Setup Compiler on Windows"
echo ""
echo "4. File → Open → installer/MicStream-Setup.iss"
echo ""
echo "5. Build → Compile"
echo ""
echo "6. Output will be in:"
echo "   output/MicStream-Receiver-Setup-v2.0.0.exe"
echo ""
echo "7. Distribute this single installer to users!"
echo ""
echo "═══════════════════════════════════════════════════════"
echo ""
echo "Installer features:"
echo "  ✓ One-click installation"
echo "  ✓ Auto-installs VB-Cable"
echo "  ✓ Creates shortcuts"
echo "  ✓ Professional experience"
echo ""
