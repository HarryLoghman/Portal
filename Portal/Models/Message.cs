using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.Models
{
    public class Message
    {
        public string MobileNumber { get; set; }
        public string ShortCode { get; set; }
        public string Content { get; set; }
        public string ReceiveTime { get; set; }
        public string MessageId { get; set; }
        public long ServiceId { get; set; }
        public long ContentId { get; set; }
        public long SubscriberId { get; set; }
        public int MessageType { get; set; }
        public int ImiChargeCode { get; set; }
        public int ImiMessageType { get; set; }
        public int ProcessStatus { get; set; }
        public long MobileOperator { get; set; }
        public long OperatorPlan { get; set; }
        public long AggregatorId { get; set; }
    }
}