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
        private readonly Dictionary<string, AlarmInfo> _alarms = new();
        private readonly object _lockObject = new();

        public AlarmInfo AddOrUpdateAlarm(string alarmKey, string message, string severity)
        {
            lock (_lockObject)
            {
                if (_alarms.ContainsKey(alarmKey))
                {
                    var alarm = _alarms[alarmKey];
                    alarm.Message = message;
                    alarm.Severity = severity;
                    return alarm;
                }
                else
                {
                    var newAlarm = new AlarmInfo
                    {
                        Message = message,
                        Severity = severity,
                        IsAcknowledged = false
                    };
                    _alarms[alarmKey] = newAlarm;
                    return newAlarm;
                }
            }
        }

        public void AcknowledgeAlarm(string alarmKey)
        {
            lock (_lockObject)
            {
                if (_alarms.ContainsKey(alarmKey))
                {
                    _alarms[alarmKey].IsAcknowledged = true;
                }
            }
        }

        public void RemoveAlarm(string alarmKey)
        {
            lock (_lockObject)
            {
                _alarms.Remove(alarmKey);
            }
        }

        public AlarmInfo GetAlarm(string alarmKey)
        {
            lock (_lockObject)
            {
                return _alarms.ContainsKey(alarmKey) ? _alarms[alarmKey] : null;
            }
        }

        public IEnumerable<(string Key, AlarmInfo Alarm)> GetActiveAlarms()
        {
            lock (_lockObject)
            {
                return _alarms.Select(kvp => (kvp.Key, kvp.Value)).ToList();
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                _alarms.Clear();
            }
        }
    }
}
