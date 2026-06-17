using System;
using System.Threading.Tasks;
using Doppler.ReportingApi.Models;

namespace Doppler.ReportingApi.Services.PushContact
{
    public interface IPushContactSdk
    {
        Task<DomainStatsPerDayModel> GetDomainStatsPerDayAsync(
            string name,
            DateTime startDate,
            DateTime endDate);
    }
}
