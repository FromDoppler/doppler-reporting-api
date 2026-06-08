using Doppler.ReportingApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public class DashboardRepository : IDashboardRepository
    {
        private static readonly int[] SentValues = new[] { 30000, 38000, 25000, 54000, 46000, 31000 };

        public Task<EmailCampaignsDashboard> GetEmailCampaignsDashboardAsync(
            string accountName,
            DateTime startDate,
            DateTime endDate,
            string campaignType,
            string chartPeriod)
        {
            var normalizedCampaignType = string.IsNullOrWhiteSpace(campaignType)
                ? "all"
                : campaignType.Trim().ToLowerInvariant();
            var normalizedChartPeriod = string.IsNullOrWhiteSpace(chartPeriod)
                ? "weekly"
                : chartPeriod.Trim().ToLowerInvariant();
            var isAutomation = normalizedCampaignType == "automation";

            var result = new EmailCampaignsDashboard
            {
                Kpis = isAutomation ? BuildEmptyKpis() : BuildDefaultKpis(),
                Chart = new EmailCampaignsDashboardChart
                {
                    Period = normalizedChartPeriod,
                    Categories = BuildCategories(endDate, normalizedChartPeriod, isAutomation)
                }
            };

            return Task.FromResult(result);
        }

        private static EmailCampaignsDashboardKpis BuildDefaultKpis()
        {
            return new EmailCampaignsDashboardKpis
            {
                Sent = 950,
                DeliveryRate = 38.9m,
                OpenRate = 38.9m,
                TotalCampaigns = 56,
                ClickThroughRate = 38.9m,
                Bounces = 38.9m,
                Unsubscribes = 0.7m,
                Spam = 38.9m,
                Variations = new EmailCampaignsDashboardVariations
                {
                    Sent = 15.8m,
                    DeliveryRate = 15.8m,
                    OpenRate = 15.8m,
                    TotalCampaigns = 15.8m,
                    ClickThroughRate = 15.8m,
                    Bounces = 15.8m,
                    Unsubscribes = 15.8m,
                    Spam = 15.8m
                }
            };
        }

        private static EmailCampaignsDashboardKpis BuildEmptyKpis()
        {
            return new EmailCampaignsDashboardKpis
            {
                Variations = new EmailCampaignsDashboardVariations()
            };
        }

        private static List<EmailCampaignsDashboardChartCategory> BuildCategories(
            DateTime endDate,
            string chartPeriod,
            bool isEmpty)
        {
            var values = isEmpty ? new int[SentValues.Length] : SentValues;
            var inclusiveEndDate = NormalizeInclusiveEndDate(endDate);
            var categories = new List<EmailCampaignsDashboardChartCategory>();

            for (var index = values.Length - 1; index >= 0; index--)
            {
                var range = ResolveBucketRange(inclusiveEndDate, chartPeriod, index);
                var sent = values[values.Length - index - 1];

                categories.Add(new EmailCampaignsDashboardChartCategory
                {
                    StartDateUtc = range.startDateUtc,
                    EndDateUtc = range.endDateUtc,
                    Sent = sent,
                    Delivered = (int)Math.Round(sent * 0.78m),
                    Opens = (int)Math.Round(sent * 0.45m),
                    Clicks = (int)Math.Round(sent * 0.06m)
                });
            }

            return categories;
        }

        private static DateTime NormalizeInclusiveEndDate(DateTime endDate)
        {
            var utcEndDate = endDate.Kind == DateTimeKind.Utc
                ? endDate
                : DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            return utcEndDate.AddTicks(-1);
        }

        private static (DateTime startDateUtc, DateTime endDateUtc) ResolveBucketRange(
            DateTime inclusiveEndDate,
            string chartPeriod,
            int reverseIndex)
        {
            switch (chartPeriod)
            {
                case "daily":
                    {
                        var anchorDate = inclusiveEndDate.Date.AddDays(-reverseIndex);
                        return (
                            DateTime.SpecifyKind(anchorDate, DateTimeKind.Utc),
                            DateTime.SpecifyKind(anchorDate.AddDays(1), DateTimeKind.Utc)
                        );
                    }
                case "monthly":
                    {
                        var anchorMonth = new DateTime(
                            inclusiveEndDate.Year,
                            inclusiveEndDate.Month,
                            1,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc
                        ).AddMonths(-reverseIndex);

                        return (anchorMonth, anchorMonth.AddMonths(1));
                    }
                default:
                    {
                        var anchorWeek = inclusiveEndDate.Date.AddDays(-(reverseIndex * 7));
                        return (
                            DateTime.SpecifyKind(anchorWeek, DateTimeKind.Utc),
                            DateTime.SpecifyKind(anchorWeek.AddDays(7), DateTimeKind.Utc)
                        );
                    }
            }
        }
    }
}
