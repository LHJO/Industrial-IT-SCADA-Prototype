using DataMonitoring.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataMonitoring.Pages
{
    public class IndexModel : PageModel
    {

        private readonly OpcReader _opcReader;

        public string CurrentTagValue { get; set; }

        public IndexModel(OpcReader opcReader)
        {
            _opcReader = opcReader;
        }
        public void OnGet()
        {
            CurrentTagValue = _opcReader.LatestData;
        }

        public JsonResult OnGetLatestData()
        {
            return new JsonResult(new 
            { 
                temp1 = _opcReader.LatestData 
            });
        }

    }
}
