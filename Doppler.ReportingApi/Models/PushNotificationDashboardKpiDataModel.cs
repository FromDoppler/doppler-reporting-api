using System;
using System.Collections.Generic;

namespace Doppler.ReportingApi.Models
{
    public class PushNotificationDashboardKpiDataModel
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public List<PushNotificationDashboardKpiItemModel> Items { get; set; }
        public PushNotificationDashboardKpiStatsModel Totals { get; set; }
    }

    public class PushNotificationDashboardKpiItemModel
    {
        public string Domain { get; set; }
        public bool Found { get; set; }
        public PushNotificationDashboardKpiStatsModel PushStats { get; set; }
    }

    public class PushNotificationDashboardKpiStatsModel
    {
        public int Sent { get; set; }
        public int Delivered { get; set; }
        public int TotalClicks { get; set; }
        public int Subscribed { get; set; }
        public int Unsubscribed { get; set; }
        public int CurrentSubscribers { get; set; }
    }
}
