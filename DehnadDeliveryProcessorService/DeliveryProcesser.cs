using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DehnadDeliveryProcessorService
{
    public class DeliveryProcesser
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Process()
        {
            try
            {
                var deliveryMessages = new List<Delivery>();
                var NumberOfConcurrentDeliveriesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentDeliveriesToProcess);
                using (var db = new PortalEntities())
                {
                    deliveryMessages = db.Deliveries.Where(o => o.IsProcessed == false).ToList();
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
                logs.Error("Exeption in DeliveryProcessor: " + e);
            }
        }

        private void HandleDeliveredMessage(Delivery delivredMessage)
        {
            try
            {

            }
            catch (Exception e)
            {
                logs.Error("Exeption in HandleDeliveredMessage: " + e);
            }
        }
    }
}
