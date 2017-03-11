using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BimeIranLibrary.Models;
using BimeIranLibrary;
using System.Data.Entity;

namespace DehnadBimeIranService
{
    public class Reminder
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Process()
        {
            try
            {
                SendWarningToUsers();
            }
            catch (Exception e)
            {
                logs.Error("Exception in Reminder Process : ", e);
            }
        }

        private void SendWarningToUsers()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                var serviceId = SharedLibrary.ServiceHandler.GetServiceId("BimeIran");
                var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId.GetValueOrDefault());
                if (serviceInfo == null)
                {
                    logs.Info("serviceInfo is null in SendWarningToUsers!");
                    return;
                }
                var shortCode = serviceInfo.ShortCode;
                using (var entity = new BimeIranEntities())
                {
                    var warningList = entity.SubscribersAdditionalInfoes.Where(o => (o.SubscriberLevel == 2 || o.SubscriberLevel == 3 || o.SubscriberLevel == 4) && o.DateWarningSent.Value.Date != today.Date).ToList();
                    foreach (var item in warningList)
                    {
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }

                        if ((item.SubscriberLevel == 2 || item.SubscriberLevel == 3) && item.NumberOfWarningsSent > 3)
                            continue;
                        if(item.SubscriberLevel == 4 && item.NumberOfWarningsSent > 7)
                        {
                            //Cancel user insurance
                            continue;
                        }
                        item.NumberOfWarningsSent++;
                        item.DateWarningSent = DateTime.Now;
                        entity.Entry(item).State = EntityState.Modified;
                        batchSaveCounter += 1;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in Reminder SendWarningToUsers : ", e);
            }
        }
    }
}
