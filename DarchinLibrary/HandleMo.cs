using SharedLibrary.Models;
using DarchinLibrary.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace DarchinLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async static void ReceivedMessage(MessageObject message, Service service)
        {
            //System.Diagnostics.Debugger.Launch();
            using (var entity = new DarchinEntities())
            {
                message.ServiceCode = service.ServiceCode;
                message.ServiceId = service.Id;
                message.OperatorPlan = 0;
                message.MobileOperator = (int)SharedLibrary.MessageHandler.MobileOperators.TCT;

                bool isUserSendsSubscriptionKeyword = false;
                bool isUserWantsToUnsubscribe = false;
                if (message.Content == "Register")
                    isUserSendsSubscriptionKeyword = true;
                else if (message.Content == "Unsubscribe")
                    isUserWantsToUnsubscribe = true;

                if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
                {
                    var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
                    var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                    if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                    {
                        Subscribers.CreateSubscriberAdditionalInfo(message.MobileNumber, service.Id);
                        if(message.Price == 7000)
                        {
                            message.Token = message.Token + ";ir.darchin.app;Darchin123";
                        }
                        ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, message.Token);
                        Subscribers.AddSubscriptionPointIfItsFirstTime(message.MobileNumber, service.Id);
                    }
                    else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                    {
                        ContentManager.DeleteFromSinglechargeQueue(message.MobileNumber);
                        ServiceHandler.CancelUserInstallments(message.MobileNumber);
                    }
                    return;
                }
            }
        }
    }
}