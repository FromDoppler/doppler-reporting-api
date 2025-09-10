using Doppler.ReportingApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public interface ICampaignRepository
    {
        Task<List<DailyCampaignMetrics>> GetDailyCampaignsMetrics(string userName, DateTime startDate, DateTime endDate);
    }
}
