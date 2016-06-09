﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Services.RockPaperScissor
{
    public class RpsSubscribers
    {
        public static void AddSubscriberAdditionalInfo(Message message)
        {
            using (var entities = new PortalEntities())
            {
                var subsriber =
                    entities.Subscribers.FirstOrDefault(
                        o => o.MobileNumber == message.MobileNumber && o.ServiceId == message.ServiceId);
                var subscriberAdditionalInfo =
                    entities.RPS_SubscribersAdditionalInfo.FirstOrDefault(o => o.SubscriberId == subsriber.Id);
                if (subscriberAdditionalInfo == null)
                {
                    var newsubscriber = new RPS_SubscribersAdditionalInfo();
                    newsubscriber.SubscriberId = subsriber.Id;
                    newsubscriber.Point = 0;
                    newsubscriber.UniqueId = CreateUniqueId();
                    entities.RPS_SubscribersAdditionalInfo.Add(newsubscriber);
                    entities.SaveChanges();
                }
            }
        }

        private static string CreateUniqueId()
        {
            Random random = new Random();
            var unqiueId = random.Next(10000000, 99999999).ToString();
            using (var entities = new PortalEntities())
            {
                var subsriber = entities.RPS_SubscribersAdditionalInfo.FirstOrDefault(o => o.UniqueId == unqiueId);
                if (subsriber != null)
                    unqiueId = CreateUniqueId();
            }
            return unqiueId;
        }
    }
}