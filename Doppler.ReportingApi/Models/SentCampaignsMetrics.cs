using System;

namespace Doppler.ReportingApi.Models
{
    public class SentCampaignMetrics
    {
        /// <summary>
        /// Unique identifier of the user who owns the campaign.
        /// </summary>
        public int IdUser { get; set; }

        /// <summary>
        /// Unique identifier of the campaign.
        /// </summary>
        public int IdCampaign { get; set; }

        /// <summary>
        /// Descriptive name of the campaign.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Date/time display in the campaign's sending timezone (e.g. "10/2/2026 16:43:48 PM (GMT-03:00)").
        /// </summary>
        public string SentDateDisplay { get; set; }

        /// <summary>
        /// Email address used as the sender of the campaign.
        /// </summary>
        public string FromEmail { get; set; }

        /// <summary>
        /// Type of campaign (e.g., CLASSIC, SOCIAL, TEST_AB).
        /// </summary>
        public string CampaignType { get; set; }

        /// <summary>
        /// Identifier of the A/B test associated with the campaign.
        /// Null or zero when the campaign is not part of an A/B test.
        /// </summary>
        public int? IdTestAB { get; set; }

        /// <summary>
        /// Total number of subscribers targeted by the campaign.
        /// </summary>
        public int Subscribers { get; set; }

        /// <summary>
        /// Total number of emails successfully sent.
        /// </summary>
        public int Sent { get; set; }

        /// <summary>
        /// Delivery rate percentage (sent / subscribers * 100).
        /// </summary>
        public decimal DlvRate { get; set; }

        /// <summary>
        /// Total number of unique email opens.
        /// </summary>
        public int Opens { get; set; }

        /// <summary>
        /// Open rate percentage (opens / sent * 100).
        /// </summary>
        public decimal OpenRate { get; set; }

        /// <summary>
        /// Total number of emails that were not opened.
        /// </summary>
        public int Unopens { get; set; }

        /// <summary>
        /// Unopen rate percentage (unopens / sent * 100).
        /// </summary>
        public decimal UnopenRate { get; set; }

        /// <summary>
        /// Total number of unique clicks recorded.
        /// </summary>
        public int Clicks { get; set; }

        /// <summary>
        /// Click-to-open rate percentage (clicks / opens * 100).
        /// Also known as CTOR (Click-To-Open Rate).
        /// </summary>
        public decimal ClickToOpenRate { get; set; }

        /// <summary>
        /// Total number of bounced emails (hard + soft).
        /// </summary>
        public int Bounces { get; set; }

        /// <summary>
        /// Bounce rate percentage (bounces / subscribers * 100).
        /// </summary>
        public decimal BounceRate { get; set; }

        /// <summary>
        /// Total number of users who unsubscribed from the campaign.
        /// </summary>
        public int Unsubscribes { get; set; }

        /// <summary>
        /// Unsubscribe rate percentage (unsubscribes / sent * 100).
        /// </summary>
        public decimal UnsubscribeRate { get; set; }

        /// <summary>
        /// Total number of spam complaints received.
        /// </summary>
        public int Spam { get; set; }

        /// <summary>
        /// Spam complaint rate percentage (spam / sent * 100).
        /// </summary>
        public decimal SpamRate { get; set; }

        /// <summary>
        /// Name of the label associated with the campaign.
        /// </summary>
        public string LabelName { get; set; }

        /// <summary>
        /// Color assigned to the campaign's label (used for visual identification).
        /// </summary>
        public string LabelColour { get; set; }
    }
}
