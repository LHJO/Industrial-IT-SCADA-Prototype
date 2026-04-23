using Newtonsoft.Json.Linq;
using Opc.Ua.Client;
using Opc.UaFx.Client;


namespace DataMonitoring.Services
{
    public class OpcReader
    {
        // Hard coded OPC UA server URL
        private string serverUrl = "opc.tcp://LHsPC:49580";

        // Hard coded OPC UA tag name for reading from the server
        private string tagNameRead = "ns=2;s=Tempreature";

        private OpcClient _client;

        public string LatestData { get; private set; } = "Waiting for data...";

        public void Connect()
        {
            _client = new OpcClient(serverUrl);
            _client.Connect();
        }

        public void ReadTag()
        {
            if (_client != null && _client.State == OpcClientState.Connected)
            {
                var val = _client.ReadNode(tagNameRead);

                LatestData = val?.Value != null ? val.Value.ToString() : "No data";
            }
        }
    }
}
