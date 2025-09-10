using Dapper;
using Doppler.ReportingApi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public class CampaignRepository : ICampaignRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        public CampaignRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<DailyCampaignMetrics>> GetDailyCampaignsMetrics(string userName, DateTime startDate, DateTime endDate)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var spName = "Campaigns_DailyCampaignsMetrics";

                var parameters = new
                {
                    AccountName = userName,
                    StartDate = startDate,
                    EndDate = endDate
                };

                var results = await connection.QueryAsync<DailyCampaignMetrics>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return results.ToList();
            }
        }
    }
}
