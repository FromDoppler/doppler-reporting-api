using System;
using System.Collections.Generic;

namespace Doppler.ReportingApi.Models
{
    public class WebsiteActivityRfmDashboard
    {
        public int? IdUser { get; set; }

        public int? RFMPeriod { get; set; }

        public string IntegrationName { get; set; }

        public IEnumerable<WebsiteActivityRfmSegment> Segments { get; set; } = Array.Empty<WebsiteActivityRfmSegment>();
    }
}
