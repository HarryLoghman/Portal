using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BimeIranLibrary.Models;
using System.Data.Entity;

namespace DehnadBimeIranService
{
    class BimeProcessor
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void ReinsertBimeDataToQueue()
        {
            try
            {
                using(var entity = new BimeIranEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var now = DateTime.Now;
                    var insuranceWaitingList = entity.InsuranceInfoes.Where(o => o.IsSendedToInsuranceCompany == true && DbFunctions.AddMinutes(o.DateInsuranceRequested, 15) < now && o.IsSendedDeliveryReceivedFromInsuranceCompnay != true).ToList();
                    foreach (var item in insuranceWaitingList)
                    {
                        item.IsSendedToInsuranceCompany = false;
                        entity.Entry(item).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                    var damageWaitingList = entity.DamageReports.Where(o => o.IsSendedToInsuranceCompany == true && o.IsDeliveryReceivedFromInsuranceCompany == false && DbFunctions.AddMinutes(o.DateDamageReported, 15) < now).ToList();
                    foreach (var item in damageWaitingList)
                    {
                        item.IsSendedToInsuranceCompany = false;
                        entity.Entry(item).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ReinsertBimeDataToQueue:", e);
            }
        }
    }
}
