using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;
using MyLeagueLibrary.Models;
using System.Text.RegularExpressions;

namespace MyLeagueLibrary
{
    public class ContentManager
    {
        public static void HandleContent(MessageObject message, vw_servicesServicesInfo service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            var leagueList = GetLeagueList();
            if (Regex.IsMatch(message.Content, @"^[1-9]"))
                message.Content = "+" + message.Content;
            if (message.Content.Contains("+"))
                message = AddLeagueToSubsriber(subscriber, message, leagueList, messagesTemplate);
            else if (message.Content.Contains("-"))
                message = RemoveLeagueFromSubsriber(subscriber, message, leagueList, messagesTemplate);
            else
                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
            if (message.Content != null)
                MessageHandler.InsertMessageToQueue(message);
        }

        private static MessageObject RemoveLeagueFromSubsriber(Subscriber subscriber, MessageObject message, List<LeagueList> leagueList, List<MessagesTemplate> messagesTemplate)
        {
            message.Content = message.Content.Replace("-", "");
            long leagueId = 0;
            foreach (var item in leagueList)
            {
                if (message.Content == item.Number.ToString() || message.Content == item.Name)
                {
                    leagueId = item.Id;
                    break;
                }
            }
            if (leagueId != 0)
            {
                using (var entity = new MyLeagueEntities())
                {
                    var subscriberLeague = entity.SubscribersLeagues.FirstOrDefault(o => o.SubscriberId == subscriber.Id && o.LeagueId == leagueId);
                    if (subscriberLeague == null)
                    {
                        message.Content = "شما عضو لیگ " + leagueList.Where(o => o.Id == leagueId).FirstOrDefault().Name + " نیستید";
                    }
                    else
                    {
                        entity.SubscribersLeagues.Remove(subscriberLeague);
                        entity.SaveChanges();
                        message.Content = "دریافت خبرهای لیگ " + leagueList.Where(o => o.Id == leagueId).FirstOrDefault().Name + " برای شما غیر فعال گردید";
                    }
                }
            }
            else
            {
                message = MessageHandler.InvalidContentWhenSubscribed(message, messagesTemplate);
            }
            return message;
        }

        private static MessageObject AddLeagueToSubsriber(Subscriber subscriber, MessageObject message, List<LeagueList> leagueList, List<MessagesTemplate> messagesTemplate)
        {
            message.Content = message.Content.Replace("+", "");
            long leagueId = 0;
            foreach (var item in leagueList)
            {
                if (message.Content == item.Number.ToString() || message.Content == item.Name)
                {
                    leagueId = item.Id;
                    break;
                }
            }
            if (leagueId != 0)
            {
                using (var entity = new MyLeagueEntities())
                {
                    var isSubscriberLeagueExits = entity.SubscribersLeagues.FirstOrDefault(o => o.SubscriberId == subscriber.Id && o.LeagueId == leagueId);
                    if (isSubscriberLeagueExits == null)
                    {
                        var league = new SubscribersLeague();
                        league.SubscriberId = subscriber.Id;
                        league.LeagueId = leagueId;
                        entity.SubscribersLeagues.Add(league);
                        entity.SaveChanges();
                    }
                    SendWelcomeToLeagueMessage(message, leagueId);
                }
                message = PrepareLatestLeagueContent(message, subscriber.Id, leagueId);
            }
            else
            {
                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
            }
            return message;
        }

        private static void SendWelcomeToLeagueMessage(MessageObject message, long leagueId)
        {
            using (var entity = new MyLeagueEntities())
            {
                var welcomeMessage = message;
                var leagueName = entity.LeagueLists.FirstOrDefault(o => o.Id == leagueId).Name;
                welcomeMessage.Content = "به لیگ " + leagueName + " خوش آمدید.";
                welcomeMessage = MessageHandler.SetImiChargeInfo(welcomeMessage, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                MessageHandler.InsertMessageToQueue(welcomeMessage);
            }
        }

        private static MessageObject PrepareLatestLeagueContent(MessageObject message, long id, long leagueId)
        {
            using (var entity = new MyLeagueEntities())
            {
                var content = entity.EventbaseContents.Where(o => o.LeagueId == leagueId).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                if (content != null)
                {
                    message = MessageHandler.SetImiChargeInfo(message, content.Price, 0, null);
                    message.Content = content.Content;
                    message.Point = content.Point;
                    message.ContentId = content.Id;
                }
                else
                    message.Content = null;
                return message;
            }
        }

        public static List<LeagueList> GetLeagueList()
        {
            using (var entity = new MyLeagueEntities())
            {
                var leagueList = entity.LeagueLists.ToList();
                return leagueList;
            }
        }

        public static List<SubscribersLeague> GetSubscriberLeagues(long subscriberId)
        {
            using (var entity = new MyLeagueEntities())
            {
                var subscriberLeageus = entity.SubscribersLeagues.Where(o => o.SubscriberId == subscriberId).ToList();
                return subscriberLeageus;
            }
        }
    }
}
