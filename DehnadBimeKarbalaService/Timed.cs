﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BimeKarbalaLibrary.Models;
using BimeKarbalaLibrary;
using System.Data.Entity;
using SharedLibrary.Models;

namespace DehnadBimeKarbalaService
{
    public class Timed
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void ProcessTempMessageBufferTable()
        {
            try
            {
                using (var entity = new BimeKarbalaEntities())
                {
                    var now = DateTime.Now;
                    var tempMessageBuffer = entity.TimedTempMessagesBuffers.Where(o => now > DbFunctions.AddMinutes(o.DateAddedToQueue, 2));
                    if (tempMessageBuffer == null)
                        return;
                    var messages = new List<MessageObject>();
                    foreach (var messageItem in tempMessageBuffer.ToList())
                    {
                        var message = SharedLibrary.MessageHandler.CreateMessageFromMessageBuffer(messageItem.SubscriberId, messageItem.MobileNumber, messageItem.ServiceId, messageItem.Content, messageItem.ContentId, (SharedLibrary.MessageHandler.MessageType)messageItem.MessageType, (SharedLibrary.MessageHandler.ProcessStatus)messageItem.ProcessStatus, messageItem.ImiMessageType, messageItem.ImiChargeCode, messageItem.ImiChargeKey, messageItem.AggregatorId, messageItem.MessagePoint.GetValueOrDefault(), messageItem.Tag, 0, "0", messageItem.Price.GetValueOrDefault());
                        messages.Add(message);
                    }
                    BimeKarbalaLibrary.MessageHandler.InsertBulkMessagesToQueue(messages);
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
