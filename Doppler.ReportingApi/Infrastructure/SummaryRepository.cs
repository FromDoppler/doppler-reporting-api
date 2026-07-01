using Dapper;
using Doppler.ReportingApi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Infrastructure
{
    public class SummaryRepository : ISummaryRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        public SummaryRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<CampaignsSummary> GetCampaignsSummaryByUserAsync(string userName, DateTime startDate, DateTime endDate)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var dummyDatabaseQuery = @"
                SELECT
                        T.TotalSentEmails,
                        T.DistinctOpenedMailCount as TotalOpenClicks,

                        CAST(ISNULL(T.UniqueClickCount, 0) AS FLOAT) /
                        CAST(NULLIF((ISNULL(T.DistinctOpenedMailCount, 0) + ISNULL(T.UnopenedMailCount, 0)), 0) AS FLOAT) * 100 AS ClickThroughRate
                FROM (
                    SELECT
                    SUM(CS.AmountSentSubscribers) AS TotalSentEmails,
                    SUM(CS.DistinctOpenedMailCount) AS DistinctOpenedMailCount,

                    SUM(CS.UnopenedMailCount) AS UnopenedMailCount,
                    SUM(LinkInfo.UniqueClickCount) AS UniqueClickCount
                    FROM [user]
                        INNER JOIN Campaign WITH (NOLOCK) on [user].iduser = Campaign.IdUser
                        LEFT JOIN CampaignStats CS ON CS.IdCampaign = Campaign.IdCampaign
                    OUTER APPLY (
                        SELECT COUNT(DISTINCT LT.IdSubscriber) AS UniqueClickCount
                        FROM Link L WITH (NOLOCK)
                        INNER JOIN LinkTracking LT WITH (NOLOCK) ON LT.IdLink = L.IdLink
                        WHERE L.IdCampaign = Campaign.IdCampaign
                    ) AS LinkInfo
                    WHERE
                        Campaign.Status = 5 AND --SENT
                        Campaign.IdTestCampaign IS NULL AND --Exclude test campaigns
                        Campaign.IdScheduledTask IS NULL AND --Exclude automations
                        Email = @userName AND
                        Campaign.UTCSentDate >= @startDate AND
                        Campaign.UTCSentDate < @endDate
                ) T";

                var results = await connection.QueryAsync<CampaignsSummary>(dummyDatabaseQuery, new { userName, startDate, endDate });
                var result = results.SingleOrDefault();
                result = result == null ? new CampaignsSummary() : result;
                result.StartDate = startDate;
                result.EndDate = endDate;
                return result;
            }
        }

        public async Task<SubscribersSummary> GetSubscribersSummaryByUserAsync(string userName, DateTime startDate, DateTime endDate)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var dummyDatabaseQuery = @"
                SELECT
                    (SELECT SUM(S.Amount) FROM ViewSubscribersByStatusXUserAmount S
                    INNER JOIN [User] on [User].idUser = S.IdUser
                    WHERE [User].Email = @userName AND S.IdSubscribersStatus <> 7) AS TotalSubscribers,
                    COUNT(1) as NewSubscribers,
                    COUNT(CASE WHEN  S.IdSubscribersStatus = 8 THEN 1 END) AS RemovedSubscribers
                FROM Subscriber S
                    INNER JOIN [User] on S.IdUser = [User].idUser
                WHERE [User].Email = @userName AND
                    S.UTCCreationDate >= @startDate AND
                    S.UTCCreationDate < @endDate AND IdSubscribersStatus <> 7";

                var results = await connection.QueryAsync<SubscribersSummary>(dummyDatabaseQuery, new { userName, startDate, endDate });
                var result = results.SingleOrDefault();
                result = result == null ? new SubscribersSummary() : result;
                result.StartDate = startDate;
                result.EndDate = endDate;
                return result;
            }
        }

        #region Home Dashboard

        #region Audience

        public async Task<IEnumerable<SubscriberStatusStat>> GetSubscribersDashboardByUserAsync(
            string userName,
            DateTime startDate,
            DateTime endDate)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var query = @"
                    DECLARE @idUser INT;
                    DECLARE @timezone INT;

                    SELECT @idUser = IdUser
                    FROM [User]
                    WHERE Email = @userName;

                    SELECT @timezone = timezone.offset
                    FROM dbo.usertimezone timezone
                    INNER JOIN dbo.[User] u ON u.idusertimezone = timezone.idusertimezone
                    WHERE u.[Email] = @userName;

                    SELECT
                        StatsAt,
                        NewSubscribers,
                        ActiveSubscribers,
                        InactiveDisableSubscribers,
                        UnsubscribedByHardSubscribers,
                        UnsubscribedBySoftSubscribers,
                        UnsubscribedByNeverOpened,
                        PendingSubscribers,
                        UnsubscribedByClient,
                        StandBySubscribers
                    FROM SubscriberStatusStat
                    WHERE IdUser = @idUser
                        AND StatsAt >= CAST(DATEADD(MINUTE, @timezone, @startDate) AS DATE)
                        AND StatsAt < CAST(DATEADD(MINUTE, @timezone, @endDate) AS DATE)
                    ORDER BY StatsAt DESC;
                ";

                return await connection.QueryAsync<SubscriberStatusStat>(
                    query,
                    new
                    {
                        userName,
                        startDate,
                        endDate
                    });
            }
        }

        #endregion Audience

        #region Email Campaign

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
                        startDate = startDate,
                        endDate = endDate,
                    },
                    commandType: CommandType.StoredProcedure);
            }
        }

        #endregion Email Campaign

        #region Website Activity

        public async Task<WebsiteActivityRfmDashboard> GetWebsiteActivityRfmAsync(string accountName)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var query = @"
                    SELECT
                        SL.IdUser,
                        S.IdSegment,
                        SL.Name AS SegmentName,
                        TPA.IdThirdPartyApp,
                        TPA.Name AS IntegrationName,
                        RFMS.IdRFMSegment,
                        TPAU.RFMPeriod,
                        COUNT(SXL.IdSubscriber) AS SubscribersQty
                    FROM dbo.[User] U
                    INNER JOIN dbo.ThirdPartyAppXUser TPAU
                        ON TPAU.IdUser = U.IdUser
                        AND TPAU.RFMActive = 1
                    INNER JOIN ThirdPartyApp TPA
                        ON TPA.IdThirdPartyApp = TPAU.IdThirdPartyApp
                    INNER JOIN dbo.SubscribersList SL
                        ON SL.IdUser = U.IdUser
                    INNER JOIN dbo.Segment S
                        ON S.IdSegment = SL.IdSubscribersList
                    INNER JOIN dbo.[Filter] F
                        ON F.IdFilter = S.IdFilter
                    INNER JOIN dbo.RFMSegment RFMS
                        ON RFMS.IdRFMSegment = F.IdRFMSegment
                    LEFT JOIN dbo.SubscriberXList SXL
                        ON SXL.IdSubscribersList = S.IdSegment
                        AND SXL.Active = 1
                    WHERE U.Email = @accountName
                    GROUP BY
                        SL.IdUser,
                        S.IdSegment,
                        SL.[Name],
                        TPA.IdThirdPartyApp,
                        TPA.[Name],
                        RFMS.IdRFMSegment,
                        TPAU.RFMPeriod
                    ORDER BY RFMS.IdRFMSegment;";

                var result = (await connection.QueryAsync<WebsiteActivityRfm>(
                    query,
                    new { accountName }))
                    .ToList();

                var firstResult = result.FirstOrDefault();

                return new WebsiteActivityRfmDashboard
                {
                    IdUser = firstResult?.IdUser,
                    RFMPeriod = firstResult?.RFMPeriod,
                    IdThirdPartyApp = firstResult?.IdThirdPartyApp,
                    IntegrationName = firstResult?.IntegrationName,
                    Segments = result.Select(x => new WebsiteActivityRfmSegment
                    {
                        IdSegment = x.IdSegment,
                        SegmentName = x.SegmentName,
                        IdRFMSegment = x.IdRFMSegment,
                        SubscribersQty = x.SubscribersQty
                    })
                };
            }
        }

        #endregion Website Activity

        #endregion Home Dashboard

        public async Task<SystemUsageSummary> GetSystemUsageAsync(string accountName)
        {
            using (var connection = _connectionFactory.GetConnection())
            {
                var databaseQuery = @"
                SELECT
                    HasCampaignCreated AS HasCampaingsCreated,
                    HasListCreated AS HasListsCreated,
                    HasCampaignSent AS HasCampaingsSent,
                    CAST(ISNULL(DomainInfo.HasDomainsReady, 0) AS BIT) AS HasDomainsReady
                FROM [User]
                    OUTER APPLY  (
                        SELECT TOP 1 1 AS HasDomainsReady
                        FROM DomainInformationXUser
                        WHERE
                            DomainInformationXUser.IdDomainStatus = 2 AND
                            DomainInformationXUser.Active = 1 AND
                            DomainInformationXUser.IdDomainDmarcStatus = 2 AND
                            DomainInformationXUser.IdDomainKeyStatus = 2 AND
                            DomainInformationXUser.IdUser = [User].IdUser
                        ) DomainInfo
                WHERE
                    [User].Email = @accountName";

                var results = await connection.QueryAsync<SystemUsageSummary>(databaseQuery, new { accountName });
                var result = results.SingleOrDefault();

                return result == null ? new SystemUsageSummary() : result;
            }
        }
    }
}
