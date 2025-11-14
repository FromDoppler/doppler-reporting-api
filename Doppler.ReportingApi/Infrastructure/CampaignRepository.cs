using Dapper;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Models.Enums;
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
                        ,CAST(C.[UTCScheduleDate] AS DATE) [Date]
                        ,ISNULL(C.[AmountSubscribersToSend],0) [Subscribers]
                        ,ISNULL(C.[AmountSentSubscribers],0) [Sent]
                        ,ISNULL(C.[DistinctOpenedMailCount],0) [Opens]
                        ,ISNULL(C.[DistinctClickCount],0) [Clicks]
                        ,ISNULL(C.[HardBouncedMailCount],0) [Hard]
                        ,ISNULL (C.[SoftBouncedMailCount],0) [Soft]
                        ,(CASE C.[Status]
                            WHEN 9
                                THEN ISNULL(C.[UnsubscriptionsCount],0)
                            ELSE 0
                            END) [Unsubscribes]
                        ,0 [Spam]
                    FROM [dbo].[Campaign] C WITH (NOLOCK)
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON C.[IdUser] = U.[IdUser]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9,10)
                        AND C.[UTCScheduleDate] BETWEEN @startDate AND @endDate
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                    UNION ALL
                    SELECT
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,CAST(C.[UTCScheduleDate] AS DATE) [Date]
                        ,0 [Subscribers]
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
                        ON S.[IdUser] = S.[IdUser] AND S.[IdCampaign] = C.[IdCampaign]
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON S.[IdUser] = U.[IdUser]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9,10)
                        AND C.[UTCScheduleDate] BETWEEN @startDate AND @endDate
                        AND S.[IdSubscribersStatus] = 5
                    GROUP BY
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,CAST(C.[UTCScheduleDate] AS DATE)
                ) AS RPT
                GROUP BY
                        RPT.[IdUser],
                        RPT.[Date]";

                var results = await connection.QueryAsync<DailyCampaignMetrics>(dummyDatabaseQuery, new { userName, startDate, endDate });

                return results.ToList();
            }
        }

        public async Task<List<SentCampaignMetrics>> GetSentCampaignsMetrics(string userName, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string campaignName = null, string campaignType = null, string fromEmail = null)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var dummyDatabaseQuery = @"
                DECLARE @timezone INT

                SELECT @timezone = offset
                FROM dbo.usertimezone timezone
                INNER JOIN dbo.[user] u ON u.idusertimezone = timezone.idusertimezone
                WHERE u.[Email] = @userName

                SELECT
                    RPT.[IdUser]
                    ,RPT.[IdCampaign]
                    ,RPT.[Name]
                    ,dateadd(MINUTE, @timezone, RPT.[UTCScheduleDate]) AS [UTCScheduleDate]
                    ,RPT.[FromEmail]
                    ,RPT.[CampaignType]
                    ,SUM(RPT.[Subscribers]) [Subscribers]
                    ,SUM(RPT.[Sent]) [Sent]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Sent]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(10,2))
                    END AS [DlvRate]
                    ,SUM(RPT.[Opens]) [Opens]
                    ,CASE
                        WHEN SUM(RPT.[Sent]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Opens]) * 100.0 / SUM(RPT.[Sent]) AS DECIMAL(10,2))
                    END AS [OpenRate]
                    ,SUM(RPT.[Sent]) - SUM(RPT.[Opens]) AS [Unopens]
                    ,CASE
                        WHEN SUM(RPT.[Sent]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Sent] - RPT.[Opens]) * 100.0 / SUM(RPT.[Sent]) AS DECIMAL(10,2))
                    END AS [UnopenRate]
                    ,SUM(RPT.[Clicks]) [Clicks]
                    ,CASE
                        WHEN SUM(RPT.[Opens]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Clicks]) * 100.0 / SUM(RPT.[Opens]) AS DECIMAL(10,2))
                    END AS [ClickToOpenRate]
                    ,SUM(RPT.[Hard] + RPT.[Soft]) [Bounces]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Hard] + RPT.[Soft]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(10,2))
                    END AS [BounceRate]
                    ,SUM(RPT.[Unsubscribes]) [Unsubscribes]
                    ,CASE
                        WHEN SUM(RPT.[Sent]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Unsubscribes]) * 100.0 / SUM(RPT.[Sent]) AS DECIMAL(10,2))
                    END AS [UnsubscribeRate]
                    ,SUM(RPT.[Spam]) [Spam]
                    ,CASE
                        WHEN SUM(RPT.[Sent]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Spam]) * 100.0 / SUM(RPT.[Sent]) AS DECIMAL(10,2))
                    END AS [SpamRate]
                    ,RPT.[LabelName]
                FROM(
                    SELECT
                        C.[IdUser]
                        ,C.[IdCampaign]
                        ,C.[Name]
                        ,C.[UTCScheduleDate]
                        ,C.[FromEmail]
                        ,C.[CampaignType]
                        ,ISNULL(C.[AmountSubscribersToSend],0) [Subscribers]
                        ,ISNULL(C.[AmountSentSubscribers],0) [Sent]
                        ,ISNULL(C.[DistinctOpenedMailCount],0) [Opens]
                        ,ISNULL(C.[DistinctClickCount],0) [Clicks]
                        ,ISNULL(C.[HardBouncedMailCount],0) [Hard]
                        ,ISNULL (C.[SoftBouncedMailCount],0) [Soft]
                        ,ISNULL(C.[UnsubscriptionsCount],0) [Unsubscribes]
                        ,0 [Spam]
                        ,L.[Name] [LabelName]
                    FROM [dbo].[Campaign] C WITH (NOLOCK)
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON C.[IdUser] = U.[IdUser]
	                LEFT JOIN [dbo].[Label] L WITH (NOLOCK)
		                ON C.[IdLabel] = L.[IdLabel]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9)
                        AND C.Active = 1
                        AND (@startDate IS NULL OR C.[UTCScheduleDate] >= @startDate)
                        AND (@endDate IS NULL OR C.[UTCScheduleDate] <= @endDate)
                        AND (@campaignName IS NULL OR LOWER(LTRIM(RTRIM(C.[Name]))) LIKE '%' + LOWER(LTRIM(RTRIM(@campaignName))) + '%')
                        AND (@campaignType IS NULL OR C.[CampaignType] LIKE @campaignType)
                        AND (@fromEmail IS NULL OR LOWER(LTRIM(RTRIM(C.[FromEmail]))) LIKE LOWER(LTRIM(RTRIM(@fromEmail))))
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                        AND (c.TestABCategory IS NULL OR c.TestABCategory = 3)
                    UNION ALL
                    SELECT
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,C.[Name]
                        ,C.[UTCScheduleDate]
                        ,C.[FromEmail]
                        ,C.[CampaignType]
                        ,0 [Subscribers]
                        ,0 [Sent]
                        ,0 [Opens]
                        ,0 [Clicks]
                        ,0 [Hard]
                        ,0 [Soft]
                        ,0 [Unsubscribes]
                        ,COUNT(1) [Spam]
                        ,L.[Name] [LabelName]
                    FROM [dbo].[Subscriber] S WITH (NOLOCK)
                    JOIN [dbo].[Campaign] C WITH (NOLOCK)
                        ON S.[IdUser] = S.[IdUser] AND S.[IdCampaign] = C.[IdCampaign]
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON S.[IdUser] = U.[IdUser]
	                LEFT JOIN [dbo].[Label] L WITH (NOLOCK)
		                ON C.[IdLabel] = L.[IdLabel]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9)
                        AND C.Active = 1
                        AND (@startDate IS NULL OR C.[UTCScheduleDate] >= @startDate)
                        AND (@endDate IS NULL OR C.[UTCScheduleDate] <= @endDate)
                        AND (@campaignName IS NULL OR LOWER(LTRIM(RTRIM(C.[Name]))) LIKE '%' + LOWER(LTRIM(RTRIM(@campaignName))) + '%')
                        AND (@campaignType IS NULL OR C.[CampaignType] LIKE @campaignType)
                        AND (@fromEmail IS NULL OR LOWER(LTRIM(RTRIM(C.[FromEmail]))) LIKE LOWER(LTRIM(RTRIM(@fromEmail))))
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                        AND (c.TestABCategory IS NULL OR c.TestABCategory = 3)
                    GROUP BY
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,C.[Name]
                        ,C.[UTCScheduleDate]
                        ,C.[FromEmail]
                        ,C.[CampaignType]
		                ,L.[Name]
                ) RPT
                GROUP BY RPT.[IdUser]
                        ,RPT.[IdCampaign]
                        ,RPT.[Name]
                        ,RPT.[UTCScheduleDate]
                        ,RPT.[FromEmail]
                        ,RPT.[CampaignType]
		                ,RPT.[LabelName]
                ORDER BY RPT.[UTCScheduleDate] DESC
                OFFSET @pageNumber * @PageSize ROWS
                FETCH NEXT @pageSize ROWS ONLY";

                var results = await connection.QueryAsync<SentCampaignMetrics>(dummyDatabaseQuery, new { userName, pageNumber, pageSize, startDate, endDate, campaignName, campaignType, fromEmail });

                return results.ToList();
            }
        }

        public async Task<int> GetSentCampaignsCount(string userName, DateTime? startDate = null, DateTime? endDate = null, string campaignName = null, string campaignType = null, string fromEmail = null)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var dummyDatabaseQuery = @"
                SELECT COUNT(1)
                FROM [dbo].[Campaign] C WITH (NOLOCK)
                JOIN [dbo].[User] U WITH (NOLOCK)
                    ON C.[IdUser] = U.[IdUser]
                WHERE
                    U.[Email] = @userName
                    AND C.[Status] IN (5,9)
                    AND C.Active = 1
                    AND (@startDate IS NULL OR C.[UTCScheduleDate] >= @startDate)
                    AND (@endDate IS NULL OR C.[UTCScheduleDate] <= @endDate)
                    AND (@campaignName IS NULL OR LOWER(LTRIM(RTRIM(C.[Name]))) LIKE '%' + LOWER(LTRIM(RTRIM(@campaignName))) + '%')
                    AND (@campaignType IS NULL OR C.[CampaignType] LIKE @campaignType)
                    AND (@fromEmail IS NULL OR LOWER(LTRIM(RTRIM(C.[FromEmail]))) LIKE LOWER(LTRIM(RTRIM(@fromEmail))))
                    AND C.[IdTestCampaign] IS NULL
                    AND C.[IdScheduledTask] IS NULL
                    AND (c.TestABCategory IS NULL OR c.TestABCategory = 3)";

                var count = await connection.QuerySingleAsync<int>(dummyDatabaseQuery, new { userName, startDate, endDate, campaignName, campaignType, fromEmail });

                return count;
            }
        }

        public async Task<List<MonthlyCampaignMetrics>> GetMonthlyCampaignsMetrics(string userName, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var dummyDatabaseQuery = @"
                DECLARE @timezone INT

                SELECT @timezone = offset
                FROM dbo.usertimezone timezone
                INNER JOIN dbo.[user] u ON u.idusertimezone = timezone.idusertimezone
                WHERE u.[Email] = @userName

                SELECT
                    RPT.[IdUser]
                    ,YEAR(dateadd(MINUTE, @timezone, UTCScheduleDate)) [Year]
                    ,MONTH(dateadd(MINUTE, @timezone, UTCScheduleDate)) [Month]
                    ,COUNT(RPT.[IdCampaign]) [CampaginsCount]
                    ,SUM(RPT.[Subscribers]) [Subscribers]
                    ,SUM(RPT.[Sent]) [Sent]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Sent]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(5,2))
                    END AS [DlvRate]
                    ,SUM(RPT.[Opens]) [Opens]
                    ,CASE
                        WHEN SUM(RPT.[Sent]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Opens]) * 100.0 / SUM(RPT.[Sent]) AS DECIMAL(5,2))
                    END AS [OpenRate]
                    ,SUM(RPT.[Sent]) - SUM(RPT.[Opens]) AS [Unopens]
                    ,CASE
                        WHEN SUM(RPT.[Sent]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Sent] - RPT.[Opens]) * 100.0 / SUM(RPT.[Sent]) AS DECIMAL(5,2))
                    END AS [UnopenRate]
                    ,SUM(RPT.[Clicks]) [Clicks]
                    ,CASE
                        WHEN SUM(RPT.[Opens]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Clicks]) * 100.0 / SUM(RPT.[Opens]) AS DECIMAL(5,2))
                    END AS [ClickToOpenRate]
                    ,SUM(RPT.[Hard] + RPT.[Soft]) [Bounces]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Hard] + RPT.[Soft]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(5,2))
                    END AS [BounceRate]
                    ,SUM(RPT.[Unsubscribes]) [Unsubscribes]
                    ,CASE
                        WHEN SUM(RPT.[Sent]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Unsubscribes]) * 100.0 / SUM(RPT.[Sent]) AS DECIMAL(5,2))
                    END AS [UnsubscribeRate]
                    ,SUM(RPT.[Spam]) [Spam]
                    ,CASE
                        WHEN SUM(RPT.[Sent]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Spam]) * 100.0 / SUM(RPT.[Sent]) AS DECIMAL(5,2))
                    END AS [SpamRate]
                FROM(
                    SELECT
                        C.[IdUser]
                        ,C.[IdCampaign]
                        ,C.[UTCScheduleDate]
                        ,ISNULL(C.[AmountSubscribersToSend],0) [Subscribers]
                        ,ISNULL(C.[AmountSentSubscribers],0) [Sent]
                        ,ISNULL(C.[DistinctOpenedMailCount],0) [Opens]
                        ,ISNULL(C.[DistinctClickCount],0) [Clicks]
                        ,ISNULL(C.[HardBouncedMailCount],0) [Hard]
                        ,ISNULL(C.[SoftBouncedMailCount],0) [Soft]
                        ,ISNULL(C.[UnsubscriptionsCount],0) [Unsubscribes]
                        ,0 [Spam]
                    FROM [dbo].[Campaign] C WITH (NOLOCK)
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON C.[IdUser] = U.[IdUser]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9)
                        AND C.Active = 1
                        AND (@startDate IS NULL OR C.[UTCScheduleDate] >= @startDate)
                        AND (@endDate IS NULL OR C.[UTCScheduleDate] < @endDate)
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                        AND (c.TestABCategory IS NULL OR c.TestABCategory = 3)
                    UNION ALL
                    SELECT
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,C.[UTCScheduleDate]
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
                        AND C.[Status] IN (5,9)
                        AND C.Active = 1
                        AND (@startDate IS NULL OR C.[UTCScheduleDate] >= @startDate)
                        AND (@endDate IS NULL OR C.[UTCScheduleDate] < @endDate)
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                        AND (c.TestABCategory IS NULL OR c.TestABCategory = 3)
                    GROUP BY
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,C.[Name]
                        ,C.[UTCScheduleDate]
                        ,C.[FromEmail]
                        ,C.[CampaignType]
                ) RPT
                GROUP BY RPT.[IdUser]
                        ,YEAR(dateadd(MINUTE, @timezone, UTCScheduleDate))
                        ,MONTH(dateadd(MINUTE, @timezone, UTCScheduleDate))

                ORDER BY [Year] DESC, [Month] DESC
                OFFSET @pageNumber * @PageSize ROWS
                FETCH NEXT @pageSize ROWS ONLY";

                var results = await connection.QueryAsync<MonthlyCampaignMetrics>(dummyDatabaseQuery, new { userName, pageNumber, pageSize, startDate, endDate });

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
                        YEAR(C.[UTCScheduleDate]) AS [Year],
                        MONTH(C.[UTCScheduleDate]) AS [Month]
                    FROM [dbo].[Campaign] C WITH (NOLOCK)
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON C.[IdUser] = U.[IdUser]
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9)
                        AND C.Active = 1
                        AND (@startDate IS NULL OR C.[UTCScheduleDate] >= @startDate)
                        AND (@endDate IS NULL OR C.[UTCScheduleDate] < @endDate)
                        AND C.[IdTestCampaign] IS NULL
                        AND C.[IdScheduledTask] IS NULL
                        AND (c.TestABCategory IS NULL OR c.TestABCategory = 3)
                    GROUP BY
                        C.[IdUser],
                        YEAR(C.[UTCScheduleDate]),
                        MONTH(C.[UTCScheduleDate])
                ) AS MonthlyGroups;";

                var count = await connection.QuerySingleAsync<int>(dummyDatabaseQuery, new { userName, startDate, endDate });

                return count;
            }
        }
    }
}
