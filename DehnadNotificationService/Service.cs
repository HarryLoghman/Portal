using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        private void IncomeWorkerThread()
        {
            while (!shutdownEvent.WaitOne(0))
            {
                Income.IncomeDiffrenceByHour();
                Thread.Sleep(60 * 60 * 1000);
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
