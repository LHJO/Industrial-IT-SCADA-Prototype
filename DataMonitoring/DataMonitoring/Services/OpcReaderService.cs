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
            // Connect once when service starts
            _opcReader.Connect();

            // Constantly read while app is running
            while (!stoppingToken.IsCancellationRequested)
            {
                _opcReader.ReadTag();

                // 1 second delay before next reading 
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
