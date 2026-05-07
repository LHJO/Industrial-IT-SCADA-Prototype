using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opc.UaFx.Client;

namespace DataMonitoring.Services
{
    /// <summary>
    /// Background service responsible for continuously monitoring data 
    /// from the OPC UA server of the main web application thread.
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

                // If connection succeeded, read tags
                if (_opcReader.IsConnected)
                {
                    _opcReader.ReadTag();
                }

                // 1 second delay before next reading and reconnection if disconnected
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
