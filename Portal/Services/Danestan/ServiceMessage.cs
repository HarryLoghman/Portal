using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Services.Danestan
{
    public class ServiceMessage
    {
        public static void InsertMessageToQueue(Message message)
        {
            using (var entity = new DanestanEntities())
            {
                if(message.AggregatorId == 0)
                    message.AggregatorId = Shared.MessageHandler.GetAggregatorId(message);

                var messageBuffer = new MessagesBuffer();
                messageBuffer.Content = message.Content;
                messageBuffer.ContentId = message.ContentId;
                messageBuffer.ImiChargeCode = message.ImiChargeCode;
                messageBuffer.ImiMessageType = message.ImiMessageType;
                messageBuffer.MobileNumber = message.MobileNumber;
                messageBuffer.MessageType = message.MessageType;
                messageBuffer.ProcessStatus = message.ProcessStatus;
                messageBuffer.ServiceId = message.ServiceId;
                messageBuffer.DateAddedToQueue = DateTime.Now;
                messageBuffer.AggregatorId = message.AggregatorId;
                messageBuffer.PersianDateAddedToQueue = Shared.Date.GetPersianDate(DateTime.Now);
                entity.MessagesBuffers.Add(messageBuffer);
                entity.SaveChanges();
            }
        }

        public static void InvalidContentWhenNotSubscribed(Message message, List<MessagesTemplate> messagesTemplate)
        {
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            InsertMessageToQueue(message);
        }
    }
}