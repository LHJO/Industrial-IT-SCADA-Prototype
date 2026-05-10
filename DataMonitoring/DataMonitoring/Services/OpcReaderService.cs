using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opc.UaFx.Client;

namespace DataMonitoring.Services
{
    /// <summary>
    /// Background service responsible for maintaining the OPC UA connection
    /// and managing subscriptions for real-time data updates.
    /// </summary>
    public class OpcReaderService : BackgroundService
    {
        private readonly OpcReader _opcReader;

        public OpcReaderService(OpcReader opcReader)
        {
            _opcReader = opcReader;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // If the connection is dropped, attempt to reconnect
                if (!_opcReader.IsConnected)
                {
                    _opcReader.Connect();
                }

                // Check every 5 seconds if still connected, reconnect if needed
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
