using System;
using NAudio.Wave;

namespace MicStreamReceiver.Services
{
    /// <summary>
    /// Audio Playback service using NAudio
    /// Plays received PCM audio through speakers or VB-Cable
    /// </summary>
    public class AudioPlayback : IDisposable
    {
        private IWavePlayer? _waveOut;
        private BufferedWaveProvider? _bufferProvider;
        private readonly WaveFormat _waveFormat;
        private readonly int _deviceNumber;

        public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
        public TimeSpan BufferedDuration => _bufferProvider?.BufferedDuration ?? TimeSpan.Zero;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="deviceNumber">Audio device number (-1 for default)</param>
        public AudioPlayback(int deviceNumber = -1)
        {
            // Audio format: 48kHz, 16-bit, Mono (as per spec)
            _waveFormat = new WaveFormat(48000, 16, 1);
            _deviceNumber = deviceNumber;
        }

        /// <summary>
        /// Initialize and start playback
        /// </summary>
        public void Start()
        {
            if (_waveOut != null)
            {
                return; // Already started
            }

            try
            {
                // Log which device we're using
                string deviceInfo = GetDeviceInfo(_deviceNumber);
                Console.WriteLine($"[AUDIO] Initializing playback on: {deviceInfo}");

                // Create buffered wave provider (2 second buffer capacity)
                _bufferProvider = new BufferedWaveProvider(_waveFormat)
                {
                    BufferDuration = TimeSpan.FromSeconds(2),
                    DiscardOnBufferOverflow = true
                };

                // Create wave output device
                // Use WaveOutEvent for better compatibility (doesn't require STA thread)
                _waveOut = new WaveOutEvent
                {
                    DeviceNumber = _deviceNumber,
                    DesiredLatency = 100 // 100ms desired latency
                };

                _waveOut.Init(_bufferProvider);
                _waveOut.Play();

                // Verify playback started
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[AUDIO] ✓ Playback started successfully on device #{_deviceNumber}");
                Console.ResetColor();

                if (_deviceNumber == -1)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[AUDIO] ⚠ Using default audio device (not VB-Cable)");
                    Console.WriteLine("[AUDIO]   Install VB-Cable for virtual microphone: https://vb-audio.com/Cable/");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start audio playback: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get device information for logging
        /// </summary>
        private string GetDeviceInfo(int deviceNumber)
        {
            if (deviceNumber == -1)
            {
                return "Default Device";
            }

            try
            {
                var capabilities = WaveOut.GetCapabilities(deviceNumber);
                return $"Device #{deviceNumber} - {capabilities.ProductName}";
            }
            catch
            {
                return $"Device #{deviceNumber} (name unavailable)";
            }
        }

        /// <summary>
        /// Add audio data to playback buffer
        /// </summary>
        /// <param name="audioData">PCM audio data</param>
        public void AddAudioData(byte[] audioData)
        {
            if (_bufferProvider == null || _waveOut == null)
            {
                throw new InvalidOperationException("Audio playback not started. Call Start() first.");
            }

            try
            {
                _bufferProvider.AddSamples(audioData, 0, audioData.Length);

                // Auto-start playback if stopped
                if (_waveOut.PlaybackState != PlaybackState.Playing)
                {
                    _waveOut.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding audio data: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop playback
        /// </summary>
        public void Stop()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;
            _bufferProvider = null;
        }

        /// <summary>
        /// Get buffer status information
        /// </summary>
        public string GetBufferStatus()
        {
            if (_bufferProvider == null)
            {
                return "Not initialized";
            }

            var bufferedMs = _bufferProvider.BufferedDuration.TotalMilliseconds;
            var bufferLength = _bufferProvider.BufferLength;
            var bufferedBytes = _bufferProvider.BufferedBytes;

            return $"Buffer: {bufferedMs:F0}ms ({bufferedBytes}/{bufferLength} bytes)";
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
