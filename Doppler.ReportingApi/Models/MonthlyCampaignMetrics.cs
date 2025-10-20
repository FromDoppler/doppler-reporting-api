using System;

namespace Doppler.ReportingApi.Models
{
    public class MonthlyCampaignMetrics
    {
        /// <summary>
        /// Unique identifier of the user who owns the campaigns.
        /// </summary>
        public int IdUser { get; set; }

        /// <summary>
        /// Year extracted from the campaign's schedule date.
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Month extracted from the campaign's schedule date.
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Total number of campaigns sent during the specified month.
        /// </summary>
        public int CampaginsCount { get; set; }

        /// <summary>
        /// Total number of subscribers targeted by the campaigns.
        /// </summary>
        public int Subscribers { get; set; }

        /// <summary>
        /// Total number of emails successfully sent.
        /// </summary>
        public int Sent { get; set; }

        /// <summary>
        /// Delivery rate percentage (Sent / Subscribers * 100).
        /// </summary>
        public decimal DlvRate { get; set; }

        /// <summary>
        /// Total number of emails opened by subscribers.
        /// </summary>
        public int Opens { get; set; }

        /// <summary>
        /// Open rate percentage (Opens / Sent * 100).
        /// </summary>
        public decimal OpenRate { get; set; }

        /// <summary>
        /// Total number of emails that were not opened.
        /// </summary>
        public int Unopens { get; set; }

        /// <summary>
        /// Unopen rate percentage (Unopens / Sent * 100).
        /// </summary>
        public decimal UnopenRate { get; set; }

        /// <summary>
        /// Total number of clicks registered across all campaigns.
        /// </summary>
        public int Clicks { get; set; }

        /// <summary>
        /// Click-to-open rate percentage (Clicks / Opens * 100).
        /// </summary>
        public decimal ClickToOpenRate { get; set; }

        /// <summary>
        /// Total number of bounced emails (hard + soft).
        /// </summary>
        public int Bounces { get; set; }

        /// <summary>
        /// Bounce rate percentage (Bounces / Subscribers * 100).
        /// </summary>
        public decimal BounceRate { get; set; }

        /// <summary>
        /// Total number of unsubscribed users.
        /// </summary>
        public int Unsubscribes { get; set; }

        /// <summary>
        /// Unsubscribe rate percentage (Unsubscribes / Sent * 100).
        /// </summary>
        public decimal UnsubscribeRate { get; set; }

        /// <summary>
        /// Total number of spam complaints received.
        /// </summary>
        public int Spam { get; set; }

        /// <summary>
        /// Spam complaint rate percentage (Spam / Sent * 100).
        /// </summary>
        public decimal SpamRate { get; set; }
    }
}

