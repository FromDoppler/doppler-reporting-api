using System;
using System.Collections.Generic;

namespace Doppler.ReportingApi.Models
{
    public class EmailCampaignsDashboard
    {
        public EmailCampaignsDashboardKpis Kpis { get; set; } = new EmailCampaignsDashboardKpis();

        public EmailCampaignsDashboardChart Chart { get; set; } = new EmailCampaignsDashboardChart();
    }

    public class EmailCampaignsDashboardKpis
    {
        public int Sent { get; set; }

        public decimal DeliveryRate { get; set; }

        public decimal OpenRate { get; set; }

        public int TotalCampaigns { get; set; }

        public decimal ClickThroughRate { get; set; }

        public decimal Bounces { get; set; }

        public decimal Unsubscribes { get; set; }

        public decimal Spam { get; set; }

        public EmailCampaignsDashboardVariations Variations { get; set; } = new EmailCampaignsDashboardVariations();
    }

    public class EmailCampaignsDashboardVariations
    {
        public decimal Sent { get; set; }

        public decimal DeliveryRate { get; set; }

        public decimal OpenRate { get; set; }

        public decimal TotalCampaigns { get; set; }

        public decimal ClickThroughRate { get; set; }

        public decimal Bounces { get; set; }

        public decimal Unsubscribes { get; set; }

        public decimal Spam { get; set; }
    }

    public class EmailCampaignsDashboardChart
    {
        public string Period { get; set; } = "weekly";

        public List<EmailCampaignsDashboardChartCategory> Categories { get; set; } = new List<EmailCampaignsDashboardChartCategory>();
    }

    public class EmailCampaignsDashboardChartCategory
    {
        public DateTime StartDateUtc { get; set; }

        public DateTime EndDateUtc { get; set; }

        public int Sent { get; set; }

        public int Delivered { get; set; }

        public int Opens { get; set; }

        public int Clicks { get; set; }
    }
}
