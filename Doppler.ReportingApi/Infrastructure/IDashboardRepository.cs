using Doppler.ReportingApi.Models;
using System;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public interface IDashboardRepository
    {
        Task<EmailCampaignsDashboard> GetEmailCampaignsDashboardAsync(
            string accountName,
            DateTime startDate,
            DateTime endDate,
            string campaignType,
            string chartPeriod);
    }
}
