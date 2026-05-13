using System;
using System.Collections.Generic;
using Opc.UaFx.Client;

namespace DataLogger;

public class OpcUaDataReader
{
    private readonly string _endpointUrl;
    private readonly List<string> _nodeIds;
    private readonly List<OpcItem> _items;
    private OpcClient _client;
    private bool _isConnected;
    private int _connectionAttempts;
    private const int MaxConnectionAttempts = 5;
    private const int RetryDelayMs = 2000;

    public OpcUaDataReader(string endpointUrl, List<string> nodeIds, List<OpcItem> items = null)
    {
        _endpointUrl = endpointUrl;
        _nodeIds = nodeIds ?? new List<string>();
        _items = items ?? new List<OpcItem>();
        _isConnected = false;
        _connectionAttempts = 0;
    }

    public void Connect()
    {
        _connectionAttempts = 0;
        ConnectWithRetry();
    }

    private void ConnectWithRetry()
    {
        _connectionAttempts++;

        try
        {
            _client = new OpcClient(_endpointUrl);
            _client.Connect();
            _isConnected = true;
            _connectionAttempts = 0;
            Console.WriteLine($"[OPC] Connected to OPC server: {_endpointUrl}");
        }
        catch (Exception ex)
        {
            _isConnected = false;
            Console.WriteLine($"[OPC] Connection attempt {_connectionAttempts}/{MaxConnectionAttempts} failed: {ex.Message}");

            if (_connectionAttempts < MaxConnectionAttempts)
            {
                Console.WriteLine($"[OPC] Retrying in {RetryDelayMs}ms...");
                System.Threading.Thread.Sleep(RetryDelayMs);
                ConnectWithRetry();
            }
            else
            {
                Console.WriteLine($"[OPC] Failed to connect after {MaxConnectionAttempts} attempts. Will retry on next cycle.");
            }
        }
    }

    public void Disconnect()
    {
        try
        {
            _client?.Disconnect();
            _client?.Dispose();
            _isConnected = false;
            Console.WriteLine("[OPC] Disconnected from OPC server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OPC] Error disconnecting: {ex.Message}");
        }
    }

    public List<DataPoint> ReadData()
    {
        var dataPoints = new List<DataPoint>();

        if (!_isConnected || _client == null)
        {
            if (_connectionAttempts == 0)
            {
                Console.WriteLine("[OPC] Not connected to OPC server. Attempting to reconnect...");
                ConnectWithRetry();
            }
            return dataPoints;
        }

        try
        {
            // Use Items collection if available, otherwise fall back to NodeIds
            var nodesToRead = _items.Count > 0 
                ? _items.ConvertAll(i => i.NodeId)
                : _nodeIds;

            foreach (var nodeId in nodesToRead)
            {
                try
                {
                    var value = _client.ReadNode(nodeId);

                    // Find the item metadata if it exists
                    var item = _items.Find(i => i.NodeId == nodeId);

                    dataPoints.Add(new DataPoint
                    {
                        NodeId = nodeId,
                        Value = value.Value,
                        Quality = "Good",
                        SourceTimestamp = value.SourceTimestamp,
                        ServerTimestamp = value.ServerTimestamp,
                        DisplayName = item?.DisplayName ?? nodeId,
                        Unit = item?.Unit,
                        SensorId = item?.SensorId
                    });
                }
                catch (Exception nodeEx)
                {
                    Console.WriteLine($"[OPC] Error reading node {nodeId}: {nodeEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OPC] Read cycle error: {ex.Message}");
            _isConnected = false;
            _connectionAttempts = 0;
        }

        return dataPoints;
    }

    public bool IsConnected => _isConnected;
}
