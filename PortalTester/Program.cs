using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Portal;
using Portal.Models;

namespace PortalTester
{
    class Program
    {
        static void Main(string[] args)
        {
            //RunTest();
        }

        public static void RunTest()
        {
            using (var entity = new MyLeagueEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var eventbaseContent = entity.EventbaseContents.FirstOrDefault(o => o.IsAddingMessagesToSendQueue == true && o.IsAddedToSendQueueFinished == false);
                if (eventbaseContent == null)
                    return;
                if (eventbaseContent.Content == null || eventbaseContent.Content.Trim() == "")
                    return;
                var aggregatorName = "PardisImi";
                var aggregatorId = Portal.Shared.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                Portal.Services.MyLeague.MessageHandler.AddEventbaseMessagesToQueue(eventbaseContent, aggregatorId);
            }
        }
    }
}
