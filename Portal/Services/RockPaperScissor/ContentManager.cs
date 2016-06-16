using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Portal.Models;
using Portal.Shared;

namespace Portal.Services.RockPaperScissor
{
    public class ContentManager
    {
        public static void HandleContent(Message message, Service serviceInfo, Subscriber subscriber)
        {
            message.Content = CheckIfUserIsPlaynigRPS(message.Content);
            if (message.Content == "1" || message.Content == "2" || message.Content == "3")
            {
                var subscriberGameState = PlayGame(message, serviceInfo);
                var userGameState = ProcessSubscriberGameState(message, serviceInfo, subscriber, subscriberGameState);
                InformUser(subscriber, serviceInfo, subscriberGameState);
            }
            else if (message.Content == "4")
                ContinueGame(message, serviceInfo);
            else
                MessageHandler.InvalidContentWhenSubscribed(message, serviceInfo);

        }

        private static void InformUser(Subscriber subscriber, Service serviceInfo, GameState subscriberGameState)
        {
            throw new NotImplementedException();
        }

        private static GameState ProcessSubscriberGameState(Message message, Service serviceInfo, Subscriber subscriber, GameState subscriberGameState)
        {
            using (var entity = new PortalEntities())
            {
                if (subscriberGameState == GameState.SubscriberWinned)
                {
                    subscriber.RPS_SubscribersAdditionalInfo.FirstOrDefault().TimesWinned += 1;
                    subscriber.RPS_SubscribersAdditionalInfo.FirstOrDefault().ContinuousGameWinned += 1;
                }
                else
                {
                    subscriber.RPS_SubscribersAdditionalInfo.FirstOrDefault().TimesLosed += 1;
                    subscriber.RPS_SubscribersAdditionalInfo.FirstOrDefault().ContinuousGameWinned = 0;
                }
                entity.Entry(subscriber).State = EntityState.Modified;
                entity.SaveChanges();
            }
            return subscriberGameState;
        }

        private static string CheckIfUserIsPlaynigRPS(string content)
        {
            if (content == "یک" || content.Contains("سنگ") || content.Contains("سنک"))
                content = "1";
            else if (content == "دو" || content.Contains("کاغذ") || content.Contains("کاغذ") || content.Contains("کاعذ") ||
                     content.Contains("کاغد") || content.Contains("گاغذ"))
                content = "2";
            else if (content == "سه" || content == "قیچی")
                content = "3";
            return content;
        }

        private static void ContinueGame(Message message, Service serviceInfo)
        {
            throw new NotImplementedException();
        }

        private static GameState PlayGame(Message message, Service serviceInfo)
        {
            var subscriberChoice = Convert.ToInt32(message.Content);
            Random rnd = new Random();
            var systemChoice = rnd.Next(1, 3);
            var subscriberState = GameLogic((RpsItem)subscriberChoice, (RpsItem)systemChoice);
            return subscriberState;
        }

        private static GameState GameLogic(RpsItem subscriberChoice, RpsItem systemChoice)
        {
            if (subscriberChoice == RpsItem.Rock && systemChoice == RpsItem.Rock)
                return GameState.SubscriberLosed;
            else if (subscriberChoice == RpsItem.Rock && systemChoice == RpsItem.Paper)
                return GameState.SubscriberLosed;
            else if (subscriberChoice == RpsItem.Rock && systemChoice == RpsItem.Scissor)
                return GameState.SubscriberWinned;

            else if (subscriberChoice == RpsItem.Paper && systemChoice == RpsItem.Rock)
                return GameState.SubscriberWinned;
            else if (subscriberChoice == RpsItem.Paper && systemChoice == RpsItem.Paper)
                return GameState.SubscriberLosed;
            else if (subscriberChoice == RpsItem.Paper && systemChoice == RpsItem.Scissor)
                return GameState.SubscriberLosed;

            else if (subscriberChoice == RpsItem.Scissor && systemChoice == RpsItem.Rock)
                return GameState.SubscriberLosed;
            else if (subscriberChoice == RpsItem.Scissor && systemChoice == RpsItem.Paper)
                return GameState.SubscriberWinned;
            else if (subscriberChoice == RpsItem.Scissor && systemChoice == RpsItem.Scissor)
                return GameState.SubscriberLosed;
            else
                return GameState.SubscriberLosed;
        }

        public enum GameState
        {
            SubscriberWinned = 1,
            SubscriberLosed = 2
        }


        public enum RpsItem
        {
            Rock = 1,
            Paper = 2,
            Scissor = 3
        }
    }
}