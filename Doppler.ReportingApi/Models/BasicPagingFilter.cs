namespace Doppler.ReportingApi.Models
{
    public class BasicPagingFilter
    {
        /// <summary>
        /// Current page number (zero-based or one-based depending on usage).
        /// </summary>
        public int PageNumber { get; set; } = 0;

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
