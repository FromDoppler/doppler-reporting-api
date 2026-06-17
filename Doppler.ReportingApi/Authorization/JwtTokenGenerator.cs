using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

namespace Doppler.ReportingApi.Authorization
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly SigningCredentials _signingCredentials;

        public JwtTokenGenerator(
            SigningCredentials signingCredentials,
            JwtSecurityTokenHandler tokenHandler)
        {
            _signingCredentials = signingCredentials;
            _tokenHandler = tokenHandler;
        }

        public string GenerateToken(Dictionary<string, object> payload, Dictionary<string, object> extraParams = null)
        {
            var jwtToken = _tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                SigningCredentials = _signingCredentials,
                Claims = payload
            }) as JwtSecurityToken;

            return _tokenHandler.WriteToken(jwtToken);
        }
    }
}
