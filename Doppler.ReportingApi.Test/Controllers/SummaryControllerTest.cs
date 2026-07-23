using Dapper;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Services.PushContact;
using Doppler.ReportingApi.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
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

            var client = CreateClient(mockConnection);

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

            var client = CreateClient(mockConnection);

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
        public async Task Get_summary_assistedsales_should_return_valid_response()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();

            mockConnection
                .SetupDapperAsync(c => c.QueryAsync<AssistedSalesSummaryItem>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(Enumerable.Empty<AssistedSalesSummaryItem>());

            var client = CreateClient(mockConnection);

            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;
            var request = new HttpRequestMessage(HttpMethod.Get, $"/{userName}/summary/assistedsales?startDate={startDate.ToLongDateString()}&endDate={endDate.ToLongTimeString()}")
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
        public async Task Get_system_usage_should_return_valid_response()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();

            mockConnection
                .SetupDapperAsync(c => c.QueryAsync<SystemUsageSummary>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(Enumerable.Empty<SystemUsageSummary>());

            var client = CreateClient(mockConnection);

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

            var client = CreateClient(mockConnection);

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

            var client = CreateClient(mockConnection);

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
                .SetupDapperAsync(c => c.QueryAsync<WebsiteActivityRfm>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(new List<WebsiteActivityRfm>
                {
                    new WebsiteActivityRfm
                    {
                        IdUser = 126712,
                        IdSegment = 45255205,
                        SegmentName = "Clientes estrella",
                        IntegrationName = "Shopify",
                        IdRFMSegment = 1,
                        RFMPeriod = 120,
                        SubscribersQty = 0
                    },
                    new WebsiteActivityRfm
                    {
                        IdUser = 126712,
                        IdSegment = 45255204,
                        SegmentName = "Clientes fieles",
                        IntegrationName = "Shopify",
                        IdRFMSegment = 2,
                        RFMPeriod = 120,
                        SubscribersQty = 0
                    }
                });

            var client = CreateClient(mockConnection);

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Get,
                $"/{userName}/dashboard/website-activity/rfm")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            });
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(126712, json.RootElement.GetProperty("idUser").GetInt32());
            Assert.Equal(120, json.RootElement.GetProperty("rfmPeriod").GetInt32());
            Assert.Equal("Shopify", json.RootElement.GetProperty("integrationName").GetString());
            Assert.Equal(2, json.RootElement.GetProperty("segments").GetArrayLength());
            Assert.False(json.RootElement.GetProperty("segments")[0].TryGetProperty("idUser", out _));
            Assert.False(json.RootElement.GetProperty("segments")[0].TryGetProperty("rfmPeriod", out _));
        }

        [Fact]
        public async Task Get_website_activity_rfm_should_return_empty_segments_when_no_data()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();

            mockConnection
                .SetupDapperAsync(c => c.QueryAsync<WebsiteActivityRfm>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(Enumerable.Empty<WebsiteActivityRfm>());

            var client = CreateClient(mockConnection);

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Get,
                $"/{userName}/dashboard/website-activity/rfm")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            });
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, json.RootElement.GetProperty("segments").GetArrayLength());
            Assert.True(json.RootElement.GetProperty("idUser").ValueKind == JsonValueKind.Null);
            Assert.True(json.RootElement.GetProperty("rfmPeriod").ValueKind == JsonValueKind.Null);
            Assert.True(json.RootElement.GetProperty("integrationName").ValueKind == JsonValueKind.Null);
        }

        [Fact]
        public async Task Get_push_notification_dashboard_kpi_data_should_return_valid_response()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();
            var mockPushContactService = new Mock<IPushContactService>();

            mockPushContactService
                .Setup(x => x.GetPushNotificationDashboardKpiData(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.Is<IEnumerable<string>>(domains => domains.SequenceEqual(new[]
                    {
                        "alpha-demo.example.test",
                        "beta-lab.example.test"
                    }))))
                .ReturnsAsync(new PushNotificationDashboardKpiDataModel
                {
                    From = new DateTimeOffset(2026, 6, 23, 14, 38, 18, 67, TimeSpan.Zero),
                    To = new DateTimeOffset(2026, 7, 23, 14, 38, 18, 67, TimeSpan.Zero),
                    Items = new List<PushNotificationDashboardKpiItemModel>
                    {
                        new PushNotificationDashboardKpiItemModel
                        {
                            Domain = "alpha-demo.example.test",
                            Found = true,
                            PushStats = new PushNotificationDashboardKpiStatsModel
                            {
                                CurrentSubscribers = 43
                            }
                        }
                    },
                    Totals = new PushNotificationDashboardKpiStatsModel
                    {
                        CurrentSubscribers = 43
                    }
                });

            var client = CreateClient(mockConnection, mockPushContactService);

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Get,
                $"/{userName}/dashboard/other-channels/notificationpush?startDate=2026-06-23T14:38:18.067Z&endDate=2026-07-23T14:38:18.067Z&domains=alpha-demo.example.test&domains=beta-lab.example.test")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            });
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(43, json.RootElement.GetProperty("totals").GetProperty("currentSubscribers").GetInt32());
        }

        [Fact]
        public async Task Get_push_notification_dashboard_kpi_data_should_return_bad_request_when_domains_are_missing()
        {
            // Arrange
            var userName = "test1@test.com";
            var token = TestJwtTokenFactory.ValidAccount123Test1;
            var mockConnection = new Mock<DbConnection>();
            var mockPushContactService = new Mock<IPushContactService>();

            var client = CreateClient(mockConnection, mockPushContactService);

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Get,
                $"/{userName}/dashboard/other-channels/notificationpush?startDate=2026-06-23T14:38:18.067Z&endDate=2026-07-23T14:38:18.067Z")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            mockPushContactService.Verify(
                x => x.GetPushNotificationDashboardKpiData(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IEnumerable<string>>()),
                Times.Never);
        }

        private HttpClient CreateClient(
            Mock<DbConnection> mockConnection,
            Mock<IPushContactService> mockPushContactService = null)
        {
            mockPushContactService ??= new Mock<IPushContactService>();

            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(mockPushContactService.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
        }
    }
}
