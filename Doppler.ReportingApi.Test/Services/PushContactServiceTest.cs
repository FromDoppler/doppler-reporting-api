using System;
using System.Threading.Tasks;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Services.PushContact;
using Moq;
using Xunit;

namespace Doppler.ReportingApi.Test.Services
{
    public class PushContactServiceTest
    {
        [Fact]
        public async Task GetDomainStatsPerDayAsync_should_delegate_to_sdk()
        {
            // Arrange
            var pushContactSdk = new Mock<IPushContactSdk>();
            var expectedResponse = new DomainStatsPerDayModel
            {
                Domain = "example.com"
            };

            pushContactSdk
                .Setup(x => x.GetDomainStatsPerDayAsync("example.com", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(expectedResponse);

            var service = new PushContactService(pushContactSdk.Object);
            var startDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2025, 6, 11, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var response = await service.GetDomainStatsPerDayAsync("example.com", startDate, endDate);

            // Assert
            Assert.Same(expectedResponse, response);
        }
    }
}
