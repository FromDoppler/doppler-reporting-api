using Dapper;
using Doppler.ReportingApi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public DashboardRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        #region EmailCampaigns

        public async Task<IEnumerable<EmailCampaignDashboardItem>> GetEmailCampaignsAsync(
            string accountName,
            DateTime startDate,
            DateTime endDate,
            string campaignType)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                return await connection.QueryAsync<EmailCampaignDashboardItem>(
                    "[dbo].[GetCampaignDailyStatsDashboard]",
                    new
                    {
                        accountName,
                        campaignType,
                        startDate = startDate.Date,
                        endDate = endDate.Date,
                    },
                    commandType: CommandType.StoredProcedure);
            }
        }

        #endregion
    }
}
