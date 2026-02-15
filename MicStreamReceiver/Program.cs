using System;
using System.Threading;
using MicStreamReceiver.Services;
using MicStreamReceiver.Models;

namespace MicStreamReceiver
{
    /// <summary>
    /// MicStream Receiver - Phase 2
    /// Receives UDP audio packets and plays through speakers/VB-Cable
    /// Includes mDNS service discovery for auto-connection
    /// </summary>
    class Program
    {
        private static UdpListener? _udpListener;
        private static AudioPlayback? _audioPlayback;
        private static VirtualDeviceManager? _deviceManager;
        private static DiscoveryService? _discoveryService;
        private static bool _isRunning = false;

        // Statistics
        private static int _totalPacketsReceived = 0;
        private static DateTime _startTime;

        static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║        MicStream Receiver - Phase 2                   ║");
            Console.WriteLine("║        Low-Latency Audio Streaming + mDNS Discovery   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Initialize services
            _deviceManager = new VirtualDeviceManager();
            _deviceManager.PrintDeviceInfo();

            // Run detailed diagnostics
            AudioDeviceDiagnostics.PrintDetailedDeviceInfo();

            // Get recommended device (VB-Cable if available, otherwise default)
            int deviceNumber = _deviceManager.GetRecommendedDeviceNumber();
            string deviceName = deviceNumber >= 0
                ? _deviceManager.GetAllAudioDevices().Find(d => d.DeviceNumber == deviceNumber)?.Name ?? "Default"
                : "Default";

            Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║           Audio Output Configuration                  ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");

            if (deviceNumber >= 0 && deviceName.Contains("CABLE", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Selected: {deviceName} (Device #{deviceNumber})");
                Console.ResetColor();
                Console.WriteLine($"  Mode: Virtual Microphone");
                Console.WriteLine($"  Audio will route through VB-Cable");
                Console.WriteLine($"  Use 'CABLE Output' as mic in Discord/Games");
            }
            else if (deviceNumber == -1)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Selected: Default Device");
                Console.ResetColor();
                Console.WriteLine($"  Mode: Speaker Playback");
                Console.WriteLine($"  Audio will play through your default speakers");
                Console.WriteLine($"  Install VB-Cable for virtual microphone functionality");
            }
            else
            {
                Console.WriteLine($"Selected: {deviceName} (Device #{deviceNumber})");
            }
            Console.WriteLine();

            // Initialize mDNS discovery service
            _discoveryService = new DiscoveryService(5005);
            _discoveryService.StatusChanged += OnDiscoveryStatusChanged;
            _discoveryService.PrintDiscoveryInfo();

            // Initialize UDP listener
            _udpListener = new UdpListener(5005);
            _udpListener.PacketReceived += OnPacketReceived;
            _udpListener.StatusChanged += OnStatusChanged;

            // Initialize audio playback
            _audioPlayback = new AudioPlayback(deviceNumber);

            // Show menu
            ShowMenu();

            // Main loop
            bool exit = false;
            while (!exit)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.S:
                        StartStreaming();
                        break;

                    case ConsoleKey.T:
                        StopStreaming();
                        break;

                    case ConsoleKey.D:
                        _deviceManager.PrintDeviceInfo();
                        break;

                    case ConsoleKey.I:
                        ShowStats();
                        break;

                    case ConsoleKey.M:
                        _discoveryService?.PrintDiscoveryInfo();
                        break;

                    case ConsoleKey.X:
                        AudioDeviceDiagnostics.PrintDetailedDeviceInfo();
                        break;

                    case ConsoleKey.H:
                        ShowMenu();
                        break;

                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        exit = true;
                        break;
                }
            }

            // Cleanup
            StopStreaming();
            _discoveryService?.StopAdvertising();
            _discoveryService?.Dispose();
            Console.WriteLine("\nShutting down...");
        }

        static async void StartStreaming()
        {
            if (_isRunning)
            {
                Console.WriteLine("[INFO] Already streaming");
                return;
            }

            try
            {
                Console.WriteLine("\n[STARTING] Starting mDNS advertisement...");
                await _discoveryService!.StartAdvertising();

                Console.WriteLine("[STARTING] Initializing audio playback...");
                _audioPlayback?.Start();

                Console.WriteLine("[STARTING] Starting UDP listener...");
                _udpListener?.Start();

                _isRunning = true;
                _startTime = DateTime.Now;
                _totalPacketsReceived = 0;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Streaming started successfully!");
                Console.ResetColor();
                Console.WriteLine("Listening on UDP port 5005");
                Console.WriteLine($"Advertising as: {_discoveryService?.InstanceName}");
                Console.WriteLine("Waiting for audio packets from Android phone...");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error starting streaming: {ex.Message}");
                Console.ResetColor();
                _isRunning = false;
            }
        }

        static void StopStreaming()
        {
            if (!_isRunning)
            {
                Console.WriteLine("[INFO] Not currently streaming");
                return;
            }

            Console.WriteLine("\n[STOPPING] Stopping mDNS advertisement...");
            _discoveryService?.StopAdvertising();

            Console.WriteLine("[STOPPING] Stopping UDP listener...");
            _udpListener?.Stop();

            Console.WriteLine("[STOPPING] Stopping audio playback...");
            _audioPlayback?.Stop();

            _isRunning = false;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("✓ Streaming stopped");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void OnPacketReceived(object? sender, AudioPacket packet)
        {
            try
            {
                // Add audio data to playback buffer
                _audioPlayback?.AddAudioData(packet.PcmData);

                _totalPacketsReceived++;

                // Show periodic status (every 50 packets = ~1 second)
                if (_totalPacketsReceived % 50 == 0)
                {
                    var uptime = DateTime.Now - _startTime;
                    var packetsPerSecond = _totalPacketsReceived / uptime.TotalSeconds;
                    var lossRate = _udpListener?.PacketLossRate ?? 0;
                    var bufferStatus = _audioPlayback?.GetBufferStatus() ?? "N/A";

                    Console.WriteLine($"[STATS] Packets: {_totalPacketsReceived} | " +
                                    $"Rate: {packetsPerSecond:F1}/s | " +
                                    $"Loss: {lossRate:P1} | " +
                                    $"{bufferStatus}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to process packet: {ex.Message}");
            }
        }

        static void OnStatusChanged(object? sender, string status)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[UDP] {status}");
            Console.ResetColor();
        }

        static void OnDiscoveryStatusChanged(object? sender, string status)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[mDNS] {status}");
            Console.ResetColor();
        }

        static void ShowStats()
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   Statistics                          ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");

            if (!_isRunning)
            {
                Console.WriteLine("  Not currently streaming");
                Console.WriteLine();
                return;
            }

            var uptime = DateTime.Now - _startTime;
            var packetsReceived = _udpListener?.PacketsReceived ?? 0;
            var packetsLost = _udpListener?.PacketsLost ?? 0;
            var lossRate = _udpListener?.PacketLossRate ?? 0;
            var isConnected = _udpListener?.IsConnected() ?? false;
            var bufferStatus = _audioPlayback?.GetBufferStatus() ?? "N/A";
            var isPlaying = _audioPlayback?.IsPlaying ?? false;

            Console.WriteLine($"  Uptime:           {uptime:hh\\:mm\\:ss}");
            Console.WriteLine($"  Connection:       {(isConnected ? "CONNECTED" : "DISCONNECTED")}");
            Console.WriteLine($"  Packets Received: {packetsReceived}");
            Console.WriteLine($"  Packets Lost:     {packetsLost}");
            Console.WriteLine($"  Loss Rate:        {lossRate:P2}");
            Console.WriteLine($"  Playback Status:  {(isPlaying ? "PLAYING" : "STOPPED")}");
            Console.WriteLine($"  {bufferStatus}");
            Console.WriteLine();
        }

        static void ShowMenu()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    Controls                           ║");
            Console.WriteLine("╠═══════════════════════════════════════════════════════╣");
            Console.WriteLine("║  [S] Start Listening (+ mDNS Advertisement)           ║");
            Console.WriteLine("║  [T] Stop Listening                                   ║");
            Console.WriteLine("║  [D] Show Audio Devices                               ║");
            Console.WriteLine("║  [M] Show mDNS Discovery Info                         ║");
            Console.WriteLine("║  [X] Run Audio Diagnostics (Troubleshooting)          ║");
            Console.WriteLine("║  [I] Show Statistics                                  ║");
            Console.WriteLine("║  [H] Show This Menu                                   ║");
            Console.WriteLine("║  [Q] Quit                                             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
            Console.WriteLine();
        }
    }
}
