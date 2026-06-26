using Dapper;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Moq.Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.ReportingApi.Controllers
{
    public class SummaryControllerTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public SummaryControllerTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_summary_campaigns_should_return_valid_response()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();

            mockConnection
                .SetupDapperAsync(c => c.QueryAsync<CampaignsSummary>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(Enumerable.Empty<CampaignsSummary>());

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;
            var request = new HttpRequestMessage(HttpMethod.Get, $"{userName}/summary/campaigns?startDate={startDate.ToLongDateString()}&endDate={endDate.ToLongTimeString()}")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.NotNull(content);
        }

        [Fact]
        public async Task Get_summary_subscribers_should_return_valid_response()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();

            mockConnection
                .SetupDapperAsync(c => c.QueryAsync<SubscribersSummary>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(Enumerable.Empty<SubscribersSummary>());

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;
            var request = new HttpRequestMessage(HttpMethod.Get, $"/{userName}/summary/subscribers?startDate={startDate.ToLongDateString()}&endDate={endDate.ToLongTimeString()}")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);
        }

        [Fact]
        public async Task Get_system_usage_should_return_valid_response()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();

            mockConnection
                .SetupDapperAsync(c => c.QueryAsync<SystemUsageSummary>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(Enumerable.Empty<SystemUsageSummary>());

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, $"/{userName}/summary/system-usage")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);
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

        [Fact]
        public async Task Get_website_activity_rfm_dashboard_should_return_valid_response()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();

            mockConnection
                .SetupDapperAsync(c => c.QueryAsync<WebsiteActivityRfmItem>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(new List<WebsiteActivityRfmItem>());

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
                $"/{userName}/dashboard/website-activity/rfm")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
