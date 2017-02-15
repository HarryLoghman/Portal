using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DehnadSinglechargeDeliveryProcessorService
{
    public class DeliveryProcesser
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Process()
        {
            try
            {
                var deliveryMessages = new List<SinglechargeDelivery>();
                var NumberOfConcurrentDeliveriesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentDeliveriesToProcess);
                using (var db = new PortalEntities())
                {
                    deliveryMessages = db.SinglechargeDeliveries.Where(o => o.IsProcessed == false).ToList();
                }
                if (deliveryMessages.Count == 0)
                    return;
                for (int i = 0; i < deliveryMessages.Count; i += NumberOfConcurrentDeliveriesToProcess)
                {
                    var receivedChunk = deliveryMessages.Skip(i).Take(NumberOfConcurrentDeliveriesToProcess).ToList();
                    Parallel.ForEach(receivedChunk, delivredMessage =>
                    {
                        HandleDeliveredMessage(delivredMessage);
                    });
                }
            }
            catch (Exception e)
            {
                logs.Error("Exeption in SinglechargeDeliveryProcessor: " + e);
            }
        }

        private void HandleDeliveredMessage(SinglechargeDelivery deliveredSinglecharge)
        {
            try
            {
                bool isSinglechargeFound = false;

                if (isSinglechargeFound == false)
                    isSinglechargeFound = ShahreKalamehFindAndUpdateSinglechargeDelivery(deliveredSinglecharge);

                UpdateDeliveryObject(deliveredSinglecharge, isSinglechargeFound);
            }
            catch (Exception e)
            {
                logs.Error("Exeption in HandleDeliveredMessage: " + e);
            }
        }

        private static void UpdateDeliveryObject(SinglechargeDelivery deliveredSinglecharge, bool isSinglechargeFound)
        {
            using (var portalEntity = new SharedLibrary.Models.PortalEntities())
            {
                portalEntity.SinglechargeDeliveries.Attach(deliveredSinglecharge);
                if (isSinglechargeFound == true)
                {
                    portalEntity.SinglechargeDeliveries.Remove(deliveredSinglecharge);
                    portalEntity.Entry(deliveredSinglecharge).State = EntityState.Deleted;
                }
                else
                {
                    deliveredSinglecharge.IsProcessed = true;
                    portalEntity.Entry(deliveredSinglecharge).State = EntityState.Modified;
                }
                portalEntity.SaveChanges();
            }
        }

        private bool ShahreKalamehFindAndUpdateSinglechargeDelivery(SinglechargeDelivery deliveredSinglecharge)
        {
            try
            {
                using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
                {
                    var singlecharge = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == deliveredSinglecharge.ReferenceId);
                    if (singlecharge != null)
                    {
                        singlecharge.Description = deliveredSinglecharge.Status + "-" + deliveredSinglecharge.Description;
                        if (deliveredSinglecharge.Status == "41" && deliveredSinglecharge.Description.Contains("ACCEPTED"))
                        {
                            singlecharge.IsSucceeded = true;
                            if (singlecharge.InstallmentId != null)
                            {
                                var installment = entity.SinglechargeInstallments.FirstOrDefault(o => o.Id == singlecharge.InstallmentId);
                                if (installment != null)
                                {
                                    installment.PricePayed += singlecharge.Price;
                                    installment.PriceTodayCharged += singlecharge.Price;
                                    if (installment.PricePayed >= installment.TotalPrice)
                                        installment.IsFullyPaid = true;
                                    entity.Entry(installment).State = EntityState.Modified;
                                }
                            }
                        }
                        entity.Entry(singlecharge).State = EntityState.Modified;
                        entity.SaveChanges();
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exeption in ShahreKalamehFindAndUpdateSinglechargeDelivery: " + e);
            }
            return false;
        }
    }
}
