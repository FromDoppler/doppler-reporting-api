using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Doppler.ReportingApi.Services.PushContact;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Doppler.ReportingApi.Test.Services
{
    public class PushContactSdkTest
    {
        [Fact]
        public async Task GetDomainStatsPerDayAsync_should_send_token_and_deserialize_response()
        {
            // Arrange
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
            var sdk = new PushContactSdk(new HttpClient(handler), configuration);

            // Act
            var response = await sdk.GetDomainStatsPerDayAsync(
                "example.com",
                new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 6, 11, 0, 0, 0, DateTimeKind.Utc),
                "super-user-token");

            // Assert
            Assert.Equal("example.com", response.Domain);
            Assert.Single(response.Items);
            Assert.Equal(3, response.Items[0].Added);
            Assert.Equal(1, response.Items[0].Deleted);
        }

        [Fact]
        public async Task GetDomainStatsPerDayAsync_should_throw_an_api_exception_for_unsuccessful_responses()
        {
            // Arrange
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
            var sdk = new PushContactSdk(new HttpClient(handler), configuration);

            // Act
            var exception = await Assert.ThrowsAsync<PushContactApiException>(() =>
                sdk.GetDomainStatsPerDayAsync(
                    "example.com",
                    new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 6, 11, 0, 0, 0, DateTimeKind.Utc),
                    "super-user-token"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
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
