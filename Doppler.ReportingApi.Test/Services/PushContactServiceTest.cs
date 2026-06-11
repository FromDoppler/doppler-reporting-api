using System;
using System.Threading.Tasks;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Services.PushContact;
using Doppler.ReportingApi.Services.SuperUserToken;
using Moq;
using Xunit;

namespace Doppler.ReportingApi.Test.Services
{
    public class PushContactServiceTest
    {
        [Fact]
        public async Task GetDomainStatsPerDayAsync_should_generate_a_token_and_delegate_to_sdk()
        {
            // Arrange
            var pushContactSdk = new Mock<IPushContactSdk>();
            var superUserTokenService = new Mock<ISuperUserTokenService>();
            var expectedResponse = new DomainStatsPerDayModel
            {
                Domain = "example.com"
            };

            superUserTokenService.Setup(x => x.GenerateToken()).Returns("super-user-token");
            pushContactSdk
                .Setup(x => x.GetDomainStatsPerDayAsync("example.com", It.IsAny<DateTime>(), It.IsAny<DateTime>(), "super-user-token"))
                .ReturnsAsync(expectedResponse);

            var service = new PushContactService(pushContactSdk.Object, superUserTokenService.Object);
            var startDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2025, 6, 11, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var response = await service.GetDomainStatsPerDayAsync("example.com", startDate, endDate);

            // Assert
            Assert.Same(expectedResponse, response);
            superUserTokenService.Verify(x => x.GenerateToken(), Times.Once);
        }
    }
}
