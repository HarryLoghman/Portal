using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;
using System.Linq;
using System.Collections;
using System.Net.Http;
using System.Data.Entity;
using System.Xml.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DehnadHoshangService
{
    class Sender : SharedShortCodeServiceLibrary.Sender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Sender() : base(Properties.Settings.Default.ServiceCode)
        {

        }
        public override void SendHandler()
        {
            base.SendHandler(new TimeSpan(7, 0, 0), new TimeSpan(21, 0, 0));
        }
        //public void SendHandler()
        //{
        //    try
        //    {
        //        var today = DateTime.Now.Date;
        //        List<AutochargeMessagesBuffer> autochargeMessages;
        //        List<EventbaseMessagesBuffer> eventbaseMessages;
        //        List<OnDemandMessagesBuffer> onDemandMessages;
        //        int readSize = Convert.ToInt32(Properties.Settings.Default.ReadSize);
        //        int takeSize = Convert.ToInt32(Properties.Settings.Default.Take);
        //        bool retryNotDelieveredMessages = Properties.Settings.Default.RetryNotDeliveredMessages;
        //        string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode);
        //        string serviceCode = Properties.Settings.Default.ServiceCode;
        //        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);

        //        var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(readSize, takeSize);
        //        var take = threadsNo["take"];
        //        var skip = threadsNo["skip"];

        //        Type entityType = typeof(HoshangEntities);

        //        autochargeMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.AutoCharge, readSize)).OfType<AutochargeMessagesBuffer>().ToList();
        //        eventbaseMessages = ((IEnumerable)GetUnprocessedMessages(SharedLibrary.MessageHandler.MessageType.EventBase, readSize)).OfType<EventbaseMessagesBuffer>().ToList();
        //        onDemandMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.OnDemand, readSize)).OfType<OnDemandMessagesBuffer>().ToList();

        //        if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
        //        {
        //            TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
        //            var now = DateTime.Now.TimeOfDay;
        //            if (now < retryEndTime)
        //            {
        //                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
        //                {
        //                    entity.RetryUndeliveredMessages();
        //                }
        //            }
        //        }

        //        SharedLibrary.MessageHandler.SendSelectedMessages(entityType, onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //        if (DateTime.Now.Hour < 21 && DateTime.Now.Hour > 7)
        //        {
        //            SharedLibrary.MessageHandler.SendSelectedMessages(entityType, autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //            SendSelectedMessages(eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Error in SendHandler:" + e);
        //    }
        //}

        //public static dynamic GetUnprocessedMessages(SharedLibrary.MessageHandler.MessageType messageType, int readSize)
        //{
        //    var today = DateTime.Now.Date;
        //    var maxRetryCount = SharedLibrary.MessageSender.retryCountMax;
        //    var retryPauseBeforeSendByMinute = SharedLibrary.MessageSender.retryPauseBeforeSendByMinute;
        //    var retryTimeOut = DateTime.Now.AddMinutes(retryPauseBeforeSendByMinute);
        //    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
        //    {
        //        entity.Configuration.AutoDetectChangesEnabled = false;

        //        if (messageType == SharedLibrary.MessageHandler.MessageType.AutoCharge)
        //            return ((IEnumerable<dynamic>)entity.AutochargeMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/ && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
        //        else if (messageType == SharedLibrary.MessageHandler.MessageType.EventBase)
        //            return ((IEnumerable<dynamic>)entity.EventbaseMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/ && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
        //        else if (messageType == SharedLibrary.MessageHandler.MessageType.OnDemand)
        //            return ((IEnumerable<dynamic>)entity.OnDemandMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
        //        else
        //            return new List<dynamic>();
        //    }
        //}

        //public static void SendSelectedMessages(List<EventbaseMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        //{
        //    if (messages.Count() == 0)
        //        return;

        //    List<Task> TaskList = new List<Task>();
        //    for (int i = 0; i < take.Length; i++)
        //    {
        //        var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
        //        if (aggregatorName == "Telepromo")
        //            TaskList.Add(SendMesssagesToTelepromo(chunkedMessages, serviceAdditionalInfo));
        //    }
        //    Task.WaitAll(TaskList.ToArray());
        //}


    }
}
