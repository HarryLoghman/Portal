using System;
using Portal.Models;
using System.Linq;
using System.Collections.Generic;

namespace DanestanWindowsService
{
    class Autocharge
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void InsertAutochargeMessagesToQueue()
        {
            try
            {
                var insertTime = TimeSpan.Parse(Properties.Resources.InsertAutochargeMessageInQueueTime);
                var serviceCode = Properties.Resources.ServiceCode;
                var today = DateTime.Now.Date;
                var portalEntity = new PortalEntities();
                var serviceId = portalEntity.Services.Where(o => o.ServiceCode == serviceCode).FirstOrDefault().Id;
                var subscribers = portalEntity.Subscribers.Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).Select(o => new { o.MobileNumber, o.Id }).ToList();
                portalEntity.Dispose();
                using (var entity = new DanestanEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var subscriber in subscribers)
                    {
                        var content = Portal.Services.Danestan.ServiceHandler.SelectAutochargeContent(entity, subscriber.Id);
                        if (content == "")
                            continue;
                    }
                }
            }
            catch (Exception e)
            {
                logs.Info(" Exception in Autocharge thread occurred ");
                logs.Error(" Exception in Autocharge thread occurred: ", e);
            }
        }
    }
}