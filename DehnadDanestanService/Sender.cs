using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Portal.Models;
using System.Diagnostics;

namespace DehnadDanestanService
{
    class Sender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void SendHandler()
        {
            Debugger.Launch();
            var today = DateTime.Now.Date;
            dynamic autochargeMessages; 
            dynamic eventbaseMessages; 
            dynamic onDemandMessages;
            int readSize = Convert.ToInt32(Properties.Resources.ReadSize);
            int takeSize = Convert.ToInt32(Properties.Resources.Take);
            string aggregatorName = Properties.Resources.AggregatorName;
            var serviceAdditionalInfo = Portal.Shared.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Danestan", aggregatorName);
            int[] take = new int[(readSize / takeSize)];
            int[] skip = new int[(readSize / takeSize)];
            skip[0] = 0;
            take[0] = takeSize;
            for (int i = 1; i < take.Length; i++)
            {
                take[i] = takeSize;
                skip[i] = skip[i - 1] + takeSize;
            }
            using (var entity = new DanestanEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                autochargeMessages = Portal.Services.Danestan.MessageHandler.GetUnprocessedAutochargeMessages(entity, readSize);
                eventbaseMessages = Portal.Services.Danestan.MessageHandler.GetUnprocessedEventbaseMessages(entity, readSize);
                onDemandMessages = Portal.Services.Danestan.MessageHandler.GetUnprocessedOnDemandMessages(entity, readSize);
            }
            Send(onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);
            Send(eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
            Send(autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
        }
        public static void Send(dynamic messages, int[] skip, int[] take, Dictionary<string,string> serviceAdditionalInfo, string aggregatorName)
        {
            if (messages.Count == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                using (var entity = new DanestanEntities())
                {
                    var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                    if (aggregatorName == "Hamrahvas")
                        TaskList.Add(Portal.Services.Danestan.MessageHandler.SendMesssagesToHamrahvas(entity, chunkedMessages, serviceAdditionalInfo));
                }
            }
            Task.WaitAll(TaskList.ToArray());
        }
    }
}
