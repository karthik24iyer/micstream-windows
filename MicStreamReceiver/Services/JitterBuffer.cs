using System;
using System.Collections.Generic;
using MicStreamReceiver.Models;

namespace MicStreamReceiver.Services
{
    /// <summary>
    /// Reorder Buffer — Phase 4 (revised)
    ///
    /// Fixes the original timer-based design:  System.Threading.Timer has ~15ms
    /// Windows resolution, so a 20ms tick fires irregularly, causing the buffer to
    /// pile up (observed 280ms) then underrun in a cycle.
    ///
    /// New approach: no timer at all.  Packets are emitted immediately as they
    /// arrive in-sequence.  Out-of-order packets wait in a SortedDictionary.
    /// If a gap isn't filled within GapTimeoutMs (60ms = 3 frames) it is declared
    /// lost and PLC is generated.  NAudio's hardware audio clock drives playback
    /// via BufferedWaveProvider — the only correct clock to follow.
    /// </summary>
    public class JitterBuffer : IDisposable
    {
        private const int FrameMs = 20;
        private const int GapTimeoutMs = 60;   // Wait up to 3 frames for a late packet
        private const int MaxReorderWindow = GapTimeoutMs / FrameMs;

        private readonly SortedDictionary<uint, AudioPacket> _buffer = new();
        private readonly object _lock = new();

        private uint _nextExpectedSeq;
        private uint _maxReceivedSeq;
        private bool _initialized;
        private DateTime _gapFirstSeen = DateTime.MinValue; // MinValue = no gap pending

        // Statistics
        private int _totalDropped;
        private int _totalPlcFrames;
        private int _totalReordered;

        public event EventHandler<AudioPacket>? PacketReady;
        public event EventHandler<int>? PacketsMissing;
        public event EventHandler<string>? StatusChanged;

        // Properties kept for API / stats compatibility with Program.cs
        public int BufferedMs { get { lock (_lock) return _buffer.Count * FrameMs; } }
        public int TargetBufferMs => GapTimeoutMs;
        public bool IsActive => _initialized;
        public int TotalDropped => _totalDropped;
        public int TotalPlcFrames => _totalPlcFrames;
        public int TotalReordered => _totalReordered;

        /// <summary>
        /// Add a received packet.  Emits all consecutive ready packets immediately;
        /// skips over gaps that have exceeded GapTimeoutMs.
        /// </summary>
        public void AddPacket(AudioPacket packet)
        {
            var toEmit = new List<AudioPacket>(4);
            int plcFrames = 0;

            lock (_lock)
            {
                if (!_initialized)
                {
                    _nextExpectedSeq = packet.SequenceNumber;
                    _maxReceivedSeq = packet.SequenceNumber;
                    _initialized = true;
                }

                // Track reordering for stats
                if (IsSequenceBefore(packet.SequenceNumber, _maxReceivedSeq))
                    _totalReordered++;
                else
                    _maxReceivedSeq = packet.SequenceNumber;

                // Drop duplicate
                if (_buffer.ContainsKey(packet.SequenceNumber))
                    return;

                // Drop packets that are already past the reorder window
                if (IsTooLate(packet.SequenceNumber))
                {
                    _totalDropped++;
                    return;
                }

                _buffer[packet.SequenceNumber] = packet;

                // Drain: emit consecutive packets, skip expired gaps
                while (true)
                {
                    if (_buffer.TryGetValue(_nextExpectedSeq, out var ready))
                    {
                        _buffer.Remove(_nextExpectedSeq);
                        _nextExpectedSeq = unchecked(_nextExpectedSeq + 1);
                        _gapFirstSeen = DateTime.MinValue;
                        toEmit.Add(ready);
                    }
                    else if (_buffer.Count > 0)
                    {
                        // Gap: _nextExpectedSeq is missing but later packets exist
                        if (_gapFirstSeen == DateTime.MinValue)
                            _gapFirstSeen = DateTime.Now;

                        if ((DateTime.Now - _gapFirstSeen).TotalMilliseconds >= GapTimeoutMs)
                        {
                            // Give up waiting — declare this frame lost
                            plcFrames++;
                            _totalPlcFrames++;
                            _nextExpectedSeq = unchecked(_nextExpectedSeq + 1);
                            _gapFirstSeen = DateTime.MinValue;
                            // Keep looping — the next seq may already be buffered
                        }
                        else
                        {
                            break; // Still within timeout, wait for next AddPacket call
                        }
                    }
                    else
                    {
                        break; // Buffer empty — fully caught up
                    }
                }
            }

            // Fire events outside the lock
            foreach (var p in toEmit) OnPacketReady(p);
            if (plcFrames > 0) OnPacketsMissing(plcFrames);
        }

        /// <summary>
        /// True when seq is more than MaxReorderWindow frames behind the play head.
        /// </summary>
        private bool IsTooLate(uint seq)
        {
            uint behind = unchecked(_nextExpectedSeq - seq);
            return behind > 0 && behind < 0x80000000 && behind > (uint)MaxReorderWindow;
        }

        /// <summary>
        /// True when seqA is strictly before seqB (wraparound-safe).
        /// </summary>
        private static bool IsSequenceBefore(uint seqA, uint seqB)
        {
            uint diff = unchecked(seqB - seqA);
            return diff > 0 && diff < 0x80000000;
        }

        public void Reset()
        {
            lock (_lock)
            {
                _buffer.Clear();
                _initialized = false;
                _nextExpectedSeq = 0;
                _maxReceivedSeq = 0;
                _gapFirstSeen = DateTime.MinValue;
                _totalDropped = 0;
                _totalPlcFrames = 0;
                _totalReordered = 0;
            }
        }

        public string GetStatus()
        {
            lock (_lock)
            {
                return $"JitterBuf: {_buffer.Count * FrameMs}ms pending | " +
                       $"PLC: {_totalPlcFrames} | Reordered: {_totalReordered} | Dropped: {_totalDropped}";
            }
        }

        protected virtual void OnPacketReady(AudioPacket packet) =>
            PacketReady?.Invoke(this, packet);

        protected virtual void OnPacketsMissing(int count) =>
            PacketsMissing?.Invoke(this, count);

        protected virtual void OnStatusChanged(string status) =>
            StatusChanged?.Invoke(this, status);

        public void Dispose() { /* no unmanaged resources */ }
    }
}
