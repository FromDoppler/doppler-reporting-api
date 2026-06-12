using System;
using System.Net;

namespace Doppler.ReportingApi.Services.PushContact
{
    public class PushContactApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ResponseContent { get; }

        public PushContactApiException(HttpStatusCode statusCode, string responseContent)
            : base($"PushContact API returned status code {(int)statusCode}.")
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}
