using System;

namespace Doppler.ReportingApi.Services.PushContact
{
    public class PushContactApiCommunicationException : Exception
    {
        public PushContactApiCommunicationException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
