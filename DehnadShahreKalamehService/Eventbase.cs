using System;
using System.Linq;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;

namespace DehnadShahreKalamehService
{
    class Eventbase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //public void InsertEventbaseMessagesToQueue()
        //{
        //    try
        //    {
        //        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
        //        {
        //            entity.Configuration.AutoDetectChangesEnabled = false;
        //            var eventbaseContent = entity.EventbaseContents.FirstOrDefault(o => o.IsAddingMessagesToSendQueue == true && o.IsAddedToSendQueueFinished == false);
        //            if (eventbaseContent == null)
        //                return;
        //            if (eventbaseContent.Content == null || eventbaseContent.Content.Trim() == "")
        //                return;
        //            var aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;
        //            var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
        //            SharedShortCodeServiceLibrary.MessageHandler.AddEventbaseMessagesToQueue(Properties.Settings.Default.ServiceCode, Properties.Settings.Default.ServiceCode, eventbaseContent, aggregatorId);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error(" Exception in Eventbase thread occurred: ", e);
        //    }
        //}
    }
}
