using System;
using System.Linq;
using Portal.Models;
using System.Data.Entity;

namespace DehnadMyLeagueService
{
    class Statistic
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Process()
        {
            try
            {
                EventbaseStatistic();
            }
            catch (Exception e)
            {
                logs.Error("Error in Statistics Thread: " + e);
            }
        }

        private void EventbaseStatistic()
        {
            try
            {
                using (var entity = new MyLeagueEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;

                    var eventbaseMonitoringItems = entity.MessagesMonitorings.Where(o => o.Status == (int)Portal.Shared.MessageHandler.ProcessStatus.TryingToSend).ToList();
                    foreach (var item in eventbaseMonitoringItems)
                    {
                        var processStatus = entity.EventbaseMessagesBuffers.Where(o => o.ContentId == item.ContentId).Select(o => o.ProcessStatus);
                        if (processStatus.Where(o => o.Value == (int)Portal.Shared.MessageHandler.ProcessStatus.InQueue || o.Value == (int)Portal.Shared.MessageHandler.ProcessStatus.TryingToSend).Count() == 0)
                            item.Status = (int)Portal.Shared.MessageHandler.ProcessStatus.Finished;
                        item.TotalSuccessfulySended = processStatus.Where(o => o.Value == (int)Portal.Shared.MessageHandler.ProcessStatus.Success).Count();
                        item.TotalFailed = processStatus.Where(o => o.Value == (int)Portal.Shared.MessageHandler.ProcessStatus.Failed).Count();
                        entity.Entry(item).State = EntityState.Modified;
                        entity.Entry(item).Property(x => x.PersianDateCreated).IsModified = false;
                        entity.Entry(item).Property(x => x.DateCreated).IsModified = false;
                        entity.Entry(item).Property(x => x.ContentId).IsModified = false;
                        entity.Entry(item).Property(x => x.TotalMessages).IsModified = false;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {

                logs.Error("Error in EventbaseStatistics: " + e);
            }
        }
    }
}