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
                    SUM(Campaign.AmountSentSubscribers) AS TotalSentEmails,
                    SUM(Campaign.DistinctOpenedMailCount) AS DistinctOpenedMailCount,

                    SUM(Campaign.UnopenedMailCount) AS UnopenedMailCount,
                    SUM(LinkInfo.UniqueClickCount) AS UniqueClickCount
                    FROM [user]
                        INNER JOIN Campaign WITH (NOLOCK) on [user].iduser = Campaign.IdUser
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
                    (SELECT SUM(S.Amount) FROM ViewSubscribersByStatusXUserAmount S INNER JOIN [User] on [User].idUser = S.IdUser  WHERE [User].Email = @userName) AS TotalSubscribers,
                    COUNT(1) as NewSubscribers,
                    COUNT(CASE WHEN  S.IdSubscribersStatus = 8 THEN 1 END) AS RemovedSubscribers
                FROM Subscriber S
                    INNER JOIN [User] on S.IdUser = [User].idUser
                WHERE [User].Email = @userName AND
                    S.UTCCreationDate >= @startDate AND
                    S.UTCCreationDate < @endDate";

                var results = await connection.QueryAsync<SubscribersSummary>(dummyDatabaseQuery, new { userName, startDate, endDate });
                var result = results.SingleOrDefault();
                result = result == null ? new SubscribersSummary() : result;
                result.StartDate = startDate;
                result.EndDate = endDate;
                return result;
            }
        }

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
