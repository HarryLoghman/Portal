using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using Tabriz2018Library.Models;
using System.Linq;

namespace DehnadTabriz2018Service
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
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Tabriz2018", aggregatorName);
                List<ParidsShortCode> paridsShortCodes = null;
                if (serviceAdditionalInfo["aggregatorId"] == "3"/*PardisPlatform*/)
                {
                    using (var portalEntity = new PortalEntities())
                    {
                        var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                        paridsShortCodes = portalEntity.ParidsShortCodes.Where(o => o.ServiceId == serivceId).ToList();
                    }
                }
                int[] take = new int[(readSize / takeSize)];
                int[] skip = new int[(readSize / takeSize)];
                skip[0] = 0;
                take[0] = takeSize;
                for (int i = 1; i < take.Length; i++)
                {
                    take[i] = takeSize;
                    skip[i] = skip[i - 1] + takeSize;
                }
                using (var entity = new Tabriz2018Entities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    autochargeMessages = Tabriz2018Library.MessageHandler.GetUnprocessedAutochargeMessages(entity, readSize);
                    eventbaseMessages = Tabriz2018Library.MessageHandler.GetUnprocessedEventbaseMessages(entity, readSize);
                    onDemandMessages = Tabriz2018Library.MessageHandler.GetUnprocessedOnDemandMessages(entity, readSize);

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

                SendAutochargeMessages(autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName, paridsShortCodes);
                SendEventbaseMessages(eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName, paridsShortCodes);
                SendOnDemandMessages(onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName, paridsShortCodes);

            }
            catch (Exception e)
            {
                logs.Error("Error in SendHandler:" + e);
            }
        }
        public static void SendAutochargeMessages(List<AutochargeMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName, List<ParidsShortCode> paridsShortCodes)
        {
            if (messages.Count == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                using (var entity = new Tabriz2018Entities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(Tabriz2018Library.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisImi")
                        TaskList.Add(Tabriz2018Library.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisPlatform")
                        TaskList.Add(Tabriz2018Library.MessageHandler.SendMesssagesToPardisPlatform(entity, chunkedMessages, serviceAdditionalInfo, paridsShortCodes));
                }
            }
            Task.WaitAll(TaskList.ToArray());
        }

        public static void SendEventbaseMessages(List<EventbaseMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName, List<ParidsShortCode> paridsShortCodes)
        {
            if (messages.Count == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                using (var entity = new Tabriz2018Entities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(Tabriz2018Library.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisImi")
                        TaskList.Add(Tabriz2018Library.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisPlatform")
                        TaskList.Add(Tabriz2018Library.MessageHandler.SendMesssagesToPardisPlatform(entity, chunkedMessages, serviceAdditionalInfo, paridsShortCodes));
                }
            }
            Task.WaitAll(TaskList.ToArray());
        }

        public static void SendOnDemandMessages(List<OnDemandMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName, List<ParidsShortCode> paridsShortCodes)
        {
            if (messages.Count == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                using (var entity = new Tabriz2018Entities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(Tabriz2018Library.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisImi")
                        TaskList.Add(Tabriz2018Library.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisPlatform")
                        TaskList.Add(Tabriz2018Library.MessageHandler.SendMesssagesToPardisPlatform(entity, chunkedMessages, serviceAdditionalInfo, paridsShortCodes));
                }
            }
            Task.WaitAll(TaskList.ToArray());
        }
    }
}
