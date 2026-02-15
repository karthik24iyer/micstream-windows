#!/usr/bin/env python3
"""
MicStream UDP Test Sender
Sends test UDP packets to MicStreamReceiver for testing Phase 1

Usage:
    python3 test_udp_sender.py [host] [port]

Example:
    python3 test_udp_sender.py localhost 5005
"""

import socket
import struct
import time
import sys
import wave
import math

def generate_sine_wave(frequency=440, duration=0.02, sample_rate=48000):
    """
    Generate a sine wave PCM data
    Args:
        frequency: Frequency in Hz (440 = A4 note)
        duration: Duration in seconds (0.02 = 20ms)
        sample_rate: Sample rate in Hz (48000 = 48kHz)
    Returns:
        bytes: 16-bit PCM mono audio data
    """
    num_samples = int(sample_rate * duration)
    pcm_data = bytearray()

    for i in range(num_samples):
        # Generate sine wave
        sample = math.sin(2 * math.pi * frequency * i / sample_rate)
        # Convert to 16-bit signed integer (-32768 to 32767)
        sample_int = int(sample * 32767)
        # Pack as 16-bit little-endian
        pcm_data.extend(struct.pack('<h', sample_int))

    return bytes(pcm_data)

def generate_silence(duration=0.02, sample_rate=48000):
    """Generate silence (zeros)"""
    num_samples = int(sample_rate * duration)
    return b'\x00' * (num_samples * 2)  # 2 bytes per sample (16-bit)

def send_test_packets(host='localhost', port=5005, mode='sine', duration=10):
    """
    Send test UDP packets
    Args:
        host: Target host (default: localhost)
        port: Target port (default: 5005)
        mode: 'sine' for tone, 'silence' for silence
        duration: How long to send (seconds)
    """
    print(f"MicStream UDP Test Sender")
    print(f"=" * 50)
    print(f"Target: {host}:{port}")
    print(f"Mode: {mode}")
    print(f"Duration: {duration}s")
    print(f"Packet Format: [seq(4 bytes)][PCM data (960 samples)]")
    print(f"=" * 50)
    print()

    # Create UDP socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    target = (host, port)

    sequence = 0
    start_time = time.time()
    packets_sent = 0

    try:
        while True:
            # Check if duration elapsed
            elapsed = time.time() - start_time
            if elapsed >= duration:
                break

            # Generate audio data
            if mode == 'sine':
                # Generate 440Hz sine wave (20ms = 960 samples @ 48kHz)
                pcm_data = generate_sine_wave(frequency=440, duration=0.02)
            else:
                # Generate silence
                pcm_data = generate_silence(duration=0.02)

            # Create packet: [4 bytes sequence (big-endian)][PCM data]
            packet = struct.pack('>I', sequence) + pcm_data

            # Send packet
            sock.sendto(packet, target)

            packets_sent += 1
            sequence += 1

            # Print status every second (50 packets)
            if packets_sent % 50 == 0:
                print(f"[{elapsed:.1f}s] Sent {packets_sent} packets (seq: {sequence-1})")

            # Wait 20ms (50 packets per second)
            time.sleep(0.02)

    except KeyboardInterrupt:
        print("\nStopped by user")

    finally:
        elapsed = time.time() - start_time
        print()
        print(f"=" * 50)
        print(f"Summary:")
        print(f"  Total packets sent: {packets_sent}")
        print(f"  Duration: {elapsed:.2f}s")
        print(f"  Rate: {packets_sent/elapsed:.1f} packets/second")
        print(f"=" * 50)
        sock.close()

def main():
    # Parse command line arguments
    host = sys.argv[1] if len(sys.argv) > 1 else 'localhost'
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 5005
    mode = sys.argv[3] if len(sys.argv) > 3 else 'sine'
    duration = int(sys.argv[4]) if len(sys.argv) > 4 else 10

    # Validate mode
    if mode not in ['sine', 'silence']:
        print(f"Error: Invalid mode '{mode}'. Use 'sine' or 'silence'")
        sys.exit(1)

    # Send test packets
    send_test_packets(host, port, mode, duration)

if __name__ == '__main__':
    main()
