namespace Doppler.ReportingApi.Models
{
    public class WebsiteActivityRfm
    {
        public int IdUser { get; set; }

        public int IdSegment { get; set; }

        public string SegmentName { get; set; }

        public int IdRFMSegment { get; set; }

        public int RFMPeriod { get; set; }

        public int SubscribersQty { get; set; }
    }
}
