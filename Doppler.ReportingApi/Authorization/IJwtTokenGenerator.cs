using System.Collections.Generic;

namespace Doppler.ReportingApi.Authorization
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Dictionary<string, object> payload, Dictionary<string, object> extraParams = null);
    }
}
