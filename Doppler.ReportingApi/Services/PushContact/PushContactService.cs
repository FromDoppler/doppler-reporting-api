using System;
using System.Threading.Tasks;
using Doppler.ReportingApi.Models;

namespace Doppler.ReportingApi.Services.PushContact
{
    public class PushContactService : IPushContactService
    {
        private readonly IPushContactSdk _pushContactSdk;

        public PushContactService(IPushContactSdk pushContactSdk)
        {
            _pushContactSdk = pushContactSdk;
        }

        public Task<DomainStatsPerDayModel> GetDomainStatsPerDayAsync(
            string name,
            DateTime startDate,
            DateTime endDate)
        {
            return _pushContactSdk.GetDomainStatsPerDayAsync(
                name,
                startDate,
                endDate);
        }
    }
}
