using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Test.Utils;
using Doppler.ReportingApi.Services.PushContact;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Doppler.ReportingApi.Controllers
{
    public class PushContactControllerTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public PushContactControllerTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetDomainStatsPerDay_should_return_ok_when_service_returns_data()
        {
            // Arrange
            var mockService = new Mock<IPushContactService>();
            mockService
                .Setup(x => x.GetDomainStatsPerDayAsync("example.com", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new DomainStatsPerDayModel
                {
                    Domain = "example.com",
                    Items = new System.Collections.Generic.List<DomainStatsPerDayPeriodModel>()
                });

            var client = CreateClient(mockService);

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Get,
                "/domains/example.com/stats-per-day?startDate=2025-06-10T00:00:00Z&endDate=2025-06-11T00:00:00Z")
            {
                Headers = { { "Authorization", $"Bearer {ValidToken}" } }
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("example.com", json.RootElement.GetProperty("domain").GetString());
        }

        [Fact]
        public async Task GetDomainStatsPerDay_should_return_internal_server_error_when_service_throws()
        {
            // Arrange
            var mockService = new Mock<IPushContactService>();
            mockService
                .Setup(x => x.GetDomainStatsPerDayAsync("example.com", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new PushContactApiException(HttpStatusCode.NotFound, "{\"error\":\"upstream\"}"));

            var client = CreateClient(mockService);

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Get,
                "/domains/example.com/stats-per-day?startDate=2025-06-10T00:00:00Z&endDate=2025-06-11T00:00:00Z")
            {
                Headers = { { "Authorization", $"Bearer {ValidToken}" } }
            });

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        [Fact]
        public async Task GetDomainStatsPerDay_should_return_bad_request_when_dates_are_missing()
        {
            // Arrange
            var mockService = new Mock<IPushContactService>();
            var client = CreateClient(mockService);

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Get,
                "/domains/example.com/stats-per-day?startDate=2025-06-11T00:00:00Z")
            {
                Headers = { { "Authorization", $"Bearer {ValidToken}" } }
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            mockService.Verify(
                x => x.GetDomainStatsPerDayAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        private HttpClient CreateClient(Mock<IPushContactService> mockService)
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(mockService.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
        }
        private static string ValidToken => TestJwtTokenFactory.ValidAccount123Test1;
    }
}
