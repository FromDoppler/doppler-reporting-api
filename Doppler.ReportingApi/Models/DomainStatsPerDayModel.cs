using System;
using System.Collections.Generic;

namespace Doppler.ReportingApi.Models
{
    public class DomainStatsPerDayModel
    {
        public string Domain { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public List<DomainStatsPerDayPeriodModel> Items { get; set; }
    }

    public class DomainStatsPerDayPeriodModel
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public int Added { get; set; }
        public int Deleted { get; set; }
    }
}
