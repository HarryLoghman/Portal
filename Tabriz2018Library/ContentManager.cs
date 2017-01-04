﻿using SharedLibrary.Models;
using Tabriz2018Library.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace Tabriz2018Library
{
    public class ContentManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void HandleContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            
                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
            if (message.Content != null)
                MessageHandler.InsertMessageToQueue(message);
        }
    }
}