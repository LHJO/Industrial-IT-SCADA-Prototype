using Newtonsoft.Json.Linq;
using Opc.Ua.Client;
using Opc.UaFx.Client;


namespace DataMonitoring.Services
{
    public class OpcReader
    {
        // Hard coded OPC UA server URL
        private string serverUrl = "opc.tcp://LHsPC:49580";

        // Hard coded OPC UA tag names for reading from the server
        private string tagNameReadFeedback = "ns=2;s=Tempreature Feedback";
        private string tagNameReadSetpoint = "ns=2;s=Tempreature Setpoint";

        private OpcClient _client;

        public string LatestFeedback { get; private set; } = "Waiting for data...";
        public string LatestSetpoint { get; private set; } = "Waiting for data...";

        public void Connect()
        {
            _client = new OpcClient(serverUrl);
            _client.Connect();
        }

        public void ReadTag()
        {
            if (_client != null && _client.State == OpcClientState.Connected)
            {
                var valFeedback = _client.ReadNode(tagNameReadFeedback);
                var valSetpoint = _client.ReadNode(tagNameReadSetpoint);

                LatestFeedback = valFeedback?.Value != null ? valFeedback.Value.ToString() : "No data";
                LatestSetpoint = valSetpoint?.Value != null ? valSetpoint.Value.ToString() : "No data"; 
            }
        }

        // public void AcknowledgeAlarm()
    }
}
