using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MicStreamReceiver.Models;

namespace MicStreamReceiver.Services
{
    /// <summary>
    /// UDP Listener service - receives audio packets on port 5005
    /// </summary>
    public class UdpListener : IDisposable
    {
        private readonly int _port;
        private UdpClient? _udpClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _receiveTask;

        // Statistics
        private uint _lastSequenceNumber;
        private int _packetsReceived;
        private int _packetsLost;
        private DateTime _lastPacketTime;
        private bool _isFirstPacket = true;

        // Events
        public event EventHandler<AudioPacket>? PacketReceived;
        public event EventHandler<string>? StatusChanged;

        public bool IsRunning { get; private set; }
        public int PacketsReceived => _packetsReceived;
        public int PacketsLost => _packetsLost;
        public double PacketLossRate => _packetsReceived > 0 ? (double)_packetsLost / (_packetsReceived + _packetsLost) : 0;

        public UdpListener(int port = 5005)
        {
            _port = port;
            _lastPacketTime = DateTime.Now;
        }

        /// <summary>
        /// Start listening for UDP packets
        /// </summary>
        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            try
            {
                _udpClient = new UdpClient(_port)
                {
                    Client =
                    {
                        ReceiveBufferSize = 1048576 // 1MB buffer (increased from 64KB to handle burst traffic)
                    }
                };

                _cancellationTokenSource = new CancellationTokenSource();
                _receiveTask = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token));

                IsRunning = true;
                OnStatusChanged($"Listening on port {_port}");
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Error starting listener: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stop listening for UDP packets
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            _cancellationTokenSource?.Cancel();
            _receiveTask?.Wait(TimeSpan.FromSeconds(2));

            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;

            IsRunning = false;
            OnStatusChanged("Stopped listening");
        }

        /// <summary>
        /// Main receive loop
        /// </summary>
        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            OnStatusChanged("Ready to receive packets...");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient!.ReceiveAsync(cancellationToken);
                    _lastPacketTime = DateTime.Now;

                    // Parse packet
                    var packet = AudioPacket.Parse(result.Buffer);

                    // Detect packet loss
                    DetectPacketLoss(packet.SequenceNumber);

                    // Update statistics
                    _packetsReceived++;

                    // Raise event
                    OnPacketReceived(packet);
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation
                    break;
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"Error receiving packet: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Detect packet loss based on sequence numbers
        /// </summary>
        private void DetectPacketLoss(uint currentSequence)
        {
            if (_isFirstPacket)
            {
                _isFirstPacket = false;
                _lastSequenceNumber = currentSequence;
                return;
            }

            // Calculate expected sequence (handling wraparound)
            uint expectedSequence = _lastSequenceNumber + 1;

            // Check for packet loss
            if (currentSequence != expectedSequence)
            {
                // Calculate how many packets were lost
                uint gap = currentSequence - expectedSequence;

                // Handle wraparound (if gap is huge, it probably wrapped)
                if (gap > 0x80000000) // > 2^31
                {
                    gap = (uint.MaxValue - expectedSequence) + currentSequence + 1;
                }

                if (gap > 0 && gap < 1000) // Sanity check
                {
                    _packetsLost += (int)gap;
                    OnStatusChanged($"Packet loss detected: {gap} packet(s) lost");
                }
            }

            _lastSequenceNumber = currentSequence;
        }

        /// <summary>
        /// Check if connection is alive (packets received recently)
        /// </summary>
        public bool IsConnected()
        {
            return IsRunning && (DateTime.Now - _lastPacketTime).TotalSeconds < 3;
        }

        protected virtual void OnPacketReceived(AudioPacket packet)
        {
            PacketReceived?.Invoke(this, packet);
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
