﻿using System;
using System.Linq;
using SharedLibrary.Models;
using BehAmooz500Library.Models;

namespace DehnadBehAmooz500Service
{
    class Eventbase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void InsertEventbaseMessagesToQueue()
        {
            try
            {
                using (var entity = new BehAmooz500Entities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var eventbaseContent = entity.EventbaseContents.FirstOrDefault(o => o.IsAddingMessagesToSendQueue == true && o.IsAddedToSendQueueFinished == false);
                    if (eventbaseContent == null)
                        return;
                    if (eventbaseContent.Content == null || eventbaseContent.Content.Trim() == "")
                        return;
                    var aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;
                    var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                    BehAmooz500Library.MessageHandler.AddEventbaseMessagesToQueue(eventbaseContent, aggregatorId);
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in Eventbase thread occurred: ", e);
            }
        }
    }
}
