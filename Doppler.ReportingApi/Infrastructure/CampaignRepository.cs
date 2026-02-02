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
                        ,COALESCE(C.[AmountSentSubscribers], C.[AmountSubscribersToSend], 0) AS [Sent]
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

        public async Task<List<SentCampaignMetrics>> GetSentCampaignsMetrics(string userName, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string campaignName = null, string campaignType = null, string fromEmail = null, List<int> labels = null)
        {
            var labelsCount = labels?.Count ?? 0;

            using (var connection = _connectionFactory.GetConnection())
            {
                var dummyDatabaseQuery = @"
                DECLARE @timezone INT
                DECLARE @idUser INT

                SELECT
                    @timezone = offset
                    ,@idUser = u.IdUser
                FROM dbo.usertimezone timezone
                INNER JOIN dbo.[user] u ON u.idusertimezone = timezone.idusertimezone
                WHERE u.[Email] = @userName;

                SELECT
                    RPT.[IdUser]
                    ,RPT.[IdCampaign]
                    ,RPT.[Name]
                    ,dateadd(MINUTE, @timezone, RPT.[UTCScheduleDate]) AS [UTCScheduleDate]
                    ,RPT.[FromEmail]
                    ,RPT.[CampaignType]
                    ,RPT.[IdTestAB]
                    ,SUM(RPT.[Subscribers]) [Subscribers]
                    ,SUM(RPT.[Sent]) [Sent]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Sent]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(10,2))
                    END AS [DlvRate]
                    ,SUM(RPT.[Opens]) [Opens]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Opens]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(10,2))
                    END AS [OpenRate]
                    ,SUM(RPT.[Unopens]) AS [Unopens]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Sent] - RPT.[Opens]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(10,2))
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
                    ,RPT.[LabelColour]
                FROM(
                    SELECT
                        C.[IdUser]
                        ,C.[IdCampaign]
                        ,C.[Name]
                        ,C.[UTCScheduleDate]
                        ,C.[FromEmail]
                        ,C.[CampaignType]
                        ,C.[IdTestAB]
                        ,ISNULL(C.[AmountSentSubscribers],0) [Subscribers]
                        ,(ISNULL(C.[DistinctOpenedMailCount],0) + ISNULL(C.[UnopenedMailCount],0)) AS [Sent]
                        ,ISNULL(C.[DistinctOpenedMailCount],0) [Opens]
                        ,ISNULL(C.[UnopenedMailCount], 0) [Unopens]
                        ,ISNULL(C.[DistinctClickCount],0) [Clicks]
                        ,ISNULL(C.[HardBouncedMailCount],0) [Hard]
                        ,ISNULL (C.[SoftBouncedMailCount],0) [Soft]
                        ,ISNULL(C.[UnsubscriptionsCount], Unsubscribes.Unsubscribes) [Unsubscribes]
                        ,Unsubscribes.Spam [Spam]
                        ,L.[Name] [LabelName]
                        ,LC.[Colour] [LabelColour]
                    FROM [dbo].[Campaign] C WITH (NOLOCK)
                    JOIN [dbo].[User] U WITH (NOLOCK)
                        ON C.[IdUser] = U.[IdUser]
                    LEFT JOIN [dbo].[Label] L WITH (NOLOCK)
                        ON C.[IdLabel] = L.[IdLabel]
                    LEFT JOIN [dbo].[Colour] LC WITH (NOLOCK)
                        ON L.[IdColour] = LC.[IdColour]
                    LEFT JOIN (
                        SELECT
                            COUNT(
                                CASE
                                    WHEN S.IdUnsubscriptionReason <> 2
                                        AND S.UnsubscriptionSubreason NOT IN (2,3,4)
                                    THEN 1
                                END
                            ) [Unsubscribes]
                            ,COUNT(
                                CASE
                                    WHEN S.IdUnsubscriptionReason = 2
                                        OR S.UnsubscriptionSubreason IN (2,3,4)
                                    THEN 1
                                END
                            ) [Spam]
                            ,c.IdCampaign
                        FROM dbo.campaign c
                        INNER JOIN dbo.subscriber s ON c.idcampaign = s.IdCampaign
                        WHERE c.iduser = @idUser
                            AND c.STATUS in (5, 9)
                            AND c.IdTestCampaign IS NULL
                            AND s.IdSubscribersStatus = 5
                        GROUP BY c.IdCampaign
                    ) Unsubscribes ON c.idcampaign = Unsubscribes.IdCampaign
                    WHERE
                        U.[Email] = @userName
                        AND C.[Status] IN (5,9)
                        AND C.Active = 1
                        AND (@startDate IS NULL OR C.[UTCScheduleDate] >= @startDate)
                        AND (@endDate IS NULL OR C.[UTCScheduleDate] < @endDate)
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
                        )
                ) RPT
                GROUP BY RPT.[IdUser]
                        ,RPT.[IdCampaign]
                        ,RPT.[Name]
                        ,RPT.[UTCScheduleDate]
                        ,RPT.[FromEmail]
                        ,RPT.[CampaignType]
                        ,RPT.[IdTestAB]
                        ,RPT.[LabelName]
                        ,RPT.[LabelColour]
                ORDER BY RPT.[UTCScheduleDate] DESC
                OFFSET @pageNumber * @pageSize ROWS
                FETCH NEXT @pageSize ROWS ONLY";

                var results = await connection.QueryAsync<SentCampaignMetrics>(dummyDatabaseQuery, new { userName, pageNumber, pageSize, startDate, endDate, campaignName, campaignType, fromEmail, labelsCount, labels });

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
                    AND (@startDate IS NULL OR C.[UTCScheduleDate] >= @startDate)
                    AND (@endDate IS NULL OR C.[UTCScheduleDate] < @endDate)
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
                    ,COUNT(DISTINCT RPT.[IdCampaign]) [CampaginsCount]
                    ,SUM(RPT.[Subscribers]) [Subscribers]
                    ,SUM(RPT.[Sent]) [Sent]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Sent]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(5,2))
                    END AS [DlvRate]
                    ,SUM(RPT.[Opens]) [Opens]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Opens]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(5,2))
                    END AS [OpenRate]
                    ,SUM(RPT.[Unopens]) AS [Unopens]
                    ,CASE
                        WHEN SUM(RPT.[Subscribers]) = 0 THEN 0
                        ELSE CAST(SUM(RPT.[Sent] - RPT.[Opens]) * 100.0 / SUM(RPT.[Subscribers]) AS DECIMAL(5,2))
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
                        ,ISNULL(C.[AmountSentSubscribers],0) [Subscribers]
                        ,(ISNULL(C.AmountSentSubscribers, 0) - (ISNULL(C.HardBouncedMailCount, 0) + ISNULL(C.SoftBouncedMailCount, 0))) AS [Sent]
                        ,ISNULL(C.[DistinctOpenedMailCount],0) [Opens]
                        ,ISNULL(C.[UnopenedMailCount], 0) [Unopens]
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
                        AND (C.TestABCategory IS NULL OR C.TestABCategory = 3)
                    UNION ALL
                    SELECT
                        S.[IdUser]
                        ,S.[IdCampaign]
                        ,C.[UTCScheduleDate]
                        ,0 [Subscribers]
                        ,0 [Sent]
                        ,0 [Opens]
                        ,0 [Unopens]
                        ,0 [Clicks]
                        ,0 [Hard]
                        ,0 [Soft]
                        ,COUNT(
                            CASE
                                WHEN S.IdUnsubscriptionReason <> 2
                                    AND S.UnsubscriptionSubreason NOT IN (2,3,4)
                                THEN 1
                            END
                        ) [Unsubscribes]
                        ,COUNT(
                            CASE
                                WHEN S.IdUnsubscriptionReason = 2
                                    OR S.UnsubscriptionSubreason IN (2,3,4)
                                THEN 1
                            END
                        ) [Spam]
                    FROM [dbo].[Subscriber] S WITH (NOLOCK)
                    JOIN [dbo].[Campaign] C WITH (NOLOCK)
                        ON S.[IdUser] = C.[IdUser] AND S.[IdCampaign] = C.[IdCampaign]
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
                        AND (C.TestABCategory IS NULL OR C.TestABCategory = 3)
                        AND S.IdSubscribersStatus = 5
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
                OFFSET @pageNumber * @pageSize ROWS
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
                        AND (C.TestABCategory IS NULL OR C.TestABCategory = 3)
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
