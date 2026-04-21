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
    /// from the OPC UA server independently of the main web application thread.
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

                // Wait 1 second before reading again
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
