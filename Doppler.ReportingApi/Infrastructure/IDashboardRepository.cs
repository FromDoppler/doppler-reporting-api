using Doppler.ReportingApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public interface IDashboardRepository
    {
        #region EmailCampaigns

        Task<IEnumerable<EmailCampaignDashboardItem>> GetEmailCampaignsAsync(
            string accountName,
            DateTime startDate,
            DateTime endDate,
            string campaignType);

        #endregion
    }
}
