using Portal.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DehnadReceiveProcessorService
{
    class MessageProcesser
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Process()
        {
            try
            {
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Resources.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;
                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    Parallel.ForEach(receivedChunk, receivedMessage =>
                    {
                        Portal.Shared.MessageHandler.HandleReceivedMessage(receivedMessage);
                    });
                }
            }
            catch (Exception e)
            {
                logs.Error("Exeption in RecieveProcessor: " + e);
            }
        }
    }
}
