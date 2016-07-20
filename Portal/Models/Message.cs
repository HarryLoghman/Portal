using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.Models
{
    public class MessageObject
    {
        public string MobileNumber { get; set; }
        public string Address { get; set; }
        public string ShortCode { get; set; }
        public string Content { get; set; }
        public string Message { get; set; }
        public string ReceiveTime { get; set; }
        public string MessageId { get; set; }
        public long ServiceId { get; set; }
        public long ContentId { get; set; }
        public long SubscriberId { get; set; }
        public int MessageType { get; set; }
        public int ImiChargeCode { get; set; }
        public int ImiMessageType { get; set; }
        public string ImiChargeKey { get; set; }
        public int ProcessStatus { get; set; }
        public long MobileOperator { get; set; }
        public long OperatorPlan { get; set; }
        public long AggregatorId { get; set; }
        public string SubUnSubMoMssage { get; set; }
        public  byte SubUnSubType { get; set; }
        public int Point { get; set; }
        public int? Tag { get; set; }
        public bool IsReceivedFromIntegratedPanel { get; set; }
    }
}