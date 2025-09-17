using System;

namespace Doppler.ReportingApi.Models
{
    public class DailyCampaignMetrics
    {
        /// <summary>
        /// Date of the campaign activity (aggregated per day)
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Total number Subscribers To Send
        /// </summary>
        public int Subscribers { get; set; }

        /// <summary>
        /// Total number of emails sent on this date
        /// </summary>
        public int Sent { get; set; }

        /// <summary>
        /// Total number of opens on this date
        /// </summary>
        public int Opens { get; set; }

        /// <summary>
        /// Total number of clicks on this date
        /// </summary>
        public int Clicks { get; set; }

        /// <summary>
        /// Total number of bounces on this date
        /// </summary>
        public int Bounces { get; set; }

        /// <summary>
        /// Total number of unsubscribes on this date
        /// </summary>
        public int Unsubscribes { get; set; }

        /// <summary>
        /// Total number of unsubscribes on this date
        /// </summary>
        public int Spam { get; set; }
    }
}
