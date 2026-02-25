using System;
using System.Threading;
using MicStreamReceiver.Services;
using MicStreamReceiver.Models;

namespace MicStreamReceiver
{
    /// <summary>
    /// MicStream Receiver - Phase 4
    /// Receives UDP audio packets (PCM or Opus), passes through adaptive jitter buffer,
    /// decodes with Opus + PLC, and plays through speakers/VB-Cable.
    /// </summary>
    class Program
    {
        private static UdpListener? _udpListener;
        private static AudioPlayback? _audioPlayback;
        private static VirtualDeviceManager? _deviceManager;
        private static DiscoveryService? _discoveryService;
        private static OpusDecoderService? _opusDecoder;
        private static JitterBuffer? _jitterBuffer;
        private static bool _isRunning = false;
        private static bool _isOpusMode = false;

        // Statistics
        private static int _totalPacketsReceived = 0;
        private static int _opusPacketsDecoded = 0;
        private static DateTime _startTime;

        static void Main(string[] args)
        {
            // Initialize logger first - redirects all Console output to both console and file
            Logger.Initialize();

            Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║        MicStream Receiver - Phase 4                   ║");
            Console.WriteLine("║        Jitter Buffer + Opus + PLC + mDNS              ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
            Console.WriteLine($"Log file: {Logger.LogFilePath}");
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
            _udpListener.PacketReceived += OnRawPacketReceived;
            _udpListener.StatusChanged += OnStatusChanged;

            // Initialize audio playback
            _audioPlayback = new AudioPlayback(deviceNumber);

            // Initialize Opus decoder (Phase 3)
            _opusDecoder = new OpusDecoderService(48000, 1);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Opus decoder initialized (48kHz, mono)");
            Console.ResetColor();

            // Initialize jitter buffer (Phase 4)
            _jitterBuffer = new JitterBuffer();
            _jitterBuffer.PacketReady += OnPacketReceived;
            _jitterBuffer.PacketsMissing += OnJitterPacketsMissing;
            _jitterBuffer.StatusChanged += OnJitterStatusChanged;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Reorder buffer initialized (60ms gap timeout, no timer)");
            Console.ResetColor();
            Console.WriteLine();

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
            _opusDecoder?.Dispose();
            _jitterBuffer?.Dispose();
            Console.WriteLine("\nShutting down...");
            Logger.Close();
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

                Console.WriteLine("[STARTING] Resetting jitter buffer...");
                _jitterBuffer?.Reset();
                _isOpusMode = false;

                Console.WriteLine("[STARTING] Starting UDP listener...");
                _udpListener?.Start();

                _isRunning = true;
                _startTime = DateTime.Now;
                _totalPacketsReceived = 0;
                _opusPacketsDecoded = 0;

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

            Console.WriteLine("[STOPPING] Resetting jitter buffer...");
            _jitterBuffer?.Reset();

            Console.WriteLine("[STOPPING] Stopping audio playback...");
            _audioPlayback?.Stop();

            _isRunning = false;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("✓ Streaming stopped");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Called by UdpListener — feeds raw packets into the jitter buffer.
        /// </summary>
        static void OnRawPacketReceived(object? sender, AudioPacket packet)
        {
            _jitterBuffer?.AddPacket(packet);
        }

        /// <summary>
        /// Called by JitterBuffer when a reordered/timed packet is ready to play.
        /// </summary>
        static void OnPacketReceived(object? sender, AudioPacket packet)
        {
            try
            {
                byte[] pcmData;

                if (packet.IsOpusEncoded)
                {
                    try
                    {
                        pcmData = _opusDecoder!.Decode(packet.AudioData);
                        _isOpusMode = true;
                        _opusPacketsDecoded++;

                        if (_opusPacketsDecoded == 1)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"[OPUS] Decoding started — latency: {packet.CalculateLatency()}ms, NS: {(packet.HasNoiseSuppression ? "ON" : "OFF")}");
                            Console.ResetColor();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[OPUS] Decode failed: {ex.Message}");
                        Console.ResetColor();
                        return;
                    }
                }
                else
                {
                    // Phase 1 fallback: raw PCM
                    pcmData = packet.AudioData;
                }

                _audioPlayback?.AddAudioData(pcmData);

                _totalPacketsReceived++;

                // Periodic stats (every 50 packets ≈ 1 second at 50pps)
                if (_totalPacketsReceived % 50 == 0)
                {
                    var uptime = DateTime.Now - _startTime;
                    var packetsPerSecond = _totalPacketsReceived / uptime.TotalSeconds;
                    var lossRate = _udpListener?.PacketLossRate ?? 0;
                    var bufferStatus = _audioPlayback?.GetBufferStatus() ?? "N/A";
                    var jitterStatus = _jitterBuffer?.GetStatus() ?? "N/A";
                    var latency = packet.IsOpusEncoded ? packet.CalculateLatency() : 0;
                    var format = packet.IsOpusEncoded ? "Opus" : "PCM";

                    Console.WriteLine($"[STATS] Packets: {_totalPacketsReceived} | " +
                                    $"Format: {format} | " +
                                    $"Rate: {packetsPerSecond:F1}/s | " +
                                    $"Loss: {lossRate:P1} | " +
                                    (latency > 0 ? $"Latency: {latency}ms | " : "") +
                                    $"{bufferStatus}");
                    Console.WriteLine($"        {jitterStatus}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to process packet: {ex.Message}");
            }
        }

        /// <summary>
        /// Called by JitterBuffer when packet(s) are missing — generate PLC audio.
        /// </summary>
        static void OnJitterPacketsMissing(object? sender, int count)
        {
            if (!_isOpusMode) return; // No PLC for raw PCM

            try
            {
                for (int i = 0; i < count; i++)
                {
                    byte[] plcData = _opusDecoder!.GeneratePlc();
                    _audioPlayback?.AddAudioData(plcData);
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[PLC] Generated {count} frame(s) for packet loss");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLC] Error generating PLC: {ex.Message}");
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

        static void OnJitterStatusChanged(object? sender, string status)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"[JITTER] {status}");
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
            var opusStats = _opusDecoder?.GetStatistics() ?? "N/A";
            var jitterStatus = _jitterBuffer?.GetStatus() ?? "N/A";

            Console.WriteLine($"  Uptime:           {uptime:hh\\:mm\\:ss}");
            Console.WriteLine($"  Connection:       {(isConnected ? "CONNECTED" : "DISCONNECTED")}");
            Console.WriteLine($"  Packets Received: {packetsReceived}");
            Console.WriteLine($"  Opus Packets:     {_opusPacketsDecoded}");
            Console.WriteLine($"  Packets Lost:     {packetsLost}");
            Console.WriteLine($"  Loss Rate:        {lossRate:P2}");
            Console.WriteLine($"  Opus Decoder:     {opusStats}");
            Console.WriteLine($"  Playback Status:  {(isPlaying ? "PLAYING" : "STOPPED")}");
            Console.WriteLine($"  {bufferStatus}");
            Console.WriteLine($"  {jitterStatus}");
            Console.WriteLine($"  Gap Timeout:      {_jitterBuffer?.TargetBufferMs ?? 0}ms");
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
