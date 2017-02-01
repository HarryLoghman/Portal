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
                bool isSusccessfulyDelivered = false;
                string description = null;
                bool isMessageFound = false;

                if (delivredMessage.AggregatorId == 2)// pardis
                {
                    if (delivredMessage.Status == "DeliveredToTerminal")
                        isSusccessfulyDelivered = true;
                    else
                        description = delivredMessage.Status + "-" + delivredMessage.Description;
                }
                else
                    UpdateDeliveryObject(delivredMessage, isMessageFound);

                if (isMessageFound == false)
                    isMessageFound = BimeKarbalaFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = BoatingFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = DanestanehFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = MashinBazhaFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = MobiligaFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = MyLeagueFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = SepidRoodFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = SoltanFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = Tabriz2018FindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = TirandaziFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = DonyayeAsatirFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);
                if (isMessageFound == false)
                    isMessageFound = ShahreKalamehFindAndUpdateMessageDelivery(delivredMessage, isSusccessfulyDelivered, description);

                UpdateDeliveryObject(delivredMessage, isMessageFound);
            }
            catch (Exception e)
            {
                logs.Error("Exeption in HandleDeliveredMessage: " + e);
            }
        }

        private static void UpdateDeliveryObject(Delivery delivredMessage, bool isMessageFound)
        {
            using (var portalEntity = new SharedLibrary.Models.PortalEntities())
            {
                portalEntity.Deliveries.Attach(delivredMessage);
                if (isMessageFound == true)
                {
                    portalEntity.Deliveries.Remove(delivredMessage);
                    portalEntity.Entry(delivredMessage).State = EntityState.Deleted;
                }
                else
                {
                    delivredMessage.IsProcessed = true;
                    portalEntity.Entry(delivredMessage).State = EntityState.Modified;
                }
                portalEntity.SaveChanges();
            }
        }

        private bool ShahreKalamehFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool DonyayeAsatirFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool BoatingFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new BoatingLibrary.Models.BoatingEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if(isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool TirandaziFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new TirandaziLibrary.Models.TirandaziEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool Tabriz2018FindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new Tabriz2018Library.Models.Tabriz2018Entities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool SoltanFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new SoltanLibrary.Models.SoltanEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool SepidRoodFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new SepidRoodLibrary.Models.SepidRoodEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool MobiligaFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new MobiligaLibrary.Models.MobiligaEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool MashinBazhaFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new MashinBazhaLibrary.Models.MashinBazhaEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool DanestanehFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new DanestanehLibrary.Models.DanestanehEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool MyLeagueFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new MyLeagueLibrary.Models.MyLeagueEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool BimeKarbalaFindAndUpdateMessageDelivery(Delivery delivredMessage, bool isSusccessfulyDelivered, string description)
        {
            using (var entity = new BimeKarbalaLibrary.Models.BimeKarbalaEntities())
            {
                var autochargeMessage = entity.AutochargeMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (autochargeMessage != null)
                {
                    autochargeMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        autochargeMessage.DeliveryDescription = description;
                    entity.Entry(autochargeMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var eventbaseMessage = entity.EventbaseMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (eventbaseMessage != null)
                {
                    eventbaseMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        eventbaseMessage.DeliveryDescription = description;
                    entity.Entry(eventbaseMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
                var ondemandMessage = entity.OnDemandMessagesBuffers.FirstOrDefault(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success && o.DeliveryStatus == null && o.ReferenceId == delivredMessage.ReferenceId);
                if (ondemandMessage != null)
                {
                    ondemandMessage.DeliveryStatus = isSusccessfulyDelivered;
                    if (isSusccessfulyDelivered == false)
                        ondemandMessage.DeliveryDescription = description;
                    entity.Entry(ondemandMessage).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            return false;
        }
    }
}
