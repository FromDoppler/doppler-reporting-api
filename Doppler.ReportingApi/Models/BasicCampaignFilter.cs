using Doppler.ReportingApi.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Doppler.ReportingApi.Models
{
    public class BasicCampaignFilter
    {
        public string CampaignName { get; set; } = null;
        public CampaignTypeEnum? CampaignType { get; set; } = null;
        public string FromEmail { get; set; } = null;
    }
}
