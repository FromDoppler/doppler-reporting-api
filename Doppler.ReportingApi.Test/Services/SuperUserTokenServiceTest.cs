using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Doppler.ReportingApi.Authorization;
using Doppler.ReportingApi.Services.SuperUserToken;
using Moq;
using Xunit;

namespace Doppler.ReportingApi.Test.Services
{
    public class SuperUserTokenServiceTest
    {
        [Fact]
        public void GenerateToken_should_generate_a_super_user_token()
        {
            // Arrange
            var jwtTokenGenerator = new Mock<IJwtTokenGenerator>();
            jwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<System.Collections.Generic.Dictionary<string, object>>(), null))
                .Returns(new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: new[]
                {
                    new System.Security.Claims.Claim("isSU", bool.TrueString)
                })));

            var superUserTokenService = new SuperUserTokenService(jwtTokenGenerator.Object);

            // Act
            var token = superUserTokenService.GenerateToken();
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            // Assert
            Assert.Equal(bool.TrueString, jwt.Claims.Single(x => x.Type == "isSU").Value);
        }
    }
}
