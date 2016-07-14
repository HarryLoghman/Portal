using System;
using System.Linq;
using Portal.Models;

namespace DehnadDanestanService
{
    class Eventbase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void InsertEventbaseMessagesToQueue()
        {
            try
            {
                using (var entity = new DanestanEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var eventbaseContent = entity.EventbaseContents.FirstOrDefault(o => o.IsAddingMessagesToSendQueue == true && o.IsAddedToSendQueueFinished == false);
                    if (eventbaseContent == null)
                        return;
                    if (eventbaseContent.Content == null || eventbaseContent.Content.Trim() == "")
                        return;
                    var aggregatorName = Properties.Resources.AggregatorName;
                    var aggregatorId = Portal.Shared.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                    Portal.Services.Danestan.MessageHandler.AddEventbaseMessagesToQueue(eventbaseContent, aggregatorId);
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in Eventbase thread occurred: ", e);
            }
        }
    }
}
