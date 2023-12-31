﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tabriz2018Library.Models;
using Tabriz2018Library;
using System.Data.Entity;
using SharedLibrary.Models;

namespace DehnadTabriz2018Service
{
    public class Timed
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void ProcessTempMessageBufferTable()
        {
            try
            {
                using (var entity = new Tabriz2018Entities())
                {
                    var tempMessageBuffer = entity.TimedTempMessagesBuffers.Where(o => o.DateAddedToQueue < DbFunctions.AddMinutes(o.DateAddedToQueue, 1));
                    if (tempMessageBuffer == null)
                        return;
                    var messages = new List<MessageObject>();
                    foreach (var messageItem in tempMessageBuffer.ToList())
                    {
                        var message = SharedLibrary.MessageHandler.CreateMessageFromMessageBuffer(messageItem.SubscriberId, messageItem.MobileNumber, messageItem.ServiceId, messageItem.Content, messageItem.ContentId, (SharedLibrary.MessageHandler.MessageType)messageItem.MessageType, (SharedLibrary.MessageHandler.ProcessStatus)messageItem.ProcessStatus, messageItem.ImiMessageType, messageItem.ImiChargeCode, messageItem.ImiChargeKey, messageItem.AggregatorId, messageItem.MessagePoint.GetValueOrDefault(), messageItem.Tag, 0, "0", messageItem.Price.GetValueOrDefault());
                        messages.Add(message);
                    }
                    Tabriz2018Library.MessageHandler.InsertBulkMessagesToQueue(messages);
                    entity.TimedTempMessagesBuffers.RemoveRange(tempMessageBuffer);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in ProcessTempMessageBufferTable: ", e);
            }
        }
    }
}
