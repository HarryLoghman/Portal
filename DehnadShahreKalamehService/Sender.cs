using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using ShahreKalamehLibrary.Models;
using System.Linq;

namespace DehnadShahreKalamehService
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
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("ShahreKalameh", aggregatorName);
                int[] take = new int[(readSize / takeSize)];
                int[] skip = new int[(readSize / takeSize)];
                skip[0] = 0;
                take[0] = takeSize;
                for (int i = 1; i < take.Length; i++)
                {
                    take[i] = takeSize;
                    skip[i] = skip[i - 1] + takeSize;
                }
                using (var entity = new ShahreKalamehEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    autochargeMessages = ShahreKalamehLibrary.MessageHandler.GetUnprocessedAutochargeMessages(entity, readSize);
                    eventbaseMessages = ShahreKalamehLibrary.MessageHandler.GetUnprocessedEventbaseMessages(entity, readSize);
                    onDemandMessages = ShahreKalamehLibrary.MessageHandler.GetUnprocessedOnDemandMessages(entity, readSize);

                    if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
                    {
                        TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
                        var now = DateTime.Now.TimeOfDay;
                        if (now < retryEndTime)
                        {
                            entity.RetryUndeliveredMessages();
                        }
                    }
                }

                SendAutochargeMessages(autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                SendEventbaseMessages(eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                SendOnDemandMessages(onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);

            }
            catch (Exception e)
            {
                logs.Error("Error in SendHandler:" + e);
            }
        }
        public static void SendAutochargeMessages(List<AutochargeMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        {
            if (messages.Count == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                using (var entity = new ShahreKalamehEntities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisImi")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "Telepromo")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToTelepromo(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "Hub")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToHub(entity, chunkedMessages, serviceAdditionalInfo));
                }
            }
            Task.WaitAll(TaskList.ToArray());
        }

        public static void SendEventbaseMessages(List<EventbaseMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        {
            if (messages.Count == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                using (var entity = new ShahreKalamehEntities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisImi")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "Telepromo")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToTelepromo(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "Hub")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToHub(entity, chunkedMessages, serviceAdditionalInfo));
                }
            }
            Task.WaitAll(TaskList.ToArray());
        }

        public static void SendOnDemandMessages(List<OnDemandMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        {
            if (messages.Count == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                using (var entity = new ShahreKalamehEntities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisImi")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "Telepromo")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToTelepromo(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "Hub")
                        TaskList.Add(ShahreKalamehLibrary.MessageHandler.SendMesssagesToHub(entity, chunkedMessages, serviceAdditionalInfo));
                }
            }
            Task.WaitAll(TaskList.ToArray());
        }
    }
}
