using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace MicStreamReceiver.Services
{
    /// <summary>
    /// Diagnostic tool to debug audio device detection and routing
    /// </summary>
    public class AudioDeviceDiagnostics
    {
        /// <summary>
        /// Print detailed information about all audio devices
        /// </summary>
        public static void PrintDetailedDeviceInfo()
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║         AUDIO DEVICE DIAGNOSTICS                      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                Console.WriteLine("Total Output Devices Found: " + WaveOut.DeviceCount);
                Console.WriteLine();

                if (WaveOut.DeviceCount == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: No audio output devices found!");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine("Detailed Device List:");
                Console.WriteLine("─────────────────────────────────────────────────────");

                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    try
                    {
                        var capabilities = WaveOut.GetCapabilities(i);

                        Console.WriteLine($"\nDevice #{i}:");
                        Console.WriteLine($"  Name:     \"{capabilities.ProductName}\"");
                        Console.WriteLine($"  Channels: {capabilities.Channels}");
                        Console.WriteLine($"  Driver:   {capabilities.NameGuid}");

                        // Check if this is VB-Cable
                        bool isVbCable = capabilities.ProductName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase) ||
                                        capabilities.ProductName.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase);

                        if (isVbCable)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"  *** VB-CABLE DETECTED ***");
                            Console.WriteLine($"  This device should be used for virtual microphone!");
                            Console.ResetColor();
                        }

                        // Check exact name match
                        if (capabilities.ProductName.Equals("CABLE Input", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"  ✓ EXACT MATCH: \"CABLE Input\"");
                            Console.ResetColor();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nDevice #{i}:");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ERROR: {ex.Message}");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine();
                Console.WriteLine("─────────────────────────────────────────────────────");
                Console.WriteLine();

                // Test VB-Cable detection
                Console.WriteLine("VB-Cable Detection Test:");
                int vbCableDevice = FindVbCableDevice();

                if (vbCableDevice >= 0)
                {
                    var caps = WaveOut.GetCapabilities(vbCableDevice);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ VB-Cable Found!");
                    Console.ResetColor();
                    Console.WriteLine($"  Device Number: #{vbCableDevice}");
                    Console.WriteLine($"  Device Name:   \"{caps.ProductName}\"");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"MicStream WILL USE: Device #{vbCableDevice}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ VB-Cable NOT Detected");
                    Console.ResetColor();
                    Console.WriteLine($"  MicStream will use: Device #-1 (Default/Speakers)");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Possible reasons:");
                    Console.WriteLine($"  1. VB-Cable not installed");
                    Console.WriteLine($"  2. Device name doesn't match expected patterns");
                    Console.WriteLine($"  3. Audio services need restart");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Failed to enumerate audio devices");
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();
        }

        /// <summary>
        /// Find VB-Cable device using same logic as VirtualDeviceManager
        /// </summary>
        private static int FindVbCableDevice()
        {
            try
            {
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var capabilities = WaveOut.GetCapabilities(i);

                    // Same logic as VirtualDeviceManager
                    if (capabilities.ProductName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase) ||
                        capabilities.ProductName.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FindVbCableDevice: {ex.Message}");
            }

            return -1;
        }

        /// <summary>
        /// Test audio output to a specific device
        /// </summary>
        public static void TestAudioOutput(int deviceNumber)
        {
            Console.WriteLine($"\n[DIAGNOSTIC] Testing audio output to device #{deviceNumber}...");

            try
            {
                if (deviceNumber >= 0)
                {
                    var caps = WaveOut.GetCapabilities(deviceNumber);
                    Console.WriteLine($"[DIAGNOSTIC] Target device: \"{caps.ProductName}\"");
                }
                else
                {
                    Console.WriteLine($"[DIAGNOSTIC] Target device: Default");
                }

                // Create a simple test tone
                var waveFormat = new WaveFormat(48000, 16, 1);
                var waveOut = new WaveOutEvent { DeviceNumber = deviceNumber };

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[DIAGNOSTIC] ✓ Successfully initialized device #{deviceNumber}");
                Console.ResetColor();

                waveOut.Dispose();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DIAGNOSTIC] ✗ Failed to initialize device #{deviceNumber}");
                Console.WriteLine($"[DIAGNOSTIC] Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
