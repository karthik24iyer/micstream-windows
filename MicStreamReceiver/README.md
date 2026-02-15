# MicStream Receiver - Phase 2

**Status**: Phase 2 Complete ✓
**Version**: 2.0.0
**Framework**: .NET 8.0

---

## Implementation Status

### ✅ Phase 1 - Complete (Basic UDP + Audio Playback)

- [x] .NET 8 Console Application
- [x] NAudio Integration (v2.2.1)
- [x] Project Structure (Models/, Services/)
- [x] **AudioPacket Model** - Parses `[seq(4)][pcm_data]` packet format
- [x] **UdpListener Service** - Receives UDP packets on port 5005
- [x] **AudioPlayback Service** - NAudio playback (48kHz, 16-bit, mono)
- [x] **VirtualDeviceManager Service** - Detects VB-Cable virtual audio device
- [x] **Console UI** - Interactive menu with start/stop, stats, device info
- [x] **Packet Loss Detection** - Tracks sequence numbers and loss rate
- [x] **Statistics Display** - Real-time packet stats, buffer status, uptime

### ✅ Phase 2 - Complete (mDNS Service Discovery)

- [x] **Zeroconf Integration** (v3.7.16)
- [x] **DiscoveryService** - mDNS advertisement (`_micstream._udp.local`)
- [x] **Auto-advertisement** - Starts when listening begins
- [x] **TXT Records** - version, name, capabilities (opus, rnnoise)
- [x] **Network Detection** - Auto-detects local IP addresses
- [x] **Enhanced UI** - Shows mDNS status, [M] menu option
- [x] **Platform Support** - Windows (full) + Linux (limited)

### 📋 Phase 1 Checklist (from WINDOWS_AGENT.md)

- [x] Create .NET 8 WPF project (Console app for now, WPF UI in future)
- [x] Add NuGet packages (NAudio ✓)
- [x] Create project structure (Models, Services folders)
- [x] Implement `AudioPacket` model - parse [seq(4)][pcm_data]
- [x] Implement `UdpListener` - receive on port 5005, detect packet loss
- [x] Implement `AudioPlayback` - NAudio WaveOut/WASAPI, 48kHz 16-bit mono
- [x] Implement `VirtualDeviceManager` - detect VB-Cable, list devices
- [x] Create console UI - status, log, start/stop controls
- [ ] Test receiving packets (use Python/netcat for testing) - **Next Step**
- [ ] Test audio playback through default device
- [ ] Test with VB-Cable if installed
- [ ] Test with Android app end-to-end

---

## Project Structure

```
MicStreamReceiver/
├── Models/
│   └── AudioPacket.cs           # Packet parsing logic
├── Services/
│   ├── UdpListener.cs           # UDP reception service
│   ├── AudioPlayback.cs         # NAudio playback service
│   └── VirtualDeviceManager.cs  # VB-Cable detection
├── Program.cs                   # Main entry point + Console UI
├── MicStreamReceiver.csproj     # Project file
└── README.md                    # This file
```

---

## Building the Project

### Prerequisites

- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows 11** (recommended) - NAudio has full support on Windows
- **VB-Cable** (optional) - [Download](https://vb-audio.com/Cable/) for virtual microphone functionality

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run application
dotnet run
```

### Platform Notes

**Windows**: Full functionality including audio device enumeration and VB-Cable detection.

**Linux/macOS**: Limited functionality - builds successfully but audio device detection is disabled. Use for development/testing the UDP listener only.

---

## Running the Application

### 1. Start the Receiver

```bash
cd MicStreamReceiver
dotnet run
```

### 2. Console Interface

Upon starting, you'll see:

```
╔═══════════════════════════════════════════════════════╗
║        MicStream Receiver - Phase 1                   ║
║        Low-Latency Audio Streaming                    ║
╚═══════════════════════════════════════════════════════╝

=== Audio Devices ===
  [0] Speakers (Realtek High Definition Audio) (2 channels)
  [1] CABLE Input (VB-Audio Virtual Cable) (2 channels) [VB-CABLE]

✓ VB-Cable detected: CABLE Input (VB-Audio Virtual Cable)
  This device can be used as a virtual microphone in Discord/Games

Selected audio device: CABLE Input (VB-Audio Virtual Cable)

╔═══════════════════════════════════════════════════════╗
║                    Controls                           ║
╠═══════════════════════════════════════════════════════╣
║  [S] Start Listening                                  ║
║  [T] Stop Listening                                   ║
║  [D] Show Audio Devices                               ║
║  [I] Show Statistics                                  ║
║  [H] Show This Menu                                   ║
║  [Q] Quit                                             ║
╚═══════════════════════════════════════════════════════╝
```

### 3. Available Commands

| Key | Action |
|-----|--------|
| `S` | Start listening for UDP packets on port 5005 |
| `T` | Stop listening |
| `D` | Show all audio devices |
| `I` | Show statistics (packets received, loss rate, buffer status) |
| `H` | Show help menu |
| `Q` | Quit application |

---

## Testing

### Test 1: UDP Packet Reception (Manual Test)

You can test the UDP listener using Python or netcat:

#### Using Python:

```python
import socket
import struct
import time

# Create UDP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Target: localhost:5005
target = ("127.0.0.1", 5005)

# Send test packets
sequence = 0
while True:
    # Create packet: [4 bytes sequence][dummy PCM data]
    packet = struct.pack('>I', sequence) + b'\x00' * 960  # 960 bytes of silence
    sock.sendto(packet, target)

    sequence += 1
    time.sleep(0.02)  # 20ms interval (50 packets/second)

    if sequence % 50 == 0:
        print(f"Sent {sequence} packets")
```

#### Using netcat:

```bash
# Send a single test packet
echo -n -e '\x00\x00\x00\x01' | cat - <(dd if=/dev/zero bs=960 count=1 2>/dev/null) | nc -u localhost 5005
```

### Test 2: Audio Playback (Requires Windows)

1. Install VB-Cable from https://vb-audio.com/Cable/
2. Run MicStream Receiver
3. Press `S` to start listening
4. Use the Python test script above to send audio data
5. Audio should play through VB-Cable (or default speakers if VB-Cable not installed)

### Test 3: End-to-End with Android App (Future)

Once the Android app is ready:

1. Start MicStream Receiver on Windows PC
2. Press `S` to start listening
3. Open Android app and connect to PC IP
4. Speak into phone - audio should come through PC speakers/VB-Cable

---

## Configuration

### Audio Format

The receiver expects audio in the following format:

- **Sample Rate**: 48000 Hz
- **Bit Depth**: 16-bit PCM
- **Channels**: Mono (1 channel)
- **Buffer Size**: 960 samples (20ms frames)

### Network Configuration

- **Protocol**: UDP (connectionless)
- **Port**: 5005 (default, not configurable in Phase 1)
- **Packet Format**: `[4 bytes: sequence number, big-endian][PCM data]`

### Buffer Settings

- **Playback Buffer**: 2 seconds capacity
- **Desired Latency**: 100ms
- **Overflow Behavior**: Discard old data

---

## Troubleshooting

### Build Errors

**Error: "dotnet: command not found"**
- Install .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0

**Error: "The type or namespace name 'NAudio' could not be found"**
- Run `dotnet restore` to download NAudio package

### Runtime Errors

**Error: "Port 5005 is already in use"**
- Another application is using port 5005
- Solution: Stop the other application or change the port in `UdpListener.cs`

**Error: "No audio playback"**
- Check Windows volume mixer
- Ensure audio device is not muted
- Verify NAudio is compatible with your audio driver

**Error: "VB-Cable not detected"**
- VB-Cable is not installed
- Solution: Install from https://vb-audio.com/Cable/ and restart PC
- Note: Audio will play through default speakers if VB-Cable is not available

---

## Technical Details

### Packet Loss Detection

The `UdpListener` tracks sequence numbers to detect lost packets:

- Each packet has a 32-bit sequence number (big-endian)
- Sequence numbers increment by 1 for each packet
- Gaps in sequence indicate packet loss
- Loss rate calculated as: `lost_packets / (received + lost)`

### Audio Pipeline

```
UDP Packet Reception
        ↓
Parse AudioPacket (extract sequence + PCM data)
        ↓
Add PCM data to NAudio BufferedWaveProvider
        ↓
NAudio WaveOut plays through speakers/VB-Cable
```

### Performance

- **CPU Usage**: <5% (target)
- **Memory**: <50MB (target)
- **Latency**: Network (10-100ms) + Playback (100ms) = ~110-200ms total

---

## Next Steps

### Phase 2: Service Discovery (mDNS)

- [ ] Add Zeroconf NuGet package
- [ ] Implement `DiscoveryService` - advertise `_micstream._udp.local`
- [ ] Add TXT records (version, name, capabilities)
- [ ] Auto-announce when app starts

### Phase 3: Opus Codec Decoder

- [ ] Add Concentus NuGet package
- [ ] Implement `OpusDecoderService`
- [ ] Update packet format: `[seq(4)][ts(4)][flags(1)][opus_data]`
- [ ] Implement Packet Loss Concealment (PLC)

### Phase 4: WPF UI

- [ ] Create WPF project (Windows-only)
- [ ] Design MainWindow.xaml
- [ ] Add system tray integration
- [ ] Visual status indicators

---

## Success Criteria (Phase 1)

### ✅ Completed

- [x] Receives UDP packets on port 5005
- [x] Parses packet format correctly
- [x] Detects packet loss
- [x] Audio playback pipeline implemented
- [x] VB-Cable detection works
- [x] Console UI functional

### 🔄 Testing Required

- [ ] Audio plays through speakers/VB-Cable
- [ ] Status indicator shows connection state
- [ ] Packet loss detection accurate
- [ ] No crashes during streaming

---

## License

GPL-3.0 (to match MicStream project and RNNoise dependency)

---

## References

- [MicStream Documentation](../design-docs/micstream/)
- [WINDOWS_AGENT.md](../design-docs/micstream/implementation/WINDOWS_AGENT.md)
- [NAudio Documentation](https://github.com/naudio/NAudio)
- [VB-Cable](https://vb-audio.com/Cable/)

---

## Contact

**Project**: MicStream - Gaming Voice Chat
**Developer**: Karthik
**Phase**: Phase 1 - Basic UDP Receiver + Audio Playback
**Status**: Implementation Complete ✓
