using System;
using System.Collections.Generic;
using System.Timers;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DataLogger
{
    public class DataLoggerWorker
    {
        private System.Timers.Timer _timer;
        private OpcUaDataReader _opcUaReader;
        private DatabaseWriter _databaseWriter;
        private Configuration _config;
        private bool _isRunning;
        private int _successfulCycles;
        private int _failedCycles;

        public DataLoggerWorker()
        {
            _successfulCycles = 0;
            _failedCycles = 0;
            _isRunning = false;
        }

        public void Start()
        {
            try
            {
                // Load configuration
                _config = Configuration.Load();

                // Print configuration header
                Console.WriteLine("\n╔════════════════════════════════════════╗");
                Console.WriteLine("║         CONFIGURATION                  ║");
                Console.WriteLine("╚════════════════════════════════════════╝\n");

                Console.WriteLine($"OPC Server: {_config.OpcServerUrl}");
                Console.WriteLine($"\nItems to Read:");
                foreach (var item in _config.Items)
                {
                    Console.WriteLine($"  • {item.DisplayName} ({item.NodeId})");
                    Console.WriteLine($"    Unit: {item.Unit}");
                }

                Console.WriteLine($"\nRead Interval: {_config.ReadIntervalMs}ms");
                Console.WriteLine($"Database: {_config.ConnectionString.Split(';')[0]}...\n");

                // Initialize components with Items
                _opcUaReader = new OpcUaDataReader(_config.OpcServerUrl, _config.NodeIds, _config.Items);
                _databaseWriter = new DatabaseWriter(_config.ConnectionString);

                // Attempt initial OPC connection
                _opcUaReader.Connect();

                // Start timer
                _timer = new System.Timers.Timer(_config.ReadIntervalMs);
                _timer.Elapsed += OnTimerElapsed;
                _timer.AutoReset = true;
                _timer.Start();

                _isRunning = true;
                Console.WriteLine("╔══════════════════════════════════════════════╗");
                Console.WriteLine("║  Reading OPC UA Data and Sending to Database ║");
                Console.WriteLine("╚══════════════════════════════════════════════╝\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Fatal startup error: {ex.Message}");
                Stop();
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                _isRunning = false;
                _timer?.Stop();
                _timer?.Dispose();
                _opcUaReader?.Disconnect();
                _databaseWriter?.Dispose();

                Console.WriteLine("\n╔══════════════════════════════════════════════╗");
                Console.WriteLine("║  Stopped                                      ║");
                Console.WriteLine("╚══════════════════════════════════════════════╝");
                Console.WriteLine($"  Cycles: {_successfulCycles} successful, {_failedCycles} failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error stopping: {ex.Message}");
            }
        }

        public void Resume()
        {
            if (!_isRunning == true)
            {
                Console.WriteLine("[INFO] DataLogger is already running");
                return;
            }

            try
            {
                Console.WriteLine("\n╔══════════════════════════════════════════════╗");
                Console.WriteLine("║  Resuming Data Logging                        ║");
                Console.WriteLine("╚══════════════════════════════════════════════╝\n");

                // Reconnect OPC reader
                if (_opcUaReader != null)
                    _opcUaReader.Connect();

                // Restart timer
                if (_timer != null)
                {
                    _timer.Start();
                }

                _isRunning = true;
                Console.WriteLine("[INFO] DataLogger resumed successfully\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error resuming: {ex.Message}");
            }
        }
        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_isRunning)
                return;

            try
            {
                _timer.Stop();

                // Read data from OPC
                var data = _opcUaReader.ReadData();

                if (data != null && data.Count > 0)
                {
                    // Send to database
                    await _databaseWriter.WriteDataAsync(data);
                    _successfulCycles++;
                }
                else
                {
                    if (!_opcUaReader.IsConnected)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [OPC] Waiting for connection...");
                        _failedCycles++;
                    }
                }
            }
            catch (Exception ex)
            {
                _failedCycles++;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERROR] {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                if (_isRunning)
                    _timer.Start();
            }
        }

        public bool IsRunning => _isRunning;
        public int SuccessfulCycles => _successfulCycles;
        public int FailedCycles => _failedCycles;
    }
}
