using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace DataLogger
{
    public class Configuration
    {
        public string OpcServerUrl { get; set; }
        public List<string> NodeIds { get; set; }
        public List<OpcItem> Items { get; set; }
        public string ConnectionString { get; set; }
        public int ReadIntervalMs { get; set; }

        public static Configuration Load()
        {
            try
            {
                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"[CONFIG] Warning: appsettings.json not found at {configPath}");
                    Console.WriteLine("[CONFIG] Using default configuration values.");
                    return GetDefaults();
                }

                try
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                    var config = builder.Build();

                    var result = new Configuration
                    {
                        OpcServerUrl = config["OpcSettings:EndpointUrl"] ?? "opc.tcp://localhost:4840",
                        NodeIds = ParseNodeIds(config["OpcSettings:NodeIds"] ?? "ns=2;s=Temperature"),
                        Items = LoadItems(config) ?? new List<OpcItem>(),
                        ConnectionString = config.GetConnectionString("DefaultConnection") ?? "Server=LHsPC\\SQLEXPRESS;Database=LIBRARY;Trusted_Connection=True;TrustServerCertificate=True",
                        ReadIntervalMs = int.TryParse(config["OpcSettings:UpdateRate"], out int interval) ? interval : 5000
                    };

                    Console.WriteLine("[CONFIG] Configuration loaded successfully from appsettings.json");
                    return result;
                }
                catch (Exception configEx)
                {
                    Console.WriteLine($"[CONFIG] Error parsing appsettings.json: {configEx.Message}");
                    Console.WriteLine("[CONFIG] Using default configuration values.");
                    return GetDefaults();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONFIG] Unexpected error loading configuration: {ex.Message}");
                Console.WriteLine("[CONFIG] Using default configuration values.");
                return GetDefaults();
            }
        }

        private static Configuration GetDefaults()
        {
            var defaultItems = new List<OpcItem>
            {
                new OpcItem { NodeId = "ns=2;s=Tempreature Setpoint", DisplayName = "Temperature Setpoint", Unit = "°C", SensorId = 1 },
                new OpcItem { NodeId = "ns=2;s=Tempreature Feedback", DisplayName = "Temperature Feedback", Unit = "°C", SensorId = 2 }
            };

            return new Configuration
            {
                OpcServerUrl = "opc.tcp://localhost:4840",
                NodeIds = new List<string> { "ns=2;s=Tempreature Setpoint", "ns=2;s=Tempreature Feedback" },
                Items = defaultItems,
                ConnectionString = "Server=LHsPC\\SQLEXPRESS;Database=LIBRARY;Trusted_Connection=True;TrustServerCertificate=True",
                ReadIntervalMs = 5000
            };
        }

        private static List<OpcItem> LoadItems(IConfiguration config)
        {
            var items = new List<OpcItem>();

            try
            {
                var itemsSection = config.GetSection("OpcSettings:Items");

                if (itemsSection.Exists())
                {
                    itemsSection.Bind(items);
                    Console.WriteLine($"[CONFIG] Loaded {items.Count} OPC items from configuration");
                }
                else
                {
                    Console.WriteLine("[CONFIG] No Items section found in OpcSettings, using NodeIds only");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONFIG] Error loading items: {ex.Message}");
            }

            return items;
        }

        private static List<string> ParseNodeIds(string nodeIdsString)
        {
            var nodeIds = new List<string>();
            if (string.IsNullOrWhiteSpace(nodeIdsString))
                return nodeIds;

            foreach (var nodeId in nodeIdsString.Split(','))
            {
                var trimmed = nodeId.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    nodeIds.Add(trimmed);
            }
            return nodeIds;
        }
    }
}
