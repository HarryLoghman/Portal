using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using SepidRoodLibrary.Models;
using System.Linq;

namespace DehnadSepidRoodService
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
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("SepidRood", aggregatorName);
                int[] take = new int[(readSize / takeSize)];
                int[] skip = new int[(readSize / takeSize)];
                skip[0] = 0;
                take[0] = takeSize;
                for (int i = 1; i < take.Length; i++)
                {
                    take[i] = takeSize;
                    skip[i] = skip[i - 1] + takeSize;
                }
                using (var entity = new SepidRoodEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    autochargeMessages = SepidRoodLibrary.MessageHandler.GetUnprocessedAutochargeMessages(entity, readSize);
                    eventbaseMessages = SepidRoodLibrary.MessageHandler.GetUnprocessedEventbaseMessages(entity, readSize);
                    onDemandMessages = SepidRoodLibrary.MessageHandler.GetUnprocessedOnDemandMessages(entity, readSize);
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
                using (var entity = new SepidRoodEntities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(SepidRoodLibrary.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisImi")
                        TaskList.Add(SepidRoodLibrary.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
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
                using (var entity = new SepidRoodEntities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(SepidRoodLibrary.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisImi")
                        TaskList.Add(SepidRoodLibrary.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
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
                using (var entity = new SepidRoodEntities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(SepidRoodLibrary.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                    else if (aggregatorName == "PardisImi")
                        TaskList.Add(SepidRoodLibrary.MessageHandler.SendMesssagesToPardisImi(entity, chunkedMessages, serviceAdditionalInfo));
                }
            }
            Task.WaitAll(TaskList.ToArray());
        }
    }
}
