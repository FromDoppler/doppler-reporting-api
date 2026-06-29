using System;

namespace Doppler.ReportingApi.Models
{
    public class EmailCampaignDashboardItem
    {
        public int IdUser { get; set; }

        public int IdCampaign { get; set; }

        public int Status { get; set; }

        public DateTime StatsAt { get; set; }

        public int AmountSentSubscribers { get; set; }

        public int HardBouncedMailCount { get; set; }

        public int SoftBouncedMailCount { get; set; }

        public int UnopenedMailCount { get; set; }

        public int TotalOpenedMailCount { get; set; }

        public int DistinctOpenedMailCount { get; set; }

        public DateTime? LastOpenedEmailDate { get; set; }

        public int TotalClickCount { get; set; }

        public int DistinctClickCount { get; set; }

        public DateTime? LastClickDate { get; set; }

        public int NotSent { get; set; }

        public int Unsubscribes { get; set; }

        public int Spam { get; set; }
    }
}
