using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class ImiData
    {
        public int sid { get; set; }
        public string transId { get; set; }
        public int status { get; set; }
        public int? basePricePoint { get; set; }
        public string msisdn { get; set; }
        public string keyword { get; set; }
        public int validity { get; set; }
        public DateTime? next_renewal_date { get; set; }
        public string shortcode { get; set; }
        public int? billedPricePoint { get; set; }
        public int transStatus { get; set; }
        public string chargeCode { get; set; }
        public DateTime datetime { get; set; }
        public string eventType { get; set; }
        public string Channel { get; set; }
    }
}
