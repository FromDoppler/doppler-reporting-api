using Doppler.ReportingApi.DopplerSecurity;
using Doppler.ReportingApi.Infrastructure;
using Doppler.ReportingApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Controllers
{
    [Authorize]
    [ApiController]
    public class CampaignController
    {
        private readonly ILogger _logger;
        private readonly ICampaignRepository _campaignRepository;

        public CampaignController(ILogger<CampaignController> logger, ICampaignRepository campaignRepository)
        {
            _logger = logger;
            _campaignRepository = campaignRepository;
        }

        /// <summary>
        /// Return an object summarizing the campaingns performance of an user
        /// </summary>
        /// <param name="accountName">User name</param>
        /// <param name="dateFilter">A basic date range filter, </param>
        /// <remarks>Dates must be valid UtcTime with timezone</remarks>
        [HttpGet]
        [Route("{accountName}/campaigns/metrics/daily")]
        [ProducesResponseType(typeof(DailyCampaignMetrics), 200)]
        [Produces("application/json")]
        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        public async Task<IActionResult> GetDailyCampaignsMetrics(string accountName, [FromQuery] BasicDateFilter dateFilter)
        {
            if (!dateFilter.StartDate.HasValue || !dateFilter.EndDate.HasValue)
            {
                return new BadRequestObjectResult("StartDate and EndDate are required fields");
            }
            var startDate = dateFilter.StartDate.Value.UtcDateTime;
            var endDate = dateFilter.EndDate.Value.UtcDateTime;

            var result = await _campaignRepository.GetDailyCampaignsMetrics(accountName, startDate, endDate);

            return new OkObjectResult(result);
        }


        /// <summary>
        /// Return an object summarizing the sent campaingns performance of an user
        /// </summary>
        /// <param name="accountName">User name</param>
        /// <param name="dateFilter">A basic date range filter, </param>
        /// <remarks>Dates must be valid UtcTime with timezone</remarks>
        /// <param name="pagingFilter">Pagination filter including page number and page size.</param>
        /// <param name="campaignFilter">Filter object containing optional parameters such as campaign name, type, and from email.</param>
        [HttpGet]
        [Route("{accountName}/campaigns/metrics/sent")]
        [ProducesResponseType(typeof(BaseCollectionPage<SentCampaignMetrics>), 200)]
        [Produces("application/json")]
        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        public async Task<IActionResult> GetDailyCampaignsMetrics(string accountName, [FromQuery] BasicPagingFilter pagingFilter, [FromQuery] BasicDateFilter dateFilter, [FromQuery] BasicCampaignFilter campaignFilter)
        {
            DateTime? startDate = dateFilter.StartDate.HasValue ? dateFilter.StartDate.Value.UtcDateTime : null;
            DateTime? endDate = dateFilter.EndDate.HasValue ? dateFilter.EndDate.Value.UtcDateTime : null;
            string campaignType = campaignFilter.CampaignType.HasValue ? campaignFilter.CampaignType.ToString() : null;

            var totalCount = await _campaignRepository.GetSentCampaignsCount(
                accountName,
                startDate,
                endDate,
                campaignFilter.CampaignName,
                campaignType,
                campaignFilter.FromEmail,
                campaignFilter.Labels
                );

            var items = await _campaignRepository.GetSentCampaignsMetrics(
                accountName,
                pagingFilter.PageNumber,
                pagingFilter.PageSize,
                startDate,
                endDate,
                campaignFilter.CampaignName,
                campaignType,
                campaignFilter.FromEmail,
                campaignFilter.Labels
                );

            var pagedResult = new BaseCollectionPage<SentCampaignMetrics>
            {
                Items = items,
                CurrentPage = pagingFilter.PageNumber,
                PageSize = pagingFilter.PageSize,
                ItemsCount = totalCount
            };

            return new OkObjectResult(pagedResult);
        }

        /// <summary>
        /// Return an object summarizing the sent campaingns performance of an user
        /// </summary>
        /// <param name="accountName">User name</param>
        /// <param name="dateFilter">A basic date range filter, </param>
        /// <remarks>Dates must be valid UtcTime with timezone</remarks>
        /// <param name="pagingFilter">Pagination filter including page number and page size.</param>

        [HttpGet]
        [Route("{accountName}/campaigns/metrics/monthly")]
        [ProducesResponseType(typeof(BaseCollectionPage<MonthlyCampaignMetrics>), 200)]
        [Produces("application/json")]
        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        public async Task<IActionResult> GetMonthlyCampaignsMetrics(string accountName, [FromQuery] BasicPagingFilter pagingFilter, [FromQuery] BasicDateFilter dateFilter)
        {
            DateTime? startDate = dateFilter.StartDate.HasValue ? dateFilter.StartDate.Value.UtcDateTime : null;
            DateTime? endDate = dateFilter.EndDate.HasValue ? dateFilter.EndDate.Value.UtcDateTime : null;

            var totalCount = await _campaignRepository.GetMonthlyCampaignsCount(
                accountName,
                startDate,
                endDate
                );

            var items = await _campaignRepository.GetMonthlyCampaignsMetrics(
                accountName,
                pagingFilter.PageNumber,
                pagingFilter.PageSize,
                startDate,
                endDate
                );

            var pagedResult = new BaseCollectionPage<MonthlyCampaignMetrics>
            {
                Items = items,
                CurrentPage = pagingFilter.PageNumber,
                PageSize = pagingFilter.PageSize,
                ItemsCount = totalCount
            };

            return new OkObjectResult(pagedResult);
        }
    }
}
