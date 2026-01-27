using Dapper;
using Doppler.ReportingApi.Models;
using System;
using System.Collections.Generic;
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
                DECLARE @timezone INT

                SELECT
                    @timezone = offset
                FROM dbo.usertimezone timezone
                INNER JOIN dbo.[user] u ON u.idusertimezone = timezone.idusertimezone
                WHERE u.[Email] = @userName;

                SELECT
                    RPT.[IdUser],
                    RPT.[Date],
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
                        ,CAST(
                            DATEADD(MINUTE, @timezone, C.[UTCSentDate])
                            AS DATE
                        ) AS [Date]
                        ,COALESCE(CS.[AmountSentSubscribers], CS.[AmountSubscribersToSend], 0) AS [Sent]
                        ,ISNULL(CS.[DistinctOpenedMailCount],0) [Opens]
                        ,ISNULL(CS.[DistinctClickCount],0) [Clicks]
                        ,ISNULL(CS.[HardBouncedMailCount],0) [Hard]
                        ,ISNULL (CS.[SoftBouncedMailCount],0) [Soft]
                        ,(CASE C.[Status]
                            WHEN 9
                                THEN ISNULL(C.[UnsubscriptionsCount],0)
                            ELSE 0
                            END) [Unsubscribes]
                        ,0 [Spam]
                    FROM [dbo].[Campaign] C WITH (NOLOCK)
					LEFT JOIN dbo.CampaignStats CS ON CS.IdCampaign = C.IdCampaign
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON C.[IdUser] = U.[IdUser]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9,10)
                        AND C.[UTCSentDate] BETWEEN @startDate AND @endDate
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                        AND C.Active = 1
                    UNION ALL
                    SELECT
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,CAST(
                            DATEADD(MINUTE, @timezone, C.[UTCSentDate])
                            AS DATE
                        ) AS [Date]
                        ,0 [Sent]
                        ,0 [Opens]
                        ,0 [Clicks]
                        ,0 [Hard]
                        ,0 [Soft]
                        ,COUNT(CASE
                            WHEN IdUnsubscriptionReason <> 2 AND UnsubscriptionSubreason NOT IN (2,3,4) AND C.[Status] IN (5,10)
                                THEN 1
                            END) [Unsubscribes]
                        ,COUNT(CASE
                            WHEN IdUnsubscriptionReason = 2
                                THEN 1
                            WHEN UnsubscriptionSubreason IN (
                                    2
                                    ,3
                                    ,4
                                    )
                                THEN 1
                            END) [Spam]
                    FROM [dbo].[Subscriber] S WITH (NOLOCK)
                    JOIN [dbo].[Campaign] C WITH (NOLOCK)
                        ON S.[IdUser] = C.[IdUser] AND S.[IdCampaign] = C.[IdCampaign]
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON S.[IdUser] = U.[IdUser]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9,10)
                        AND C.[UTCSentDate] BETWEEN @startDate AND @endDate
                        AND S.[IdSubscribersStatus] = 5
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                        AND C.Active = 1
                    GROUP BY
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,CAST(
                            DATEADD(MINUTE, @timezone, C.[UTCSentDate])
                            AS DATE
                        )
                ) AS RPT
                GROUP BY
                        RPT.[IdUser],
                        RPT.[Date]";

                var results = await connection.QueryAsync<DailyCampaignMetrics>(dummyDatabaseQuery, new { userName, startDate, endDate });

                return results.ToList();
            }
        }

        public async Task<List<SentCampaignMetrics>> GetSentCampaignsMetrics(
            string userName,
            int pageNumber,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string campaignName = null,
            string campaignType = null,
            string fromEmail = null,
            List<int> labels = null)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var labelsStr = (labels != null && labels.Count > 0)
                    ? string.Join(",", labels)
                    : null;

                var results = await connection.QueryAsync<SentCampaignMetrics>(
                    "[dbo].[CampaignsSent_CampaignsMetrics]",
                    new
                    {
                        userName,
                        startdate = startDate,
                        enddate = endDate,
                        campaignName,
                        campaignType,
                        fromEmail,
                        labels = labelsStr,
                        pageNumber,
                        pageSize
                    },
                    commandType: System.Data.CommandType.StoredProcedure);

                return results.ToList();
            }
        }

        public async Task<int> GetSentCampaignsCount(string userName, DateTime? startDate = null, DateTime? endDate = null, string campaignName = null, string campaignType = null, string fromEmail = null, List<int> labels = null)
        {
            var labelsCount = labels?.Count ?? 0;

            using (var connection = _connectionFactory.GetConnection())
            {
                var dummyDatabaseQuery = @"
                SELECT COUNT(1)
                FROM [dbo].[Campaign] C WITH (NOLOCK)
                JOIN [dbo].[User] U WITH (NOLOCK)
                    ON C.[IdUser] = U.[IdUser]
                LEFT JOIN [dbo].[Label] L WITH (NOLOCK)
                    ON C.[IdLabel] = L.IdLabel
                WHERE
                    U.[Email] = @userName
                    AND C.[Status] IN (5,9)
                    AND C.Active = 1
                    AND (@startDate IS NULL OR C.[UTCSentDate] >= @startDate)
                    AND (@endDate IS NULL OR C.[UTCSentDate] < @endDate)
                    AND (@campaignName IS NULL OR LOWER(LTRIM(RTRIM(C.[Name]))) LIKE '%' + LOWER(LTRIM(RTRIM(@campaignName))) + '%')
                    AND (
                            @campaignType IS NULL
                            OR (LTRIM(RTRIM(@campaignType)) = '')
                            OR (@campaignType = 'TEST_AB' AND C.IdTestAB IS NOT NULL)
                            OR (C.CampaignType = @campaignType AND C.IdTestAB IS NULL)
                        )
                    AND (@fromEmail IS NULL OR LOWER(LTRIM(RTRIM(C.[FromEmail]))) LIKE LOWER(LTRIM(RTRIM(@fromEmail))))
                    AND C.[IdTestCampaign] IS NULL
                    AND C.[IdScheduledTask] IS NULL
                    AND (C.TestABCategory IS NULL OR C.TestABCategory = 3)
                    AND (
                    @labelsCount = 0
                    OR C.[IdLabel] IN @labels
                )";

                var count = await connection.QuerySingleAsync<int>(dummyDatabaseQuery, new { userName, startDate, endDate, campaignName, campaignType, fromEmail, labelsCount, labels });

                return count;
            }
        }

        public async Task<List<MonthlyCampaignMetrics>> GetMonthlyCampaignsMetrics(string userName, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var results = await connection.QueryAsync<MonthlyCampaignMetrics>(
                    "[dbo].[CampaignsSent_CampaignsByMonthMetrics]",
                    new
                    {
                        userName,
                        startdate = startDate,
                        enddate = endDate,
                        pageNumber,
                        pageSize
                    },
                    commandType: System.Data.CommandType.StoredProcedure
                );

                return results.ToList();
            }
        }

        public async Task<int> GetMonthlyCampaignsCount(string userName, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var dummyDatabaseQuery = @"
                SELECT COUNT(*)
                FROM (
                    SELECT
                        C.[IdUser],
                        YEAR(C.[UTCSentDate]) AS [Year],
                        MONTH(C.[UTCSentDate]) AS [Month]
                    FROM [dbo].[Campaign] C WITH (NOLOCK)
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON C.[IdUser] = U.[IdUser]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9)
                        AND C.Active = 1
                        AND (@startDate IS NULL OR C.[UTCSentDate] >= @startDate)
                        AND (@endDate IS NULL OR C.[UTCSentDate] < @endDate)
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                        AND (C.TestABCategory IS NULL OR C.TestABCategory = 3)
                    GROUP BY
                        C.[IdUser],
                        YEAR(C.[UTCSentDate]),
                        MONTH(C.[UTCSentDate])
                ) AS MonthlyGroups;";

                var count = await connection.QuerySingleAsync<int>(dummyDatabaseQuery, new { userName, startDate, endDate });

                return count;
            }
        }
    }
}
