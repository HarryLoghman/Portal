﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using System.Linq;
using System.Collections;

using SharedLibrary.Models.ServiceModel;

namespace DehnadPorShetabService
{
    class Sender : SharedShortCodeServiceLibrary.Sender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Sender() : base(Properties.Settings.Default.ServiceCode)
        {

        }
        public override void SendHandler()
        {
            base.SendHandler(new TimeSpan(7, 0, 0), new TimeSpan(23, 0, 0));
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
        //        string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;
        //        string serviceCode = Properties.Settings.Default.ServiceCode;
        //        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
        //        serviceAdditionalInfo["aggregatorServiceId"] = "98012000021509";
        //        var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(readSize, takeSize);
        //        var take = threadsNo["take"];
        //        var skip = threadsNo["skip"];

        //        Type entityType = typeof(SharedServiceEntities);

        //        autochargeMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.AutoCharge, 200)).OfType<AutochargeMessagesBuffer>().ToList();
        //        eventbaseMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.EventBase, 200)).OfType<EventbaseMessagesBuffer>().ToList();
        //        onDemandMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.OnDemand, 200)).OfType<OnDemandMessagesBuffer>().ToList();

        //        if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
        //        {
        //            TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
        //            var now = DateTime.Now.TimeOfDay;
        //            if (now < retryEndTime)
        //            {
        //                using (var entity = new SharedServiceEntities(SharedVariables.connectionStringInAppConfig))
        //                {
        //                    entity.RetryUndeliveredMessages();
        //                }
        //            }
        //        }
        //        if (DateTime.Now.Hour < 23 && DateTime.Now.Hour > 7)
        //        {
        //            //SharedLibrary.MessageHandler.SendSelectedMessages(entityType, autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //            //SharedLibrary.MessageHandler.SendSelectedMessages(entityType, eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //            SharedLibrary.MessageHandler.SendSelectedMessages(PorShetabLibrary.SharedVariables.prp_serviceEntity, autochargeMessages, 3);
        //            SharedLibrary.MessageHandler.SendSelectedMessages(PorShetabLibrary.SharedVariables.prp_serviceEntity, eventbaseMessages, 3);
        //        }
        //        //SharedLibrary.MessageHandler.SendSelectedMessages(entityType, onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //        SharedLibrary.MessageHandler.SendSelectedMessages(PorShetabLibrary.SharedVariables.prp_serviceEntity, onDemandMessages, 3);
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Error in SendHandler:" + e);
        //    }
        //}
    }
}

