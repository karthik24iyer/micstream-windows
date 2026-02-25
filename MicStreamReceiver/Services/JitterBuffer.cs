using System;
using System.Collections.Generic;
using System.Threading;
using MicStreamReceiver.Models;

namespace MicStreamReceiver.Services
{
    /// <summary>
    /// Adaptive Jitter Buffer — Phase 4
    /// Reorders out-of-order UDP packets and smooths network jitter.
    /// Buffer size adapts between 40ms and 80ms based on observed packet loss.
    /// </summary>
    public class JitterBuffer : IDisposable
    {
        private const int MinBufferMs = 40;
        private const int MaxBufferMs = 80;
        private const int FrameMs = 20;          // 20ms per Opus frame at 48kHz
        private const int MaxReorderWindow = 10; // Accept packets up to 10 seq behind play head

        private readonly SortedDictionary<uint, AudioPacket> _buffer = new();
        private readonly object _lock = new();
        private readonly Timer _outputTimer;

        private uint _nextExpectedSeq;
        private bool _initialized = false;
        private bool _timerStarted = false;
        private int _targetBufferMs;

        // Adaptive sizing — 1-second rolling window
        private int _windowLost = 0;
        private int _windowReceived = 0;
        private DateTime _lastAdaptTime = DateTime.Now;

        // Statistics
        private int _totalDropped = 0;
        private int _totalPlcFrames = 0;
        private int _totalReordered = 0;
        private uint _maxReceivedSeq = 0;

        // Events
        public event EventHandler<AudioPacket>? PacketReady;
        public event EventHandler<int>? PacketsMissing;  // arg = number of lost frames
        public event EventHandler<string>? StatusChanged;

        public int BufferedMs { get { lock (_lock) return _buffer.Count * FrameMs; } }
        public int TargetBufferMs => _targetBufferMs;
        public bool IsActive => _initialized;
        public int TotalDropped => _totalDropped;
        public int TotalPlcFrames => _totalPlcFrames;
        public int TotalReordered => _totalReordered;

        public JitterBuffer()
        {
            _targetBufferMs = MinBufferMs;
            _outputTimer = new Timer(OutputTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Add a received packet to the buffer.
        /// </summary>
        public void AddPacket(AudioPacket packet)
        {
            bool startTimer = false;
            string? statusMsg = null;

            lock (_lock)
            {
                // Initialize play head on first packet
                if (!_initialized)
                {
                    _nextExpectedSeq = packet.SequenceNumber;
                    _maxReceivedSeq = packet.SequenceNumber;
                    _initialized = true;
                }

                // Track reordering: arrived lower than max seen so far?
                if (IsSequenceBefore(packet.SequenceNumber, _maxReceivedSeq))
                    _totalReordered++;
                else
                    _maxReceivedSeq = packet.SequenceNumber;

                // Discard duplicates
                if (_buffer.ContainsKey(packet.SequenceNumber))
                    return;

                // Drop if already past the reorder window
                if (IsTooLate(packet.SequenceNumber))
                {
                    _totalDropped++;
                    return;
                }

                _buffer[packet.SequenceNumber] = packet;
                _windowReceived++;

                // Start output timer once we've pre-filled the target buffer
                if (!_timerStarted && _buffer.Count * FrameMs >= _targetBufferMs)
                {
                    _timerStarted = true;
                    startTimer = true;
                    statusMsg = $"Jitter buffer pre-filled ({_buffer.Count * FrameMs}ms), starting playback";
                }
            }

            // Start timer and fire status outside the lock
            if (startTimer)
                _outputTimer.Change(FrameMs, FrameMs);
            if (statusMsg != null)
                OnStatusChanged(statusMsg);
        }

        /// <summary>
        /// Timer callback — fires every 20ms to emit one frame.
        /// </summary>
        private void OutputTimerCallback(object? state)
        {
            AudioPacket? readyPacket = null;
            int plcCount = 0;
            string? statusMsg = null;
            bool stopTimer = false;

            lock (_lock)
            {
                if (!_initialized) return;

                // Adapt buffer size every second
                if ((DateTime.Now - _lastAdaptTime).TotalMilliseconds >= 1000)
                {
                    statusMsg = AdaptBufferSize();
                    _lastAdaptTime = DateTime.Now;
                    _windowLost = 0;
                    _windowReceived = 0;
                }

                if (_buffer.TryGetValue(_nextExpectedSeq, out var packet))
                {
                    // Normal case: packet is ready
                    _buffer.Remove(_nextExpectedSeq);
                    _nextExpectedSeq = unchecked(_nextExpectedSeq + 1);
                    readyPacket = packet;
                }
                else if (_buffer.Count > 0)
                {
                    // Future packets already in buffer → this one is lost; request PLC
                    _totalPlcFrames++;
                    _windowLost++;
                    _nextExpectedSeq = unchecked(_nextExpectedSeq + 1);
                    plcCount = 1;
                }
                else
                {
                    // Buffer drained (underrun) — stop timer and wait for re-fill
                    _timerStarted = false;
                    stopTimer = true;
                    statusMsg = $"Buffer underrun — re-filling to {_targetBufferMs}ms";
                }
            }

            // Fire events outside the lock to avoid blocking AddPacket
            if (stopTimer) _outputTimer.Change(Timeout.Infinite, Timeout.Infinite);
            if (readyPacket != null) OnPacketReady(readyPacket);
            if (plcCount > 0) OnPacketsMissing(plcCount);
            if (statusMsg != null) OnStatusChanged(statusMsg);
        }

        /// <summary>
        /// Adapt target buffer size based on 1-second loss rate.
        /// Returns a status message if the target changed, otherwise null.
        /// </summary>
        private string? AdaptBufferSize()
        {
            int total = _windowReceived + _windowLost;
            if (total == 0) return null;

            double lossRate = (double)_windowLost / total;

            if (lossRate > 0.05)
            {
                int prev = _targetBufferMs;
                _targetBufferMs = Math.Min(_targetBufferMs + 10, MaxBufferMs);
                if (_targetBufferMs != prev)
                    return $"Buffer increased to {_targetBufferMs}ms (loss {lossRate:P0})";
            }
            else if (lossRate < 0.01 && _targetBufferMs > MinBufferMs)
            {
                int prev = _targetBufferMs;
                _targetBufferMs = Math.Max(_targetBufferMs - 5, MinBufferMs);
                if (_targetBufferMs != prev)
                    return $"Buffer reduced to {_targetBufferMs}ms (loss {lossRate:P0})";
            }

            return null;
        }

        /// <summary>
        /// Returns true if seq is more than MaxReorderWindow behind the play head.
        /// </summary>
        private bool IsTooLate(uint seq)
        {
            uint behind = unchecked(_nextExpectedSeq - seq);
            // 'behind' is small+positive when seq < _nextExpectedSeq; huge when seq > _nextExpectedSeq
            return behind > 0 && behind < 0x80000000 && behind > (uint)MaxReorderWindow;
        }

        /// <summary>
        /// Returns true if seqA is strictly before seqB (wraparound-safe).
        /// </summary>
        private static bool IsSequenceBefore(uint seqA, uint seqB)
        {
            uint diff = unchecked(seqB - seqA);
            return diff > 0 && diff < 0x80000000;
        }

        /// <summary>
        /// Reset buffer state (call on stream stop/restart).
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _outputTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _buffer.Clear();
                _initialized = false;
                _timerStarted = false;
                _nextExpectedSeq = 0;
                _maxReceivedSeq = 0;
                _targetBufferMs = MinBufferMs;
                _windowLost = 0;
                _windowReceived = 0;
                _totalDropped = 0;
                _totalPlcFrames = 0;
                _totalReordered = 0;
            }
        }

        public string GetStatus()
        {
            lock (_lock)
            {
                return $"JitterBuf: {_buffer.Count * FrameMs}ms/{_targetBufferMs}ms target | " +
                       $"PLC: {_totalPlcFrames} | Reordered: {_totalReordered} | Dropped: {_totalDropped}";
            }
        }

        protected virtual void OnPacketReady(AudioPacket packet) =>
            PacketReady?.Invoke(this, packet);

        protected virtual void OnPacketsMissing(int count) =>
            PacketsMissing?.Invoke(this, count);

        protected virtual void OnStatusChanged(string status) =>
            StatusChanged?.Invoke(this, status);

        public void Dispose()
        {
            _outputTimer.Dispose();
        }
    }
}
