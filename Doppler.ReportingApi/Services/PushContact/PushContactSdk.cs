using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Doppler.ReportingApi.Models;
using Doppler.ReportingApi.Services.SuperUserToken;
using Microsoft.Extensions.Configuration;

namespace Doppler.ReportingApi.Services.PushContact
{
    public class PushContactSdk : IPushContactSdk
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly string _pushContactApiBaseUrl;
        private readonly ISuperUserTokenService _superUserTokenService;

        public PushContactSdk(
            HttpClient httpClient,
            IConfiguration configuration,
            ISuperUserTokenService superUserTokenService)
        {
            _httpClient = httpClient;
            _superUserTokenService = superUserTokenService;
            _pushContactApiBaseUrl = configuration.GetValue<string>("BASEURL_PUSH_CONTACT");

            if (string.IsNullOrWhiteSpace(_pushContactApiBaseUrl))
            {
                throw new InvalidOperationException("BASEURL_PUSH_CONTACT configuration is required to call PushContact API.");
            }
        }

        public async Task<DomainStatsPerDayModel> GetDomainStatsPerDayAsync(
            string name,
            DateTime startDate,
            DateTime endDate)
        {
            var token = _superUserTokenService.GenerateToken();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_pushContactApiBaseUrl.TrimEnd('/')}/domains/{Uri.EscapeDataString(name)}/stats-per-day?startDate={Uri.EscapeDataString(startDate.ToString("o"))}&endDate={Uri.EscapeDataString(endDate.ToString("o"))}");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (HttpRequestException exception)
            {
                throw new PushContactApiCommunicationException("An error occurred while calling PushContact API.", exception);
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new PushContactApiException(response.StatusCode, responseContent);
            }

            try
            {
                var model = JsonSerializer.Deserialize<DomainStatsPerDayModel>(responseContent, JsonSerializerOptions);

                if (model == null)
                {
                    throw new PushContactApiCommunicationException("PushContact API returned an empty response body.");
                }

                return model;
            }
            catch (JsonException exception)
            {
                throw new PushContactApiCommunicationException("PushContact API returned an invalid response body.", exception);
            }
        }

        public Task<IEnumerable<PushNotificationDashboardKpiDataModel>> GetPushNotificationDashboardKpiData(
            string accountName,
            DateTime startDate,
            DateTime endDate,
            IEnumerable<string> domains)
        {
            var from = new DateTimeOffset(DateTime.SpecifyKind(startDate, DateTimeKind.Utc));
            var to = new DateTimeOffset(DateTime.SpecifyKind(endDate, DateTimeKind.Utc));
            var items = new List<PushNotificationDashboardKpiItemModel>
            {
                new PushNotificationDashboardKpiItemModel
                {
                    Domain = "newsletters.acme.com",
                    Found = true,
                    PushStats = new PushNotificationDashboardKpiStatsModel
                    {
                        Sent = 18450,
                        Delivered = 17210,
                        TotalClicks = 1328,
                        Subscribed = 214,
                        Unsubscribed = 37,
                        CurrentSubscribers = 4986
                    }
                },
                new PushNotificationDashboardKpiItemModel
                {
                    Domain = "shop.acme.com",
                    Found = true,
                    PushStats = new PushNotificationDashboardKpiStatsModel
                    {
                        Sent = 9620,
                        Delivered = 9054,
                        TotalClicks = 874,
                        Subscribed = 126,
                        Unsubscribed = 18,
                        CurrentSubscribers = 3124
                    }
                },
                new PushNotificationDashboardKpiItemModel
                {
                    Domain = "blog.acme.com",
                    Found = true,
                    PushStats = new PushNotificationDashboardKpiStatsModel
                    {
                        Sent = 4310,
                        Delivered = 3978,
                        TotalClicks = 291,
                        Subscribed = 48,
                        Unsubscribed = 9,
                        CurrentSubscribers = 1187
                    }
                }
            };
            var totals = new PushNotificationDashboardKpiStatsModel
            {
                Sent = items.Sum(x => x.PushStats.Sent),
                Delivered = items.Sum(x => x.PushStats.Delivered),
                TotalClicks = items.Sum(x => x.PushStats.TotalClicks),
                Subscribed = items.Sum(x => x.PushStats.Subscribed),
                Unsubscribed = items.Sum(x => x.PushStats.Unsubscribed),
                CurrentSubscribers = items.Sum(x => x.PushStats.CurrentSubscribers)
            };

            IEnumerable<PushNotificationDashboardKpiDataModel> response = new[]
            {
                new PushNotificationDashboardKpiDataModel
                {
                    From = from,
                    To = to,
                    Items = items,
                    Totals = totals
                }
            };

            return Task.FromResult(response);
        }

    }
}
