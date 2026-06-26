using Doppler.ReportingApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public interface ISummaryRepository
    {
        Task<CampaignsSummary> GetCampaignsSummaryByUserAsync(string userName, DateTime startDate, DateTime endDate);
        Task<SubscribersSummary> GetSubscribersSummaryByUserAsync(string userName, DateTime startDate, DateTime endDate);
        Task<IEnumerable<SubscriberStatusStat>> GetSubscribersDashboardByUserAsync(string userName, DateTime startDate, DateTime endDate);
        Task<IEnumerable<EmailCampaignDashboardItem>> GetEmailCampaignsAsync(string accountName, DateTime startDate, DateTime endDate, string campaignType);
        Task<IEnumerable<WebsiteActivityRfmItem>> GetWebsiteActivityRfmAsync(string accountName);
        Task<SystemUsageSummary> GetSystemUsageAsync(string accountName);
    }
}
