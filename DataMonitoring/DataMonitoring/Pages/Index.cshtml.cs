using DataMonitoring.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataMonitoring.Pages
{
    public class IndexModel : PageModel
    {
        private readonly OpcReader _opcReader;
        private readonly AlarmService _alarmService;

        public string CurrentFeedback { get; set; }
        public string CurrentSetpoint { get; set; }

        public IndexModel(OpcReader opcReader, AlarmService alarmService)
        {
            _opcReader = opcReader;
            _alarmService = alarmService;
        }

        public void OnGet()
        {
            CurrentFeedback = _opcReader.LatestFeedback;
            CurrentSetpoint = _opcReader.LatestSetpoint;
        }

        public JsonResult OnGetLatestData()
        {
            // Get feedback and setpoint values
            double feedback = double.TryParse(_opcReader.LatestFeedback, out var fb) ? fb : 0;

            // Alarm thresholds
            const double alarmLimitHigh = 40;
            const double alarmLimitLow = 15;

            // Check for alarms and update AlarmService
            if (feedback >= alarmLimitHigh)
            {
                _alarmService.AddOrUpdateAlarm("tempHigh", $"Temperature High ({feedback:F2} °C)", "High");
            }
            else
            {
                _alarmService.RemoveAlarm("tempHigh");
            }

            if (feedback <= alarmLimitLow)
            {
                _alarmService.AddOrUpdateAlarm("tempLow", $"Temperature Low ({feedback:F2} °C)", "High");
            }
            else
            {
                _alarmService.RemoveAlarm("tempLow");
            }

            return new JsonResult(new 
            { 
                feedback = _opcReader.LatestFeedback,
                setpoint = _opcReader.LatestSetpoint,
                isConnected = _opcReader.IsConnected,
                alarms = _alarmService.GetActiveAlarms().Select(a => new 
                { 
                    key = a.Key,
                    id = a.Alarm.Id,
                    message = a.Alarm.Message,
                    severity = a.Alarm.Severity,
                    time = a.Alarm.Time,
                    isAcknowledged = a.Alarm.IsAcknowledged
                }).ToList()
            });
        }

        public JsonResult OnPostAcknowledgeAlarm([FromBody] AcknowledgeRequest request)
        {
            if (!string.IsNullOrEmpty(request.AlarmKey))
            {
                _alarmService.AcknowledgeAlarm(request.AlarmKey);
                return new JsonResult(new { success = true, message = "Alarm acknowledged" });
            }
            return new JsonResult(new { success = false, error = "Invalid alarm key" });
        }

        public JsonResult OnPostClearAlarm([FromBody] ClearRequest request)
        {
            if (!string.IsNullOrEmpty(request.AlarmKey))
            {
                _alarmService.RemoveAlarm(request.AlarmKey);
                return new JsonResult(new { success = true, message = "Alarm cleared" });
            }
            return new JsonResult(new { success = false, error = "Invalid alarm key" });
        }
    }

    public class AcknowledgeRequest
    {
        public string AlarmKey { get; set; }
    }

    public class ClearRequest
    {
        public string AlarmKey { get; set; }
    }
}
