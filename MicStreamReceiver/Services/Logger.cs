using System;
using System.IO;
using System.Text;

namespace MicStreamReceiver.Services
{
    /// <summary>
    /// Dual-output TextWriter that writes to both console and file
    /// </summary>
    public class DualWriter : TextWriter
    {
        private readonly TextWriter _console;
        private readonly StreamWriter _file;

        public DualWriter(TextWriter console, StreamWriter file)
        {
            _console = console;
            _file = file;
        }

        public override Encoding Encoding => _console.Encoding;

        public override void Write(char value)
        {
            _console.Write(value);
            try { _file?.Write(value); } catch { }
        }

        public override void WriteLine(string? value)
        {
            _console.WriteLine(value);
            try { _file?.WriteLine(value); } catch { }
        }

        public override void Flush()
        {
            _console.Flush();
            try { _file?.Flush(); } catch { }
        }
    }

    /// <summary>
    /// Logger that redirects Console output to both console and file
    /// </summary>
    public static class Logger
    {
        private static StreamWriter? _logWriter;
        private static DualWriter? _dualWriter;
        private static string? _logFilePath;

        /// <summary>
        /// Initialize logger - redirects Console.Out to both console and file
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Use logs directory relative to the application
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

                // Create directory if it doesn't exist
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                // Create log file with timestamp
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logFilePath = Path.Combine(logDir, $"MicStreamReceiver_{timestamp}.log");

                // Open file for writing
                _logWriter = new StreamWriter(_logFilePath, append: true)
                {
                    AutoFlush = true
                };

                // Redirect Console.Out to dual writer
                _dualWriter = new DualWriter(Console.Out, _logWriter);
                Console.SetOut(_dualWriter);

                // Write header
                Console.WriteLine("═══════════════════════════════════════════════════════");
                Console.WriteLine($"MicStream Receiver - Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("═══════════════════════════════════════════════════════");
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to initialize log file: {ex.Message}");
                Console.WriteLine("Continuing with console-only logging...");
            }
        }

        /// <summary>
        /// Close log file and restore console
        /// </summary>
        public static void Close()
        {
            try
            {
                Console.WriteLine($"\nLog saved to: {_logFilePath}");
                _logWriter?.Close();
                _logWriter?.Dispose();
                _logWriter = null;
            }
            catch
            {
                // Ignore
            }
        }

        public static string? LogFilePath => _logFilePath;
    }
}
