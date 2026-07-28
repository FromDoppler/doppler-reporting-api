using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Services.PushContact;
using Doppler.ReportingApi.Services.SuperUserToken;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Doppler.ReportingApi.Test.Services
{
    public class PushContactSdkTest
    {
        [Fact]
        public async Task GetDomainStatsPerDayAsync_should_send_token_and_deserialize_response()
        {
            // Arrange
            var superUserTokenService = new Mock<ISuperUserTokenService>();
            superUserTokenService.Setup(x => x.GenerateToken()).Returns("super-user-token");

            var handler = new StubHttpMessageHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
                Assert.Equal("super-user-token", request.Headers.Authorization.Parameter);
                Assert.Equal(
                    "https://push.example.com/doppler-push-contact/domains/example.com/stats-per-day?startDate=2025-06-10T00%3A00%3A00.0000000Z&endDate=2025-06-11T00%3A00%3A00.0000000Z",
                    request.RequestUri.ToString());

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"domain\":\"example.com\",\"startDate\":\"2025-06-10T00:00:00+00:00\",\"endDate\":\"2025-06-11T00:00:00+00:00\",\"items\":[{\"from\":\"2025-06-10T00:00:00+00:00\",\"to\":\"2025-06-11T00:00:00+00:00\",\"added\":3,\"deleted\":1}]}")
                };

                return Task.FromResult(response);
            });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "BASEURL_PUSH_CONTACT", "https://push.example.com/doppler-push-contact" }
                })
                .Build();
            var sdk = new PushContactSdk(new HttpClient(handler), configuration, superUserTokenService.Object);

            // Act
            var response = await sdk.GetDomainStatsPerDayAsync(
                "example.com",
                new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 6, 11, 0, 0, 0, DateTimeKind.Utc));

            // Assert
            Assert.Equal("example.com", response.Domain);
            Assert.Single(response.Items);
            Assert.Equal(3, response.Items[0].Added);
            Assert.Equal(1, response.Items[0].Deleted);
            superUserTokenService.Verify(x => x.GenerateToken(), Times.Once);
        }

        [Fact]
        public async Task GetDomainStatsPerDayAsync_should_support_base_url_with_trailing_slash()
        {
            // Arrange
            var superUserTokenService = new Mock<ISuperUserTokenService>();
            superUserTokenService.Setup(x => x.GenerateToken()).Returns("super-user-token");

            var handler = new StubHttpMessageHandler(request =>
            {
                Assert.Equal(
                    "https://push.example.com/doppler-push-contact/domains/example.com/stats-per-day?startDate=2025-06-10T00%3A00%3A00.0000000Z&endDate=2025-06-11T00%3A00%3A00.0000000Z",
                    request.RequestUri.ToString());

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"domain\":\"example.com\",\"startDate\":\"2025-06-10T00:00:00+00:00\",\"endDate\":\"2025-06-11T00:00:00+00:00\",\"items\":[]}")
                });
            });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "BASEURL_PUSH_CONTACT", "https://push.example.com/doppler-push-contact/" }
                })
                .Build();
            var sdk = new PushContactSdk(new HttpClient(handler), configuration, superUserTokenService.Object);

            // Act
            await sdk.GetDomainStatsPerDayAsync(
                "example.com",
                new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 6, 11, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task GetDomainStatsPerDayAsync_should_throw_an_api_exception_for_unsuccessful_responses()
        {
            // Arrange
            var superUserTokenService = new Mock<ISuperUserTokenService>();
            superUserTokenService.Setup(x => x.GenerateToken()).Returns("super-user-token");

            var handler = new StubHttpMessageHandler(request =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{\"error\":\"not-found\"}")
                }));

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "BASEURL_PUSH_CONTACT", "https://push.example.com/doppler-push-contact" }
                })
                .Build();
            var sdk = new PushContactSdk(new HttpClient(handler), configuration, superUserTokenService.Object);

            // Act
            var exception = await Assert.ThrowsAsync<PushContactApiException>(() =>
                sdk.GetDomainStatsPerDayAsync(
                    "example.com",
                    new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 6, 11, 0, 0, 0, DateTimeKind.Utc)));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GetPushNotificationDashboardKpiData_should_send_post_payload_token_and_deserialize_response()
        {
            // Arrange
            var superUserTokenService = new Mock<ISuperUserTokenService>();
            superUserTokenService.Setup(x => x.GenerateToken()).Returns("super-user-token");

            var handler = new StubHttpMessageHandler(async request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
                Assert.Equal("super-user-token", request.Headers.Authorization.Parameter);
                Assert.Equal(
                    "https://push.example.com/doppler-push-contact/domains/push-stats",
                    request.RequestUri.ToString());

                var body = await request.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(body);
                Assert.Equal("alpha-demo.example.test", json.RootElement.GetProperty("domains")[0].GetString());
                Assert.Equal("beta-lab.example.test", json.RootElement.GetProperty("domains")[1].GetString());
                Assert.Equal(new DateTimeOffset(2026, 6, 23, 14, 38, 18, 67, TimeSpan.Zero), json.RootElement.GetProperty("from").GetDateTimeOffset());
                Assert.Equal(new DateTimeOffset(2026, 7, 23, 14, 38, 18, 67, TimeSpan.Zero), json.RootElement.GetProperty("to").GetDateTimeOffset());

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"from\":\"2026-06-23T14:38:18.067+00:00\",\"to\":\"2026-07-23T14:38:18.067+00:00\",\"items\":[{\"domain\":\"alpha-demo.example.test\",\"found\":true,\"pushStats\":{\"sent\":0,\"delivered\":0,\"totalClicks\":0,\"subscribed\":3,\"unsubscribed\":0,\"currentSubscribers\":43}}],\"totals\":{\"sent\":0,\"delivered\":0,\"totalClicks\":0,\"subscribed\":3,\"unsubscribed\":0,\"currentSubscribers\":43}}")
                };

                return response;
            });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "BASEURL_PUSH_CONTACT", "https://push.example.com/doppler-push-contact" }
                })
                .Build();
            var sdk = new PushContactSdk(new HttpClient(handler), configuration, superUserTokenService.Object);

            // Act
            var response = await sdk.GetPushNotificationDashboardKpiData(
                new DateTime(2026, 6, 23, 14, 38, 18, 67, DateTimeKind.Utc),
                new DateTime(2026, 7, 23, 14, 38, 18, 67, DateTimeKind.Utc),
                new[] { "alpha-demo.example.test", "beta-lab.example.test" });

            // Assert
            Assert.Equal(new DateTimeOffset(2026, 6, 23, 14, 38, 18, 67, TimeSpan.Zero), response.From);
            Assert.Equal(new DateTimeOffset(2026, 7, 23, 14, 38, 18, 67, TimeSpan.Zero), response.To);
            Assert.Single(response.Items);
            Assert.Equal("alpha-demo.example.test", response.Items[0].Domain);
            Assert.True(response.Items[0].Found);
            Assert.Equal(43, response.Totals.CurrentSubscribers);
            superUserTokenService.Verify(x => x.GenerateToken(), Times.Once);
        }

        private class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

            public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                return _handler(request);
            }
        }
    }
}
