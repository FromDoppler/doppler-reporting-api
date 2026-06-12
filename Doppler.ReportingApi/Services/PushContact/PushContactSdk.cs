using System;
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

    }
}
