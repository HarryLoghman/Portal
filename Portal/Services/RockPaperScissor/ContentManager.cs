using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;
using Portal.Shared;

namespace Portal.Services.RockPaperScissor
{
    public class ContentManager
    {
        public static void HandleContent(Message message, Service serviceInfo)
        {
            if (message.Content == "1" || message.Content == "2" || message.Content == "3")
                PlayGame(message,serviceInfo);
            else if (message.Content == "4")
                ContinueGame(message, serviceInfo);
            else
                MessageHandler.InvalidContentWhenSubscribed(message, serviceInfo);

        }

        private static void ContinueGame(Message message, Service serviceInfo)
        {
            throw new NotImplementedException();
        }

        private static void PlayGame(Message message, Service serviceInfo)
        {
            var subscriberChoice = Convert.ToInt32(message.Content);
            Random rnd = new Random();
            var systemChoice = rnd.Next(1, 3);
            var subscriberState = GameLogic((RpsItem)subscriberChoice, (RpsItem)systemChoice);
        }

        private static GameState GameLogic(RpsItem subscriberChoice, RpsItem systemChoice)
        {
            if(subscriberChoice == RpsItem.Rock && systemChoice == RpsItem.Rock)
                return GameState.Matched;
            else if (subscriberChoice == RpsItem.Rock && systemChoice == RpsItem.Paper)
                return GameState.SubscriberLosed;
            else if (subscriberChoice == RpsItem.Rock && systemChoice == RpsItem.Scissor)
                return GameState.SubscriberWinned;

            else if (subscriberChoice == RpsItem.Paper && systemChoice == RpsItem.Rock)
                return GameState.SubscriberWinned;
            else if (subscriberChoice == RpsItem.Paper && systemChoice == RpsItem.Paper)
                return GameState.Matched;
            else if (subscriberChoice == RpsItem.Paper && systemChoice == RpsItem.Scissor)
                return GameState.SubscriberLosed;

            else if (subscriberChoice == RpsItem.Scissor && systemChoice == RpsItem.Rock)
                return GameState.SubscriberLosed;
            else if (subscriberChoice == RpsItem.Scissor && systemChoice == RpsItem.Paper)
                return GameState.SubscriberWinned;
            else if (subscriberChoice == RpsItem.Scissor && systemChoice == RpsItem.Scissor)
                return GameState.Matched;

            return GameState.Matched;
        }

        public enum GameState
        {
            Matched = 0,
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