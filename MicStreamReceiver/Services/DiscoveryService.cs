using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Makaretu.Dns;

namespace MicStreamReceiver.Services
{
    /// <summary>
    /// mDNS Service Discovery - Advertises this PC on the network
    /// Android apps can discover this service via _micstream._udp.local
    /// </summary>
    public class DiscoveryService : IDisposable
    {
        private const string ServiceType = "_micstream._udp";
        private const string Version = "1.0";

        private readonly int _port;
        private readonly string _instanceName;
        private MulticastService? _mdns;
        private ServiceDiscovery? _sd;
        private bool _isAdvertising = false;

        public event EventHandler<string>? StatusChanged;

        public DiscoveryService(int port = 5005, string? instanceName = null)
        {
            _port = port;
            _instanceName = instanceName ?? Environment.MachineName;
        }

        public async Task StartAdvertising()
        {
            if (_isAdvertising) return;

            var ipAddresses = GetLocalIPAddresses();
            OnStatusChanged($"Starting mDNS advertisement on {ipAddresses.Count} interface(s)");

            // Build service profile
            var profile = new ServiceProfile(_instanceName, ServiceType, (ushort)_port);
            profile.AddProperty("version", Version);
            profile.AddProperty("name", _instanceName);
            profile.AddProperty("capabilities", "opus,rnnoise");

            OnStatusChanged("mDNS Service Configuration:");
            OnStatusChanged($"  Service Type: {ServiceType}.local");
            OnStatusChanged($"  Instance Name: {_instanceName}");
            OnStatusChanged($"  Port: {_port}");
            OnStatusChanged($"  IP Addresses: {string.Join(", ", ipAddresses)}");
            OnStatusChanged("  TXT Records:");
            OnStatusChanged($"    version={Version}");
            OnStatusChanged($"    name={_instanceName}");
            OnStatusChanged("    capabilities=opus,rnnoise");

            // Start real mDNS responder, excluding USB tethering (RNDIS) adapters.
            // When the Android phone is plugged in via USB for sideloading, Windows
            // gains a 192.168.143.x tethering interface. Without this filter,
            // Makaretu.Dns advertises on ALL interfaces and Android's NSD picks up
            // the tethering IP instead of the WiFi IP, making the connection fail.
            _mdns = new MulticastService(all => all.Where(ni =>
                !ni.Description.Contains("Remote NDIS", StringComparison.OrdinalIgnoreCase) &&
                !ni.Description.Contains("RNDIS", StringComparison.OrdinalIgnoreCase) &&
                !ni.Description.Contains("Android USB", StringComparison.OrdinalIgnoreCase)));
            _sd = new ServiceDiscovery(_mdns);
            _sd.Advertise(profile);
            _mdns.Start();

            _isAdvertising = true;
            OnStatusChanged($"√ Advertising as: {_instanceName}");
            OnStatusChanged("  Android devices can discover this PC via mDNS");

            await Task.CompletedTask;
        }

        public void StopAdvertising()
        {
            if (!_isAdvertising) return;

            _mdns?.Stop();
            _sd?.Dispose();
            _mdns?.Dispose();
            _sd = null;
            _mdns = null;

            _isAdvertising = false;
            OnStatusChanged("Stopped mDNS advertisement");
        }

        private List<string> GetLocalIPAddresses()
        {
            var ipAddresses = new List<string>();
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                foreach (var ni in interfaces)
                {
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            ipAddresses.Add(addr.Address.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Error getting IP addresses: {ex.Message}");
            }
            return ipAddresses;
        }

        public bool IsAdvertising => _isAdvertising;
        public string InstanceName => _instanceName;
        public string ServiceType_ => $"{ServiceType}.local";
        public int Port => _port;
        public List<string> GetIPAddresses() => GetLocalIPAddresses();

        public void PrintDiscoveryInfo()
        {
            Console.WriteLine("\n=== mDNS Service Discovery ===");
            Console.WriteLine($"  Instance Name: {_instanceName}");
            Console.WriteLine($"  Service Type:  {ServiceType}.local");
            Console.WriteLine($"  Port:          {_port}");
            Console.WriteLine($"  Status:        {(_isAdvertising ? "Advertising" : "Not Advertising")}");

            var ips = GetLocalIPAddresses();
            foreach (var ip in ips)
                Console.WriteLine($"  IP: {ip}");

            Console.WriteLine();
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        public void Dispose()
        {
            StopAdvertising();
        }
    }
}
