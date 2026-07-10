using System;

namespace Doppler.ReportingApi.Models
{
    public class AssistedSalesSummaryItem
    {
        public int IdUser { get; set; }

        public int IdThirdPartyApp { get; set; }

        public DateTime StatsAt { get; set; }

        public decimal OrdersAmount { get; set; }

        public decimal AssistedOrderAmount { get; set; }

        public decimal OrdersTotal { get; set; }

        public decimal AssistedOrdersTotal { get; set; }

        public string Currency { get; set; }

        public DateTime UTCAddedDate { get; set; }
    }
}
