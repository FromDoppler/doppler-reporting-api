using Doppler.ReportingApi.DopplerSecurity;
using Doppler.ReportingApi.Infrastructure;
using Doppler.ReportingApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

        [HttpGet]
        [Route("{accountName}/dashboard/emailcampaigns")]
        [Route("{accountName}/dashboard/emailcampaings")]
        [ProducesResponseType(typeof(EmailCampaignsDashboard), 200)]
        [Produces("application/json")]
        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        public async Task<IActionResult> GetEmailCampaignsDashboard(
            string accountName,
            [FromQuery] BasicDateFilter dateFilter,
            [FromQuery] EmailCampaignsDashboardFilter dashboardFilter)
        {
            if (!dateFilter.StartDate.HasValue || !dateFilter.EndDate.HasValue)
            {
                return new BadRequestObjectResult("StartDate and EndDate are required fields");
            }

            var result = await _dashboardRepository.GetEmailCampaignsDashboardAsync(
                accountName,
                dateFilter.StartDate.Value.UtcDateTime,
                dateFilter.EndDate.Value.UtcDateTime,
                dashboardFilter.CampaignType,
                dashboardFilter.ChartPeriod);

            return new OkObjectResult(result);
        }
    }
}
