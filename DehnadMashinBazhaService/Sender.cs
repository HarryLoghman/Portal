﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using MashinBazhaLibrary.Models;
using System.Linq;

namespace DehnadMashinBazhaService
{
    class Sender : SharedShortCodeServiceLibrary.Sender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void SendHandler()
        {
            base.SendHandler(MashinBazhaLibrary.SharedVariables.prp_serviceEntity, new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0));
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
        //        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MashinBazha", aggregatorName);
        //        int[] take = new int[(readSize / takeSize)];
        //        int[] skip = new int[(readSize / takeSize)];
        //        skip[0] = 0;
        //        take[0] = takeSize;
        //        for (int i = 1; i < take.Length; i++)
        //        {
        //            take[i] = takeSize;
        //            skip[i] = skip[i - 1] + takeSize;
        //        }
        //        using (var entity = new MashinBazhaEntities())
        //        {
        //            entity.Configuration.AutoDetectChangesEnabled = false;
        //            autochargeMessages = MashinBazhaLibrary.MessageHandler.GetUnprocessedAutochargeMessages(entity, readSize);
        //            eventbaseMessages = MashinBazhaLibrary.MessageHandler.GetUnprocessedEventbaseMessages(entity, readSize);
        //            onDemandMessages = MashinBazhaLibrary.MessageHandler.GetUnprocessedOnDemandMessages(entity, readSize);

        //            if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
        //            {
        //                TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
        //                var now = DateTime.Now.TimeOfDay;
        //                if (now < retryEndTime)
        //                {
        //                    entity.RetryUndeliveredMessages();
        //                }
        //            }

        //        }

        //        SendAutochargeMessages(autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //        SendEventbaseMessages(eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        //        SendOnDemandMessages(onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);

        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Error in SendHandler:" + e);
        //    }
        //}
        //public static void SendAutochargeMessages(List<AutochargeMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        //{
        //    if (messages.Count == 0)
        //        return;

        //    List<Task> TaskList = new List<Task>();
        //    for (int i = 0; i < take.Length; i++)
        //    {
        //        using (var entity = new MashinBazhaEntities())
        //        {
        //            var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
        //            if (aggregatorName == "Hamrahvas")
        //                TaskList.Add(MashinBazhaLibrary.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
        //            else if (aggregatorName == "PardisImi")
        //                TaskList.Add(MashinBazhaLibrary.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
        //        }
        //    }
        //    Task.WaitAll(TaskList.ToArray());
        //}

        //public static void SendEventbaseMessages(List<EventbaseMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        //{
        //    if (messages.Count == 0)
        //        return;

        //    List<Task> TaskList = new List<Task>();
        //    for (int i = 0; i < take.Length; i++)
        //    {
        //        using (var entity = new MashinBazhaEntities())
        //        {
        //            var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
        //            if (aggregatorName == "Hamrahvas")
        //                TaskList.Add(MashinBazhaLibrary.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
        //            else if (aggregatorName == "PardisImi")
        //                TaskList.Add(MashinBazhaLibrary.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
        //        }
        //    }
        //    Task.WaitAll(TaskList.ToArray());
        //}

        //public static void SendOnDemandMessages(List<OnDemandMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        //{
        //    if (messages.Count == 0)
        //        return;

        //    List<Task> TaskList = new List<Task>();
        //    for (int i = 0; i < take.Length; i++)
        //    {
        //        using (var entity = new MashinBazhaEntities())
        //        {
        //            var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
        //            if (aggregatorName == "Hamrahvas")
        //                TaskList.Add(MashinBazhaLibrary.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
        //            else if (aggregatorName == "PardisImi")
        //                TaskList.Add(MashinBazhaLibrary.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
        //        }
        //    }
        //    Task.WaitAll(TaskList.ToArray());
        //}
    }
}
