﻿using System;
using System.Linq;
using SharedLibrary.Models;
using FitShowLibrary.Models;

namespace DehnadFitShowService
{
    class Eventbase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void InsertEventbaseMessagesToQueue()
        {
            try
            {
                using (var entity = new FitShowEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var eventbaseContent = entity.EventbaseContents.FirstOrDefault(o => o.IsAddingMessagesToSendQueue == true && o.IsAddedToSendQueueFinished == false);
                    if (eventbaseContent == null)
                        return;
                    if (eventbaseContent.Content == null || eventbaseContent.Content.Trim() == "")
                        return;
                    var aggregatorName = Properties.Settings.Default.AggregatorName;
                    var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                    FitShowLibrary.MessageHandler.AddEventbaseMessagesToQueue(eventbaseContent, aggregatorId);
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in Eventbase thread occurred: ", e);
            }
        }
    }
}