using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;
using System.Linq;
using System.Collections;
using System.Data.Entity;
using System.Net.Http;

namespace SharedShortCodeServiceLibrary
{
    public class Sender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public virtual void SendHandler(string serviceCode)
        {
            this.SendHandler(serviceCode, null, null);
        }

        public virtual void SendHandler(string serviceCode, TimeSpan? timeStart, TimeSpan? timeEnd)
        {
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var service = portal.vw_servicesServicesInfo.Where(o => o.ServiceCode == serviceCode).FirstOrDefault();
                this.SendHandler(service, timeStart, timeEnd);
            }
        }

        public virtual void SendHandler(SharedLibrary.Models.vw_servicesServicesInfo service)
        {
            this.SendHandler(service,  null, null);
        }
        public virtual void SendHandler(SharedLibrary.Models.vw_servicesServicesInfo service, TimeSpan? timeStart, TimeSpan? timeEnd)
        {
            try
            {
                string connectionStringInAppConfig = service.ServiceCode;
                var today = DateTime.Now.Date;
                List<AutochargeMessagesBuffer> autochargeMessages;
                List<EventbaseMessagesBuffer> eventbaseMessages;
                List<OnDemandMessagesBuffer> onDemandMessages;
                string aggregatorName = service.aggregatorName;
                string serviceCode = service.ServiceCode;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);


                using (var serviceEntity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(connectionStringInAppConfig))
                {
                    onDemandMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(serviceEntity, SharedLibrary.MessageHandler.MessageType.OnDemand, 0)).OfType<OnDemandMessagesBuffer>().ToList();
                    SharedLibrary.MessageHandler.SendSelectedMessages(service, onDemandMessages);
                    if ((!timeStart.HasValue || timeStart.Value <= DateTime.Now.TimeOfDay) && (!timeEnd.HasValue || DateTime.Now.TimeOfDay <= timeEnd.Value))
                    {
                        autochargeMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(serviceEntity, SharedLibrary.MessageHandler.MessageType.AutoCharge, 0)).OfType<AutochargeMessagesBuffer>().ToList();
                        eventbaseMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(serviceEntity, SharedLibrary.MessageHandler.MessageType.EventBase, 0)).OfType<EventbaseMessagesBuffer>().ToList();
                        eventbaseMessages.RemoveAll(o => o.MobileNumber == "09122137327");
                        SharedLibrary.MessageHandler.SendSelectedMessages(service, autochargeMessages);
                        SharedLibrary.MessageHandler.SendSelectedMessages(service, eventbaseMessages);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in SendHandler:" + e);
            }
        }
    }
}
