using System;

namespace Doppler.ReportingApi.Models
{
    public class SubscriberStatusStat
    {
        public DateTime StatsAt { get; set; }

        public int NewSubscribers { get; set; }

        public int ActiveSubscribers { get; set; }

        public int InactiveDisableSubscribers { get; set; }

        public int UnsubscribedByHardSubscribers { get; set; }

        public int UnsubscribedBySoftSubscribers { get; set; }

        public int UnsubscribedBySubscriber { get; set; }

        public int UnsubscribedByNeverOpened { get; set; }

        public int PendingSubscribers { get; set; }

        public int UnsubscribedByClient { get; set; }

        public int StandBySubscribers { get; set; }
    }
}
