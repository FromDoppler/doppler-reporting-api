using Dapper;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Moq.Dapper;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.ReportingApi.Controllers
{
    public class DashboardControllerTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public DashboardControllerTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_email_campaigns_dashboard_should_return_valid_response()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();

            mockConnection
                .SetupDapperAsync(c => c.QueryAsync<EmailCampaignDashboardItem>(It.IsAny<string>(), It.IsAny<object>(), null, null, It.IsAny<CommandType?>()))
                .ReturnsAsync(new List<EmailCampaignDashboardItem>());

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Get,
                $"/{userName}/dashboard/email-campaigns?startDate=2025-06-10T00:00:00Z&endDate=2025-06-11T00:00:00Z&campaignType=regular")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_email_campaigns_dashboard_should_return_bad_request_when_dates_are_missing()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Get,
                $"/{userName}/dashboard/email-campaigns?startDate=2025-06-10T00:00:00Z&campaignType=regular")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
