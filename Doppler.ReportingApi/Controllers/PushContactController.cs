using System;
using System.Threading.Tasks;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Services.PushContact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doppler.ReportingApi.Controllers
{
    [Authorize]
    [ApiController]
    public class PushContactController : ControllerBase
    {
        private readonly IPushContactService _pushContactService;

        public PushContactController(IPushContactService pushContactService)
        {
            _pushContactService = pushContactService;
        }

        [HttpGet]
        [Route("domains/{name}/stats-per-day")]
        public async Task<IActionResult> GetDomainStatsPerDay(
            string name,
            [FromQuery] BasicDateFilter dateFilter)
        {
            if (!dateFilter.StartDate.HasValue || !dateFilter.EndDate.HasValue)
            {
                return new BadRequestObjectResult("StartDate and EndDate are required fields");
            }

            var startDate = dateFilter.StartDate.Value.UtcDateTime;
            var endDate = dateFilter.EndDate.Value.UtcDateTime;

            var response = await _pushContactService.GetDomainStatsPerDayAsync(
                name,
                startDate,
                endDate);

            return new OkObjectResult(response);
        }
    }
}
