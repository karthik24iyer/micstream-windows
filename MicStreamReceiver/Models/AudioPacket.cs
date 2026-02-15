using System;

namespace MicStreamReceiver.Models
{
    /// <summary>
    /// Represents an audio packet received via UDP
    /// Phase 1 Format: [4 bytes: sequence][PCM data]
    /// </summary>
    public class AudioPacket
    {
        public uint SequenceNumber { get; set; }
        public byte[] PcmData { get; set; }

        public AudioPacket()
        {
            PcmData = Array.Empty<byte>();
        }

        /// <summary>
        /// Parse raw UDP packet into AudioPacket
        /// </summary>
        /// <param name="data">Raw packet data</param>
        /// <returns>Parsed AudioPacket</returns>
        public static AudioPacket Parse(byte[] data)
        {
            if (data.Length < 4)
            {
                throw new ArgumentException("Packet too small - must be at least 4 bytes");
            }

            var packet = new AudioPacket
            {
                // Read sequence number (4 bytes, big-endian)
                SequenceNumber = ReadUInt32BigEndian(data, 0),

                // Extract PCM data (remaining bytes)
                PcmData = new byte[data.Length - 4]
            };

            Array.Copy(data, 4, packet.PcmData, 0, packet.PcmData.Length);

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
    }
}
