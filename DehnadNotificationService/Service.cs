using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace DehnadNotificationService
{
    public partial class Service : ServiceBase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Thread telegramBotThread;
        private Thread incomeThread;
        private Thread sendMessageThread;
        private Thread serviceCheckThread;
        private Thread overChargeThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (Properties.Settings.Default.IsBotServer)
            {
                telegramBotThread = new Thread(TelegramBotWorkerThread);
                telegramBotThread.IsBackground = true;
                telegramBotThread.Start();
            }
            else
            {
                incomeThread = new Thread(IncomeWorkerThread);
                incomeThread.IsBackground = true;
                incomeThread.Start();

                serviceCheckThread = new Thread(ServiceCheckWorkerThread);
                serviceCheckThread.IsBackground = true;
                serviceCheckThread.Start();

                overChargeThread = new Thread(OverChargeWorkerThread);
                overChargeThread.IsBackground = true;
                overChargeThread.Start();
            }

            sendMessageThread = new Thread(SendMessagesThread);
            sendMessageThread.IsBackground = true;
            sendMessageThread.Start();

        }

        protected override void OnStop()
        {
            try
            {
                shutdownEvent.Set();
                if (Properties.Settings.Default.IsBotServer)
                {
                    if (!telegramBotThread.Join(3000))
                    {
                        telegramBotThread.Abort();
                    }
                }
                else
                {
                    if (!incomeThread.Join(3000))
                    {
                        incomeThread.Abort();
                    }

                    if (!overChargeThread.Join(3000))
                    {
                        overChargeThread.Abort();
                    }
                }
                if (!sendMessageThread.Join(3000))
                {
                    sendMessageThread.Abort();
                }
            }
            catch (Exception exp)
            {
                logs.Info(" Exception in thread termination ");
                logs.Error(" Exception in thread termination " + exp);
            }
        }

        private void TelegramBotWorkerThread()
        {
            while (!shutdownEvent.WaitOne(0))
            {
                TelegramBot.StartBot();
                Thread.Sleep(1000);
            }
        }

        private void OverChargeWorkerThread()
        {
            while (!shutdownEvent.WaitOne(0))
            {
                try
                {
                    Income.OverChargeChecker();
                    Thread.Sleep(60 * 1000);
                }
                catch (Exception e)
                {
                    logs.Error(" Exception in OverChargeWorkerThread: " + e);
                    DehnadNotificationService.Service.SaveMessageToSendQueue("Exception in OverChargeWorkerThread", UserType.AdminOnly);
                }
            }
        }

        private void ServiceCheckWorkerThread()
        {
            while (!shutdownEvent.WaitOne(0))
            {
                try
                {
                    ServiceChecker.Job();
                    ServiceChecker.MoQueueCheck();
                    Thread.Sleep(5 * 60 * 1000);
                }
                catch (Exception e)
                {
                    logs.Error(" Exception in ServiceCheckWorkerThread: " + e);
                    DehnadNotificationService.Service.SaveMessageToSendQueue("Exception in ServiceCheckWorkerThread", UserType.AdminOnly);
                }
            }
        }

        private void IncomeWorkerThread()
        {
            while (!shutdownEvent.WaitOne(0))
            {
                try
                {
                    if (DateTime.Now.Hour == 0)
                        Thread.Sleep(1 * 60 * 60);
                    else
                    {
                        if (DateTime.Now.Minute == 0)
                            Thread.Sleep(1 * 60 * 60);
                        else
                        {
                            Income.IncomeDiffrenceByHour();
                            Thread.Sleep(60 * 60 * 1000);
                        }
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in IncomeWorkerThread:", e);
                    DehnadNotificationService.Service.SaveMessageToSendQueue("Exception in IncomeWorkerThread", UserType.AdminOnly);
                }
            }
        }

        public static void SaveMessageToSendQueue(string message, UserType userType)
        {
            try
            {
                TelegramBot.SaveTelegramMessageToQueue(message, userType);
                SendMessage.SaveSmsMessageToQueue(message, userType);
            }
            catch (Exception e)
            {
                logs.Error(" Exception in SaveMessageToSendQueue: " + e);
            }
        }

        public static bool CheckMessagesAlreadySent(string message)
        {
            bool result = false;
            try
            {
                using (var entity = new Models.NotificationEntities())
                {
                    var today = DateTime.Now;
                    var isExists = entity.SentMessages.FirstOrDefault(o => DbFunctions.TruncateTime(o.DateCreated) == today && o.Content == message);
                    if (isExists != null)
                        result = true;
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in CheckMessagesAlreadySent: " + e);
            }
            return result;
        }

        public static async void ChangeMessageStatusToSended(List<long> messageIds)
        {
            try
            {
                if (Properties.Settings.Default.UseWebServiceForDbOperations == false)
                {
                    using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                    {
                        var messages = entity.SentMessages.Where(o => messageIds.Contains(o.Id)).ToList();
                        foreach (var message in messages)
                        {
                            message.IsSent = true;
                            message.DateSent = DateTime.Now;
                            entity.Entry(message).State = System.Data.Entity.EntityState.Modified;
                        }
                        entity.SaveChanges();
                    }
                }
                else
                {
                    var serializeMessageIds = JsonConvert.SerializeObject(messageIds);
                    var userParams = new Dictionary<string, string>() { { "messageIds", serializeMessageIds } };
                    await SharedLibrary.UsefulWebApis.NotificationBotApi<string>("ChangeMessageStatusToSended", userParams);
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in ChangeMessageStatusToSended: " + e);
            }
        }

        public void SendMessagesThread()
        {
            while (!shutdownEvent.WaitOne(0))
            {
                try
                {
                    if (Properties.Settings.Default.SendTelegramMessages)
                    {
                        TelegramBot.TelegramSendMessage();
                    }
                    if (Properties.Settings.Default.SendSmsMessages)
                    {
                        SendMessage.SendMessageBySms();
                    }
                }
                catch (Exception e)
                {
                    logs.Error(" Exception in SendMessagesThread: " + e);
                }
                Thread.Sleep(60 * 1000);
            }
        }
    }
}
