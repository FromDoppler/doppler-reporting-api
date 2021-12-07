using Doppler.ReportingApi.Models;
using System;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public interface ISummaryRepository
    {
        Task<CampaignsSummary> GetCampaignsSummaryByUserAsync(string userName, DateTime startDate, DateTime endDate);
        Task<SubscribersSummary> GetSubscribersSummaryByUserAsync(string userName, DateTime startDate, DateTime endDate);
        Task<SystemUsageSummary> GetSystemUsageAsync(string accountName);
    }
}
