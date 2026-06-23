using Doppler.ReportingApi.DopplerSecurity;
using Doppler.ReportingApi.Infrastructure;
using Doppler.ReportingApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.ReportingApi.Controllers
{
    [Authorize]
    [ApiController]
    public class DashboardController
    {
        private readonly ILogger _logger;
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardController(ILogger<DashboardController> logger, IDashboardRepository dashboardRepository)
        {
            _logger = logger;
            _dashboardRepository = dashboardRepository;
        }

        #region EmailCampaigns

        /// <summary>
        /// Returns dashboard metrics for email campaigns by account and campaign type.
        /// </summary>
        /// <param name="accountName">User name.</param>
        /// <param name="dateFilter">A basic date range filter.</param>
        /// <param name="campaignType">Campaign type to filter.</param>
        /// <remarks>Dates must be valid UtcTime with timezone.</remarks>
        [HttpGet]
        [Route("{accountName}/dashboard/email-campaigns")]
        [ProducesResponseType(typeof(IEnumerable<EmailCampaignDashboardItem>), 200)]
        [Produces("application/json")]
        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        public async Task<IActionResult> GetEmailCampaignsDashboard(
            string accountName,
            [FromQuery] BasicDateFilter dateFilter,
            [FromQuery] string campaignType)
        {
            if (!dateFilter.StartDate.HasValue || !dateFilter.EndDate.HasValue)
            {
                return new BadRequestObjectResult("StartDate and EndDate are required fields");
            }

            var startDate = dateFilter.StartDate.Value.UtcDateTime;
            var endDate = dateFilter.EndDate.Value.UtcDateTime;

            var result = await _dashboardRepository.GetEmailCampaignsAsync(accountName, startDate, endDate, campaignType);

            return new OkObjectResult(result);
        }

        #endregion
    }
}
