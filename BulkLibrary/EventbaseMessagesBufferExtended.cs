using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkLibrary
{
    public class EventbaseMessagesBufferExtended : SharedShortCodeServiceLibrary.SharedModel.EventbaseMessagesBuffer
    {
        public EventbaseMessagesBufferExtended(SharedShortCodeServiceLibrary.SharedModel.EventbaseMessagesBuffer eventbase)
        {
            this.AggregatorId = eventbase.AggregatorId;
            this.Content = eventbase.Content;
            this.ContentId = eventbase.ContentId;
            this.DateAddedToQueue = eventbase.DateAddedToQueue;
            this.DateLastTried = eventbase.DateLastTried;
            this.DeliveryDescription = eventbase.DeliveryDescription;
            this.DeliveryStatus = eventbase.DeliveryStatus;
            this.Id = eventbase.Id;
            this.ImiChargeCode = eventbase.ImiChargeCode;
            this.ImiChargeKey = eventbase.ImiChargeKey;
            this.ImiMessageType = eventbase.ImiMessageType;
            this.MessagePoint = eventbase.MessagePoint;
            this.MessageType = eventbase.MessageType;
            this.MobileNumber = eventbase.MobileNumber;
            this.PersianDateAddedToQueue = eventbase.PersianDateAddedToQueue;
            this.PersianSentDate = eventbase.PersianSentDate;
            this.Price = eventbase.Price;
            this.ProcessStatus = eventbase.ProcessStatus;
            this.prp_IsSucceeded = false;
            this.prp_payload = null;
            this.prp_resultDescription = null;
            this.prp_url = null;
            this.ReferenceId = eventbase.ReferenceId;
            this.RetryCount = eventbase.RetryCount;
            this.SentDate = eventbase.SentDate;
            this.ServiceId = eventbase.ServiceId;
            this.SubscriberId = eventbase.SubscriberId;
            this.SubUnSubMoMssage = eventbase.SubUnSubMoMssage;
            this.SubUnSubType = eventbase.SubUnSubType;
            this.Tag = eventbase.Tag;

        }
        public string prp_resultDescription { get; set; }
        public bool prp_IsSucceeded { get; set; }
        public string prp_url { get; set; }
        public string prp_payload { get; set; }
    }
}
