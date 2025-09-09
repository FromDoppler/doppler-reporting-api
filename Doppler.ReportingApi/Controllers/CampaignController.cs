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
        public async Task<IActionResult> GetDailyCampaignsMetrics(string accountName, [FromQuery] BasicDatefilter dateFilter)
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
    }
}
