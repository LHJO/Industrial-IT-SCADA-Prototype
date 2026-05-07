namespace DataMonitoring.Services
{

    /// <summary>
    /// Background service responsible for alarm handling 
    /// from the data read from the OPC UA server of the main web application thread.
    /// </summary>

    public class AlarmInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; }
        public string Severity { get; set; }
        public string Time { get; set; } = DateTime.Now.ToString("HH:mm:ss");
        public bool IsAcknowledged { get; set; }
    }

    public class AlarmService
    {
    }
}
