using System;

namespace MicStreamReceiver.Models
{
    /// <summary>
    /// Represents an audio packet received via UDP
    /// Phase 1 Format: [4 bytes: sequence][PCM data]
    /// Phase 3 Format: [4 bytes: seq][4 bytes: timestamp][1 byte: flags][Opus data]
    /// </summary>
    public class AudioPacket
    {
        public uint SequenceNumber { get; set; }
        public uint Timestamp { get; set; }
        public byte Flags { get; set; }
        public byte[] AudioData { get; set; }
        public bool IsOpusEncoded { get; set; }

        public bool HasNoiseSuppression => (Flags & 0x01) != 0;

        public AudioPacket()
        {
            AudioData = Array.Empty<byte>();
        }

        /// <summary>
        /// Parse raw UDP packet into AudioPacket
        /// Auto-detects Phase 1 (PCM) or Phase 3 (Opus) format
        /// </summary>
        public static AudioPacket Parse(byte[] data)
        {
            if (data.Length < 4)
            {
                throw new ArgumentException("Packet too small - must be at least 4 bytes");
            }

            // Check if this is Phase 3 format (has timestamp + flags header = 9 bytes minimum)
            if (data.Length >= 9 && IsLikelyPhase3Format(data))
            {
                return ParsePhase3(data);
            }
            else
            {
                return ParsePhase1(data);
            }
        }

        /// <summary>
        /// Heuristic to detect Phase 3 format
        /// Phase 3 has 9-byte header, Phase 1 has 4-byte header
        /// </summary>
        private static bool IsLikelyPhase3Format(byte[] data)
        {
            // Phase 3 packets are typically 169-209 bytes (9 header + 160-200 Opus data)
            // Phase 1 PCM packets are much larger (4 header + 1920 bytes PCM = 1924 bytes)
            return data.Length >= 9 && data.Length < 1000;
        }

        /// <summary>
        /// Parse Phase 1 format: [seq(4)][PCM data]
        /// </summary>
        private static AudioPacket ParsePhase1(byte[] data)
        {
            var packet = new AudioPacket
            {
                SequenceNumber = ReadUInt32BigEndian(data, 0),
                Timestamp = 0,
                Flags = 0,
                AudioData = new byte[data.Length - 4],
                IsOpusEncoded = false
            };

            Array.Copy(data, 4, packet.AudioData, 0, packet.AudioData.Length);
            return packet;
        }

        /// <summary>
        /// Parse Phase 3 format: [seq(4)][timestamp(4)][flags(1)][Opus data]
        /// </summary>
        private static AudioPacket ParsePhase3(byte[] data)
        {
            if (data.Length < 9)
            {
                throw new ArgumentException("Phase 3 packet too small - must be at least 9 bytes");
            }

            var packet = new AudioPacket
            {
                SequenceNumber = ReadUInt32BigEndian(data, 0),
                Timestamp = ReadUInt32BigEndian(data, 4),
                Flags = data[8],
                AudioData = new byte[data.Length - 9],
                IsOpusEncoded = true
            };

            Array.Copy(data, 9, packet.AudioData, 0, packet.AudioData.Length);
            return packet;
        }

        /// <summary>
        /// Read a 32-bit unsigned integer from byte array (big-endian)
        /// </summary>
        private static uint ReadUInt32BigEndian(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset] << 24) |
                   ((uint)buffer[offset + 1] << 16) |
                   ((uint)buffer[offset + 2] << 8) |
                   (uint)buffer[offset + 3];
        }

        /// <summary>
        /// Calculate latency in milliseconds
        /// </summary>
        public long CalculateLatency()
        {
            if (Timestamp == 0) return 0;

            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long sendTime = Timestamp;

            // Handle timestamp wraparound
            if (currentTime - sendTime > int.MaxValue)
            {
                sendTime += (1L << 32);
            }

            return currentTime - sendTime;
        }
    }
}
