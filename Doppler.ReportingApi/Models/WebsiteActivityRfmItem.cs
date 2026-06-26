namespace Doppler.ReportingApi.Models
{
    public class WebsiteActivityRfmItem
    {
        public int IdUser { get; set; }

        public int IdSegment { get; set; }

        public string SegmentName { get; set; }

        public int IdRFMSegment { get; set; }

        public int SubscribersQty { get; set; }
    }
}
