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
        public int ContentId { get; set; }
        public int SubscriberId { get; set; }
        public int MessageType { get; set; }
        public int ImiChargeCode { get; set; }
        public int ImiMessageType { get; set; }
        public int ProcessStatus { get; set; }
        public int MobileOperator { get; set; }
        public int OperatorPlan { get; set; }
    }
}