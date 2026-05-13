using System;
using DataLogger;

Console.WriteLine("Data Logger: OPC UA to SQL Database");
Console.WriteLine("================================");

try
{
    var worker = new DataLoggerWorker();
    worker.Start();

    Console.WriteLine("Press Ctrl+C to exit...");
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        worker.Stop();
    };

    // Keep the application running
    while (true)
    {
        System.Threading.Thread.Sleep(1000);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error: {ex.Message}");
    Environment.Exit(1);
}
