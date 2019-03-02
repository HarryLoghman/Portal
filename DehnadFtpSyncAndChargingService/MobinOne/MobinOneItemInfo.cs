using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadFtpSyncAndChargingService
{
    class MobinOneItemInfo
    {
        public Nullable<System.DateTime> date { get; set; }
        public string service_name { get; set; }
        public string event_type { get; set; }
        public string requestedChannel { get; set; }
        public string download_channel { get; set; }
        public Nullable<decimal> eup { get; set; }
        public string action_type { get; set; }
        public string subscriber_type { get; set; }
        public string shortcode { get; set; }
        public Nullable<int> downloads { get; set; }
        public Nullable<int> revenue { get; set; }
    }
}
