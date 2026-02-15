using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace MicStreamReceiver.Services
{
    /// <summary>
    /// Virtual Device Manager - detects and manages VB-Cable virtual audio device
    /// </summary>
    public class VirtualDeviceManager
    {
        /// <summary>
        /// Check if VB-Cable is installed
        /// </summary>
        public bool IsVbCableInstalled()
        {
            return GetVbCableDeviceNumber() >= 0;
        }

        /// <summary>
        /// Get VB-Cable device number
        /// </summary>
        /// <returns>Device number, or -1 if not found</returns>
        public int GetVbCableDeviceNumber()
        {
            try
            {
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var capabilities = WaveOut.GetCapabilities(i);

                    // Look for VB-Cable Input device
                    if (capabilities.ProductName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase) ||
                        capabilities.ProductName.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting VB-Cable: {ex.Message}");
            }

            return -1;
        }

        /// <summary>
        /// Get VB-Cable device name
        /// </summary>
        public string GetVbCableDeviceName()
        {
            int deviceNumber = GetVbCableDeviceNumber();

            if (deviceNumber >= 0)
            {
                try
                {
                    var capabilities = WaveOut.GetCapabilities(deviceNumber);
                    return capabilities.ProductName;
                }
                catch
                {
                    return "Not found";
                }
            }

            return "Not found";
        }

        /// <summary>
        /// List all available audio output devices
        /// </summary>
        public List<AudioDeviceInfo> GetAllAudioDevices()
        {
            var devices = new List<AudioDeviceInfo>();

            try
            {
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var capabilities = WaveOut.GetCapabilities(i);

                    devices.Add(new AudioDeviceInfo
                    {
                        DeviceNumber = i,
                        Name = capabilities.ProductName,
                        Channels = capabilities.Channels,
                        IsVbCable = capabilities.ProductName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase) ||
                                   capabilities.ProductName.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing audio devices: {ex.Message}");
            }

            return devices;
        }

        /// <summary>
        /// Get default audio device number
        /// </summary>
        public int GetDefaultDeviceNumber()
        {
            return -1; // -1 means default device in NAudio
        }

        /// <summary>
        /// Get recommended device number (VB-Cable if available, otherwise default)
        /// </summary>
        public int GetRecommendedDeviceNumber()
        {
            int vbCableDevice = GetVbCableDeviceNumber();
            return vbCableDevice >= 0 ? vbCableDevice : GetDefaultDeviceNumber();
        }

        /// <summary>
        /// Print device information to console
        /// </summary>
        public void PrintDeviceInfo()
        {
            Console.WriteLine("\n=== Audio Devices ===");

            var devices = GetAllAudioDevices();

            if (devices.Count == 0)
            {
                Console.WriteLine("No audio devices found!");
                return;
            }

            foreach (var device in devices)
            {
                string marker = device.IsVbCable ? " [VB-CABLE] ← Virtual Microphone" : "";
                Console.WriteLine($"  [{device.DeviceNumber}] {device.Name} ({device.Channels} channels){marker}");
            }

            Console.WriteLine();

            // VB-Cable status
            if (IsVbCableInstalled())
            {
                int vbDevice = GetVbCableDeviceNumber();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ VB-Cable detected: {GetVbCableDeviceName()}");
                Console.ResetColor();
                Console.WriteLine($"  Device Number: #{vbDevice}");
                Console.WriteLine($"  This will be used as virtual microphone in Discord/Games");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  How to use in Discord:");
                Console.WriteLine($"    1. Open Discord → Settings → Voice & Video");
                Console.WriteLine($"    2. Input Device → Select 'CABLE Output'");
                Console.WriteLine($"    3. Your phone mic will work in Discord!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("✗ VB-Cable not detected");
                Console.ResetColor();
                Console.WriteLine("  Install from: https://vb-audio.com/Cable/");
                Console.WriteLine("  Without VB-Cable: Audio plays through speakers (not a virtual mic)");
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Audio device information
    /// </summary>
    public class AudioDeviceInfo
    {
        public int DeviceNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Channels { get; set; }
        public bool IsVbCable { get; set; }
    }
}
