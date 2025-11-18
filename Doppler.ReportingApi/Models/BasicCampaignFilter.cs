using Doppler.ReportingApi.Models.Enums;
using System.Collections.Generic;

namespace Doppler.ReportingApi.Models
{
    public class BasicCampaignFilter
    {
        public string CampaignName { get; set; } = null;
        public CampaignTypeEnum? CampaignType { get; set; } = null;
        public string FromEmail { get; set; } = null;
        public List<string> Labels { get; set; }
    }
}
