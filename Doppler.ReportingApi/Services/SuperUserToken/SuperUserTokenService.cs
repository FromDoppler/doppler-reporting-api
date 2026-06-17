using System.Collections.Generic;
using Doppler.ReportingApi.Authorization;

namespace Doppler.ReportingApi.Services.SuperUserToken
{
    public class SuperUserTokenService : ISuperUserTokenService
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public SuperUserTokenService(IJwtTokenGenerator jwtTokenGenerator)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public string GenerateToken()
        {
            var payload = new Dictionary<string, object>
            {
                { "isSU", true }
            };

            return _jwtTokenGenerator.GenerateToken(payload);
        }
    }
}
