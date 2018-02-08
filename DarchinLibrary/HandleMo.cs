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
            logs.Info("0");
            //System.Diagnostics.Debugger.Launch();
            using (var entity = new DarchinEntities())
            {
                message.ServiceCode = service.ServiceCode;
                message.ServiceId = service.Id;
                message.OperatorPlan = 0;
                message.MobileOperator = (int)SharedLibrary.MessageHandler.MobileOperators.TCT;
                logs.Info("0-1");
                bool isUserSendsSubscriptionKeyword = false;
                bool isUserWantsToUnsubscribe = false;
                if (message.Content == "Register")
                    isUserSendsSubscriptionKeyword = true;
                else if (message.Content == "Unsubscribe")
                    isUserWantsToUnsubscribe = true;
                logs.Info("0-2");
                if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
                {
                    logs.Info("0-3");
                    if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                    {
                        logs.Info("0-4");
                        var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                        if (user != null && user.DeactivationDate == null)
                        {
                            logs.Info("0-5");
                            return;
                        }
                    }
                    logs.Info("0-6");
                    var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
                    logs.Info("0-7");

                    var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                    if (message.Price == 7000)
                    {
                        message.Token = message.Token + ";ir.darchin.app;Darchin123";
                    }
                    logs.Info("0-8");
                    if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                    {
                        logs.Info("1");
                        Subscribers.CreateSubscriberAdditionalInfo(message.MobileNumber, service.Id);
                        
                        logs.Info("2");
                        ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, message.Token);
                        logs.Info("3");
                        Subscribers.AddSubscriptionPointIfItsFirstTime(message.MobileNumber, service.Id);
                        logs.Info("4");
                    }
                    else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                    {
                        ContentManager.DeleteFromSinglechargeQueue(message.MobileNumber);
                        ServiceHandler.CancelUserInstallments(message.MobileNumber);
                    }
                    else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                    {
                        logs.Info("5");
                        ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, message.Token);
                        logs.Info("6");
                    }
                    return;
                }
            }
        }
    }
}