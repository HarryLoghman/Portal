using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyLeagueLibrary.Models;
using SharedLibrary.Models;

namespace MyLeagueLibrary
{
    public class RechargeHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void RechargeProcess(MessageObject message, Subscriber subscriber, List<MessagesTemplate> messagesTemplate, List<ServiceRechargeKeyword> serviceRehcargeKeywords)
        {
            var isUserAlreadyUsedTheRehargeCode = CheckIsUserAlreadyUsedTheRechargeCode(subscriber.Id);
            if (isUserAlreadyUsedTheRehargeCode == true)
            {
                message = MessageHandler.SubscriberAlreadyUsedTheRechargeCode(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return;
            }

            var chargePrice = serviceRehcargeKeywords.FirstOrDefault(o => o.Keyword == message.Content).Price;
            using (var entity = new MyLeagueEntities())
            {
                var rechargeCode = entity.OperatorsRechargeCodes.FirstOrDefault(o => o.OperatorId == subscriber.MobileOperator && o.ChargePrice == chargePrice && o.IsUsed == false);
                if(rechargeCode == null)
                {
                    message = MessageHandler.OutOfRechargeCodes(message, messagesTemplate);
                }
                else
                {
                    rechargeCode.IsUsed = true;
                    rechargeCode.SubscriberUsedTheCharge = subscriber.Id;
                    rechargeCode.DateUsed = DateTime.Now;
                    rechargeCode.PersianDateUsed = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    entity.Entry(rechargeCode).State = System.Data.Entity.EntityState.Modified;
                    entity.SaveChanges();
                    message = MessageHandler.SuccessfullyRecharged(message, messagesTemplate);
                    message.Content = HandleRechargeSpecialStrings(message.Content, rechargeCode);
                }
            }
            MessageHandler.InsertMessageToQueue(message);
            return;
        }

        private static string HandleRechargeSpecialStrings(string content, OperatorsRechargeCode rechargeCode)
        {
            if (content.Contains("{ChargePrice}"))
            {
                content = content.Replace("{ChargePrice}", rechargeCode.ChargePrice.ToString());
            }
            if (content.Contains("{ChargeKey}"))
            {
                content = content.Replace("{ChargeKey}", rechargeCode.ChargeCode);
            }
            if (content.Contains("{OperatorName}"))
            {
                content = content.Replace("{OperatorName}", Enum.GetName(typeof(SharedLibrary.MessageHandler.MobileOperators), rechargeCode.OperatorId));
            }
            return content;
        }

        public static bool CheckIsUserAlreadyUsedTheRechargeCode(long subscriberId)
        {
            using (var entity = new MyLeagueEntities())
            {
                var charged = entity.OperatorsRechargeCodes.FirstOrDefault(o => o.SubscriberUsedTheCharge == subscriberId);
                if (charged == null)
                    return false;
                else
                    return true;
            }
        }
    }
}
