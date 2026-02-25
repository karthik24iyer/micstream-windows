# MicStream Receiver - Bundled Installer

**Path A Implementation:** Professional installer with VB-Cable bundled

---

## 📦 **What This Does**

Creates a **single installer** that:
- ✅ Installs MicStream Receiver
- ✅ Installs VB-Cable virtual audio driver (automatically)
- ✅ Creates desktop/start menu shortcuts
- ✅ Configures everything for you
- ✅ Professional installation experience

**User Experience:** One-click install, just like the commercial MicStream app!

---

## 🛠️ **Building the Installer**

### **Prerequisites**

1. **Download Inno Setup** (Free)
   - Website: https://jrsoftware.org/isinfo.php
   - Download: Inno Setup 6.x (latest version)
   - Install on Windows PC

2. **Download VB-Cable Installer**
   - Website: https://vb-audio.com/Cable/
   - Download: VBCABLE_Driver_Pack43.zip
   - Extract `VBCABLE_Setup_x64.exe`

### **Setup Steps**

1. **Prepare VB-Cable:**
   ```
   # Create vbcable folder in installer directory
   mkdir installer/vbcable/

   # Copy VB-Cable installer
   cp VBCABLE_Setup_x64.exe installer/vbcable/
   ```

2. **Build MicStream executable:**
   ```bash
   cd /media/karthik/Misc/Projects/micstream-windows
   ./build-windows-exe.sh
   ```

3. **Copy to installer sources:**
   ```
   mkdir -p installer/sources/
   cp MicStreamReceiver/bin/Release/net8.0/win-x64/publish/MicStreamReceiver.exe installer/sources/
   ```

4. **Create assets folder (optional):**
   ```
   mkdir installer/assets/
   # Add icon.ico, wizard images (optional)
   ```

5. **Compile installer with Inno Setup:**
   - Open Inno Setup Compiler
   - File → Open → `MicStream-Setup.iss`
   - Build → Compile
   - Output: `output/MicStream-Receiver-Setup-v2.0.0.exe`

---

## 📁 **Directory Structure**

```
micstream-windows/
├── installer/
│   ├── MicStream-Setup.iss         # Inno Setup script
│   ├── README.md                   # This file
│   ├── vbcable/
│   │   └── VBCABLE_Setup_x64.exe   # VB-Cable installer (bundled)
│   ├── scripts/
│   │   ├── verify-installation.bat # Verification script
│   │   └── uninstall-vbcable.bat   # Uninstall helper
│   ├── assets/ (optional)
│   │   ├── icon.ico
│   │   ├── wizard-image.bmp
│   │   └── wizard-small.bmp
│   └── sources/
│       └── MicStreamReceiver.exe   # Built executable
└── output/
    └── MicStream-Receiver-Setup-v2.0.0.exe  # Final installer
```

---

## 🎯 **Installation Flow**

### **User Experience:**

1. **Download installer:**
   - User downloads: `MicStream-Receiver-Setup-v2.0.0.exe`
   - Single file, ~70-80MB

2. **Run installer:**
   - Double-click installer
   - Welcome screen with app info
   - Choose installation directory
   - Select tasks (install VB-Cable, create shortcuts)

3. **Automatic installation:**
   - Installs MicStream Receiver
   - Installs VB-Cable driver (auto-detects if already installed)
   - Creates shortcuts
   - Shows completion message

4. **Post-install:**
   - Restart PC (required for VB-Cable)
   - Run MicStream Receiver from Start Menu/Desktop
   - Start listening, connect from Android app
   - Done! ✅

---

## 🔧 **Installer Features**

### **Smart VB-Cable Installation**

```pascal
// Checks if VB-Cable already installed
function VBCableNotInstalled: Boolean;
var
  UninstallKey: String;
begin
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\VB-Audio Virtual Cable';
  Result := not RegKeyExists(HKEY_LOCAL_MACHINE, UninstallKey);
end;
```

- ✅ Only installs VB-Cable if not present
- ✅ Skips if already installed
- ✅ Can be unchecked by user

### **Post-Install Message**

Shows helpful next steps:
```
Installation Complete!

Next Steps:
1. Restart your PC (required for VB-Cable)
2. Run MicStream Receiver
3. Press [S] to start listening
4. Open Discord → Input Device → "CABLE Output"
5. Connect from Android MicStream app

Enjoy your wireless microphone!
```

### **Verification Script**

Included batch file checks:
- ✅ VB-Cable registry entry
- ✅ VB-Cable audio devices
- ✅ MicStream executable

### **Uninstall Helper**

Helps remove VB-Cable if needed:
- Finds VB-Cable uninstaller
- Launches it for user
- Provides instructions

---

## 📋 **Customization**

### **Change App Info**

Edit `MicStream-Setup.iss`:
```pascal
#define MyAppName "MicStream Receiver"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Your Name"
#define MyAppURL "https://your-url.com"
```

### **Add Custom Icons**

1. Create `installer/assets/icon.ico`
2. Uncomment in script:
   ```pascal
   SetupIconFile=..\assets\icon.ico
   ```

### **Modify Installation Path**

Change default directory:
```pascal
DefaultDirName={autopf}\MicStream
```

To:
```pascal
DefaultDirName=C:\MicStream
```

---

## 🚀 **Automated Build Script**

Create `build-installer.sh`:

```bash
#!/bin/bash
# Automated installer build script

echo "Building MicStream Installer..."

# Step 1: Build Windows executable
echo "[1/4] Building MicStream executable..."
./build-windows-exe.sh

# Step 2: Copy to installer sources
echo "[2/4] Copying files..."
mkdir -p installer/sources
cp MicStreamReceiver/bin/Release/net8.0/win-x64/publish/MicStreamReceiver.exe installer/sources/

# Step 3: Verify VB-Cable present
if [ ! -f "installer/vbcable/VBCABLE_Setup_x64.exe" ]; then
    echo "ERROR: VB-Cable installer not found!"
    echo "Download from: https://vb-audio.com/Cable/"
    echo "Extract VBCABLE_Setup_x64.exe to installer/vbcable/"
    exit 1
fi

# Step 4: Instructions for Windows
echo "[3/4] Ready to compile installer"
echo ""
echo "Next steps (on Windows PC):"
echo "  1. Copy 'installer' folder to Windows"
echo "  2. Open Inno Setup Compiler"
echo "  3. Open: installer/MicStream-Setup.iss"
echo "  4. Build → Compile"
echo "  5. Output: output/MicStream-Receiver-Setup-v2.0.0.exe"
echo ""
echo "[4/4] Done!"
```

---

## ✅ **Testing Checklist**

### **Before Release:**

- [ ] Build executable successfully
- [ ] VB-Cable installer present in bundle
- [ ] Compile installer with Inno Setup
- [ ] Test on clean Windows 11 VM
- [ ] Verify VB-Cable installs correctly
- [ ] Verify MicStream runs after install
- [ ] Test desktop/start menu shortcuts
- [ ] Verify installation script works
- [ ] Test uninstaller
- [ ] Check disk space requirements
- [ ] Verify admin rights prompt

### **Post-Install Testing:**

- [ ] VB-Cable shows in Device Manager
- [ ] "CABLE Input/Output" in Sound settings
- [ ] MicStream detects VB-Cable
- [ ] Audio routes correctly
- [ ] Discord can use "CABLE Output"
- [ ] End-to-end test with Android app

---

## 📊 **Installer Size**

| Component | Size |
|-----------|------|
| MicStreamReceiver.exe | ~67MB |
| VB-Cable installer | ~3MB |
| Scripts + docs | ~50KB |
| Installer overhead | ~5MB |
| **Total** | **~75MB** |

---

## 🎯 **Legal Considerations**

### **VB-Cable Bundling:**

From VB-Cable website:
- ✅ Free for personal use
- ✅ Can be bundled with proper attribution
- ✅ Donation-ware (not required)

**Required Attribution:**
```
This installer includes VB-Cable virtual audio driver by VB-Audio Software.
VB-Cable: https://vb-audio.com/Cable/
License: Donation-ware, free for personal use
```

Add to installer welcome screen and README.

---

## 🎉 **Result**

**Professional installer that:**
- ✅ Matches commercial MicStream app experience
- ✅ One-click installation
- ✅ No manual VB-Cable setup
- ✅ Creates "virtual microphone" automatically
- ✅ Works in Discord/games out of the box

**User Experience:**
```
Before (VB-Cable separate):
  1. Download MicStream
  2. Download VB-Cable
  3. Install VB-Cable (manual)
  4. Restart PC
  5. Install MicStream
  6. Configure
  ❌ Complex, error-prone

After (Bundled installer):
  1. Download MicStream-Setup.exe
  2. Run installer
  3. Restart PC
  4. Done!
  ✅ Simple, professional
```

---

**Next Step:** Download Inno Setup on Windows and compile the installer! 🚀
