using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadFtpSyncAndChargingService
{
    class MCIftpItemInfo
    {
        public int sid { get; set; }
        public string trans_id { get; set; }
        public int status { get; set; }
        public int? base_price_point { get; set; }
        public string msisdn { get; set; }
        public string keyword { get; set; }
        public int validity { get; set; }
        public DateTime? next_renewal_date { get; set; }
        public string shortcode { get; set; }
        public int? billed_price_point { get; set; }
        public int trans_status { get; set; }
        public string chargeCode { get; set; }
        public DateTime datetime { get; set; }
        public string event_type { get; set; }
        public string channel { get; set; }


    }
}
