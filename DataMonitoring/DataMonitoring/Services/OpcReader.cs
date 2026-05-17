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
        private string tagNameReadFeedback = "ns=2;s=Temperature Process Value";
        private string tagNameReadSetpoint = "ns=2;s=Temperature Setpoint";

        private OpcClient _client;
        private OpcSubscription _subscriptionFeedback;
        private OpcSubscription _subscriptionSetpoint;

        public string LatestFeedback { get; private set; } = "Waiting for data...";
        public string LatestSetpoint { get; private set; } = "Waiting for data...";

        public bool IsConnected => _client != null && _client.State == OpcClientState.Connected;

        public void Connect()
        {
            try
            {
                if (_client == null)
                {
                    _client = new OpcClient(serverUrl);
                }

                if (_client.State != OpcClientState.Connected)
                {
                    _client.Connect();
                    CreateSubscriptions();
                }
            }
            catch (Exception)
            {
                LatestFeedback = "Server offline";
                LatestSetpoint = "Server offline";
            }
        }

        private void CreateSubscriptions()
        {
            try
            {
                if (_client != null && _client.State == OpcClientState.Connected)
                {
                    Console.WriteLine($"[OpcReader] Creating subscriptions for:");
                    Console.WriteLine($"  Feedback: {tagNameReadFeedback}");
                    Console.WriteLine($"  Setpoint: {tagNameReadSetpoint}");

                    _subscriptionFeedback = _client.SubscribeDataChange(
                        tagNameReadFeedback, 
                        (sender, e) => 
                        {
                            if (e.Item?.Value != null)
                            {
                                LatestFeedback = e.Item.Value.ToString();
                                Console.WriteLine($"[OpcReader] Feedback updated: {LatestFeedback}");
                            }
                        });

                    _subscriptionSetpoint = _client.SubscribeDataChange(
                        tagNameReadSetpoint, 
                        (sender, e) => 
                        {
                            if (e.Item?.Value != null)
                            {
                                LatestSetpoint = e.Item.Value.ToString();
                                Console.WriteLine($"[OpcReader] Setpoint updated: {LatestSetpoint}");
                            }
                        });

                    Console.WriteLine("[OpcReader] Subscriptions created successfully");
                }
            }
            catch (Exception ex)
            {
                LatestFeedback = "No Data";
                LatestSetpoint = "No Data";
                Console.WriteLine($"[OpcReader] Subscription error: {ex.Message}");
            }
        }
    }
}
