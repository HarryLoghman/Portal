using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using AcharLibrary.Models;
using System.Linq;
using System.Collections;

namespace DehnadAcharService
{
    class Sender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void SendHandler()
        {
            try
            {
                var today = DateTime.Now.Date;
                List<AutochargeMessagesBuffer> autochargeMessages;
                List<EventbaseMessagesBuffer> eventbaseMessages;
                List<OnDemandMessagesBuffer> onDemandMessages;
                int readSize = Convert.ToInt32(Properties.Settings.Default.ReadSize);
                int takeSize = Convert.ToInt32(Properties.Settings.Default.Take);
                bool retryNotDelieveredMessages = Properties.Settings.Default.RetryNotDeliveredMessages;
                string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;
                string serviceCode = Properties.Settings.Default.ServiceCode;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);

                var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(readSize, takeSize);
                var take = threadsNo["take"];
                var skip = threadsNo["skip"];

                Type entityType = typeof(AcharEntities);

                onDemandMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.OnDemand, readSize)).OfType<OnDemandMessagesBuffer>().ToList();

                //if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
                //{
                //    TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
                //    var now = DateTime.Now.TimeOfDay;
                //    if (now < retryEndTime)
                //    {
                //        using (var entity = new AcharEntities())
                //        {
                //            entity.RetryUndeliveredMessages();
                //        }
                //    }
                //}

                SharedLibrary.MessageHandler.SendSelectedMessages(entityType, onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                if (DateTime.Now.Hour < 20 && DateTime.Now.Hour >= 8)
                {
                    autochargeMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.AutoCharge, readSize)).OfType<AutochargeMessagesBuffer>().ToList();
                    eventbaseMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.EventBase, readSize)).OfType<EventbaseMessagesBuffer>().ToList();
                    eventbaseMessages.RemoveAll(o => o.MobileNumber == "09122137327");
                    SharedLibrary.MessageHandler.SendSelectedMessages(entityType, autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                    SharedLibrary.MessageHandler.SendSelectedMessages(entityType, eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in SendHandler:" + e);
            }
        }
    }
}
