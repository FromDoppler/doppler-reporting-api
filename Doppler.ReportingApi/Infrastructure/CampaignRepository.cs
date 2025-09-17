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
                var dummyDatabaseQuery = @"
                SELECT
                    RPT.[IdUser],
                    RPT.[Date],
                    SUM(RPT.[Subscribers]) [Subscribers],
                    SUM(RPT.[Sent]) [Sent],
                    SUM(RPT.[Opens]) [Opens],
                    SUM(RPT.[Clicks]) [Clicks],
                    SUM(RPT.[Hard] + RPT.[Soft]) [Bounces],
                    SUM(RPT.[Unsubscribes]) [Unsubscribes],
                    SUM(RPT.[Spam]) [Spam]
                FROM (
                    SELECT
                        C.[IdUser]
                        ,[IdCampaign]
                        ,CAST(C.[UTCSentDate] AS DATE) [Date]
                        ,ISNULL(C.[AmountSubscribersToSend],0) [Subscribers]
                        ,ISNULL(C.[AmountSentSubscribers],0) [Sent]
                        ,ISNULL(C.[DistinctOpenedMailCount],0) [Opens]
                        ,ISNULL(C.[DistinctClickCount],0) [Clicks]
                        ,ISNULL(C.[HardBouncedMailCount],0) [Hard]
                        ,ISNULL (C.[SoftBouncedMailCount],0) [Soft]
                        ,ISNULL(C.[UnsubscriptionsCount],0) [Unsubscribes]
                        ,0 [Spam]
                    FROM [dbo].[Campaign] C WITH (NOLOCK)
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON C.[IdUser] = U.[IdUser]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] = 5
                        AND C.[UTCSentDate] BETWEEN @startDate AND @endDate
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                    UNION ALL
                    SELECT
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,CAST(C.[UTCSentDate] AS DATE) [Date]
                        ,0 [Subscribers]
                        ,0 [Sent]
                        ,0 [Opens]
                        ,0 [Clicks]
                        ,0 [Hard]
                        ,0 [Soft]
                        ,0 [Unsubscribes]
                        ,COUNT(1) [Spam]
                    FROM [dbo].[Subscriber] S WITH (NOLOCK)
                    JOIN [dbo].[Campaign] C WITH (NOLOCK)
                        ON S.[IdUser] = S.[IdUser] AND S.[IdCampaign] = C.[IdCampaign]
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON S.[IdUser] = U.[IdUser]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] = 5
                        AND C.[UTCSentDate] BETWEEN @startDate AND @endDate
                        AND S.[IdSubscribersStatus] = 5
                        AND S.[IdUnsubscriptionReason] = 2
                    GROUP BY
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,CAST(C.[UTCSentDate] AS DATE)
                ) AS RPT
                GROUP BY
                        RPT.[IdUser],
                        RPT.[Date]";

                var results = await connection.QueryAsync<DailyCampaignMetrics>(dummyDatabaseQuery, new { userName, startDate, endDate });

                return results.ToList();
            }
        }
    }
}
