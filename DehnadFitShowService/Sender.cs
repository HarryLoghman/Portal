using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;
using System.Linq;
using System.Collections;
using System.Data.Entity;
using System.Net.Http;

namespace DehnadFitShowService
{
    class Sender : SharedShortCodeServiceLibrary.Sender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void SendHandler()
        {
            base.SendHandler(Properties.Settings.Default.ServiceCode, new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0));
        }
        //public void SendHandler()
        //{
        //    try
        //    {
        //        logs.Info("A1:start of send process");
        //        var today = DateTime.Now.Date;
        //        List<AutochargeMessagesBuffer> autochargeMessages;
        //        List<EventbaseMessagesBuffer> eventbaseMessages;
        //        List<OnDemandMessagesBuffer> onDemandMessages;
        //        int readSize = Convert.ToInt32(Properties.Settings.Default.ReadSize);
        //        int takeSize = Convert.ToInt32(Properties.Settings.Default.Take);
        //        bool retryNotDelieveredMessages = Properties.Settings.Default.RetryNotDeliveredMessages;
        //        string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;
        //        string serviceCode = Properties.Settings.Default.ServiceCode;
        //        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);

        //        var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(readSize, takeSize);
        //        var take = threadsNo["take"];
        //        var skip = threadsNo["skip"];

        //        Type entityType = typeof(FitShowEntities);

        //        onDemandMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.OnDemand, readSize)).OfType<OnDemandMessagesBuffer>().ToList();

        //        //if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
        //        //{
        //        //    TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
        //        //    var now = DateTime.Now.TimeOfDay;
        //        //    if (now < retryEndTime)
        //        //    {
        //        //        entity.RetryUndeliveredMessages();
        //        //    }
        //        //}

        //        if (DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20)
        //        {
        //            autochargeMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.AutoCharge, readSize)).OfType<AutochargeMessagesBuffer>().ToList();
        //            eventbaseMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.EventBase, readSize)).OfType<EventbaseMessagesBuffer>().ToList();
        //            eventbaseMessages.RemoveAll(o => o.MobileNumber == "09122137327");
        //            SharedLibrary.MessageHandler.SendSelectedMessages(entityType, autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //            SharedLibrary.MessageHandler.SendSelectedMessages(entityType, eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //        }
        //        SharedLibrary.MessageHandler.SendSelectedMessages(entityType, onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Error in SendHandler:" + e);
        //    }
        //    logs.Info("A5:end of send process");
        //}
    }
}
