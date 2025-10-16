using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public interface ICampaignRepository
    {
        Task<List<DailyCampaignMetrics>> GetDailyCampaignsMetrics(string userName, DateTime startDate, DateTime endDate);

        Task<List<SentCampaignMetrics>> GetSentCampaignsMetrics(string userName, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string campaignName = null, string campaignType = null, string fromEmail = null);

        Task<int> GetSentCampaignsCount(string userName, DateTime? startDate = null, DateTime? endDate = null, string campaignName = null, string campaignType = null, string fromEmail = null);

        Task<List<MonthlyCampaignMetrics>> GetMonthlyCampaignsMetrics(string userName, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null);

        Task<int> GetMonthlyCampaignsCount(string userName, DateTime? startDate = null, DateTime? endDate = null);
    }
}
