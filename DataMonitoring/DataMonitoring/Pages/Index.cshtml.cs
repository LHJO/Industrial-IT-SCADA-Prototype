using DataMonitoring.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataMonitoring.Pages
{
    public class IndexModel : PageModel
    {

        private readonly OpcReader _opcReader;

        public string CurrentFeedback { get; set; }
        public string CurrentSetpoint { get; set; }

        public IndexModel(OpcReader opcReader)
        {
            _opcReader = opcReader;
        }
        public void OnGet()
        {
            CurrentFeedback = _opcReader.LatestFeedback;
            CurrentSetpoint = _opcReader.LatestSetpoint;
        }

        public JsonResult OnGetLatestData()
        {
            return new JsonResult(new 
            { 
                feedback = _opcReader.LatestFeedback,
                setpoint = _opcReader.LatestSetpoint,
                isConnected = _opcReader.IsConnected
            });
        }

    }
}
