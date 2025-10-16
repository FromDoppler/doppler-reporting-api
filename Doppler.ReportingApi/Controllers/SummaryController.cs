using Doppler.ReportingApi.DopplerSecurity;
using Doppler.ReportingApi.Infrastructure;
using Doppler.ReportingApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Controllers
{
    [Authorize]
    [ApiController]
    public class SummaryController
    {
        private readonly ILogger _logger;
        private readonly ISummaryRepository _summaryRepository;

        public SummaryController(ILogger<SummaryController> logger, ISummaryRepository summaryRepository)
        {
            _logger = logger;
            _summaryRepository = summaryRepository;
        }

        /// <summary>
        /// Return an object summarizing the campaingns performance of an user
        /// </summary>
        /// <param name="accountName">User name</param>
        /// <param name="dateFilter">A basic date range filter, </param>
        /// <remarks>Dates must be valid UtcTime with timezone</remarks>
        [HttpGet]
        [Route("{accountName}/summary/campaigns")]
        [ProducesResponseType(typeof(CampaignsSummary), 200)]
        [Produces("application/json")]
        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        public async Task<IActionResult> GetCampaignsSummary(string accountName, [FromQuery] BasicDateFilter dateFilter)
        {
            if (!dateFilter.StartDate.HasValue || !dateFilter.EndDate.HasValue)
            {
                return new BadRequestObjectResult("StartDate and EndDate are required fields");
            }
            var startDate = dateFilter.StartDate.Value.UtcDateTime;
            var endDate = dateFilter.EndDate.Value.UtcDateTime;
            var result = await _summaryRepository.GetCampaignsSummaryByUserAsync(accountName, startDate, endDate);

            return new OkObjectResult(result);
        }

        /// <summary>
        /// Return an object summarizing the subscribers of an user
        /// </summary>
        /// <param name="accountName">User name</param>
        /// <param name="dateFilter">A basic date range filter </param>
        /// <remarks>Dates must be valid UtcTime with timezone</remarks>
        [HttpGet]
        [Route("{accountName}/summary/subscribers")]
        [ProducesResponseType(typeof(SubscribersSummary), 200)]
        [Produces("application/json")]
        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        public async Task<IActionResult> GetSubscribers(string accountName, [FromQuery] BasicDateFilter dateFilter)
        {
            if (!dateFilter.StartDate.HasValue || !dateFilter.EndDate.HasValue)
            {
                return new BadRequestObjectResult("StartDate and EndDate are required fields");
            }
            var startDate = dateFilter.StartDate.Value.UtcDateTime;
            var endDate = dateFilter.EndDate.Value.UtcDateTime;
            SubscribersSummary result = await _summaryRepository.GetSubscribersSummaryByUserAsync(accountName, startDate, endDate);

            return new OkObjectResult(result);
        }

        /// <summary>
        /// Returns an object with info about the use of Doppler by a particular user
        /// </summary>
        /// <param name="accountName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{accountName}/summary/system-usage")]
        [ProducesResponseType(typeof(SystemUsageSummary), 200)]
        [Produces("application/json")]
        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        public async Task<IActionResult> GetSystemUsage(string accountName)
        {
            var result = await _summaryRepository.GetSystemUsageAsync(accountName);

            return new OkObjectResult(result);
        }
    }
}
