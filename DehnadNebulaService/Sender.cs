using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using NebulaLibrary.Models;
using System.Linq;
using System.Collections;

namespace DehnadNebulaService
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
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Nebula", aggregatorName);

                var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(readSize, takeSize);
                var take = threadsNo["take"];
                var skip = threadsNo["skip"];

                using (var entity = new NebulaEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    autochargeMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entity, SharedLibrary.MessageHandler.MessageType.AutoCharge, 200)).OfType<AutochargeMessagesBuffer>().ToList();
                    eventbaseMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entity, SharedLibrary.MessageHandler.MessageType.EventBase, 200)).OfType<EventbaseMessagesBuffer>().ToList();
                    onDemandMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entity, SharedLibrary.MessageHandler.MessageType.OnDemand, 200)).OfType<OnDemandMessagesBuffer>().ToList();

                    if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
                    {
                        TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
                        var now = DateTime.Now.TimeOfDay;
                        if (now < retryEndTime)
                        {
                            entity.RetryUndeliveredMessages();
                        }
                    }

                    if (DateTime.Now.Hour < 23 && DateTime.Now.Hour > 7)
                    {
                        SharedLibrary.MessageHandler.SendSelectedMessages(entity, autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                        SharedLibrary.MessageHandler.SendSelectedMessages(entity, eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                    }
                    SharedLibrary.MessageHandler.SendSelectedMessages(entity, onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in SendHandler:" + e);
            }
        }
    }
}
