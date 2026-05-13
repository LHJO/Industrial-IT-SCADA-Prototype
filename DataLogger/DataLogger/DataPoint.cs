namespace DataLogger;

public class DataPoint
{
    public string NodeId { get; set; }
    public object Value { get; set; }
    public string Quality { get; set; }
    public DateTime? SourceTimestamp { get; set; }
    public DateTime? ServerTimestamp { get; set; }
    public DateTime? Timestamp => SourceTimestamp ?? ServerTimestamp;
    public string DisplayName { get; set; }
    public string Unit { get; set; }
    public int? SensorId { get; set; }
}
