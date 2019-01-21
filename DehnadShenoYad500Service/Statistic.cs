using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;
using System.Data.Entity;

namespace DehnadShenoYad500Service
{
    class Statistic
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Process()
        {
            try
            {
                //AutochargeStatistic();
                EventbaseStatistic();
            }
            catch (Exception e)
            {
                logs.Error("Error in Statistics Thread: " + e);
            }
        }

        private void AutochargeStatistic()
        {
            try
            {
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;

                    var autochargeMonitoringItems = entity.MessagesMonitorings.Where(o => o.Status == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend || o.Status == (int)SharedLibrary.MessageHandler.ProcessStatus.InQueue).ToList();
                    foreach (var item in autochargeMonitoringItems)
                    {
                        var processStatus = entity.AutochargeMessagesBuffers.Where(o => o.Tag == item.Tag).Select(o => o.ProcessStatus);
                        if (processStatus.Where(o => o.Equals((int)SharedLibrary.MessageHandler.ProcessStatus.InQueue) || o.Equals((int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)).Count() == 0)
                            item.Status = (int)SharedLibrary.MessageHandler.ProcessStatus.Finished;
                        item.TotalSuccessfulySended = processStatus.Where(o => o.Equals((int)SharedLibrary.MessageHandler.ProcessStatus.Success)).Count();
                        item.TotalFailed = processStatus.Where(o => o.Equals((int)SharedLibrary.MessageHandler.ProcessStatus.Failed)).Count();
                        entity.Entry(item).State = EntityState.Modified;
                        entity.Entry(item).Property(x => x.PersianDateCreated).IsModified = false;
                        entity.Entry(item).Property(x => x.DateCreated).IsModified = false;
                        entity.Entry(item).Property(x => x.ContentId).IsModified = false;
                        entity.Entry(item).Property(x => x.Tag).IsModified = false;
                        entity.Entry(item).Property(x => x.MessageType).IsModified = false;
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

        private void EventbaseStatistic()
        {
            try
            {
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;

                    var eventbaseMonitoringItems = entity.MessagesMonitorings.Where(o => o.Status == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).ToList();
                    foreach (var item in eventbaseMonitoringItems)
                    {
                        var processStatus = entity.EventbaseMessagesBuffers.Where(o => o.ContentId == item.ContentId).Select(o => o.ProcessStatus);
                        if (processStatus.Where(o => o.Equals((int)SharedLibrary.MessageHandler.ProcessStatus.InQueue) || o.Equals((int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)).Count() == 0)
                            item.Status = (int)SharedLibrary.MessageHandler.ProcessStatus.Finished;
                        item.TotalSuccessfulySended = processStatus.Where(o => o.Equals((int)SharedLibrary.MessageHandler.ProcessStatus.Success)).Count();
                        item.TotalFailed = processStatus.Where(o => o.Equals((int)SharedLibrary.MessageHandler.ProcessStatus.Failed)).Count();
                        entity.Entry(item).State = EntityState.Modified;
                        entity.Entry(item).Property(x => x.PersianDateCreated).IsModified = false;
                        entity.Entry(item).Property(x => x.DateCreated).IsModified = false;
                        entity.Entry(item).Property(x => x.ContentId).IsModified = false;
                        entity.Entry(item).Property(x => x.Tag).IsModified = false;
                        entity.Entry(item).Property(x => x.MessageType).IsModified = false;
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