using System;
using System.Threading.Tasks;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Services.SuperUserToken;

namespace Doppler.ReportingApi.Services.PushContact
{
    public class PushContactService : IPushContactService
    {
        private readonly IPushContactSdk _pushContactSdk;
        private readonly ISuperUserTokenService _superUserTokenService;

        public PushContactService(IPushContactSdk pushContactSdk, ISuperUserTokenService superUserTokenService)
        {
            _pushContactSdk = pushContactSdk;
            _superUserTokenService = superUserTokenService;
        }

        public Task<DomainStatsPerDayModel> GetDomainStatsPerDayAsync(
            string name,
            DateTime startDate,
            DateTime endDate)
        {
            var token = _superUserTokenService.GenerateToken();

            return _pushContactSdk.GetDomainStatsPerDayAsync(
                name,
                startDate,
                endDate,
                token);
        }
    }
}
