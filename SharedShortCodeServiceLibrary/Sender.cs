using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;
using System.Linq;
using System.Collections;
using System.Data.Entity;
using System.Net.Http;

namespace SharedShortCodeServiceLibrary
{
    public class Sender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        SharedLibrary.Aggregators.Aggregator v_aggregator;
        SharedLibrary.Models.vw_servicesServicesInfo v_service;
        int v_messageCount;
        public Sender(string serviceCode)
        {
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                this.sb_senderConstructor(portal.vw_servicesServicesInfo.Where(o => o.ServiceCode == serviceCode).FirstOrDefault());

            }
        }
        public Sender(SharedLibrary.Models.vw_servicesServicesInfo service)
        {
            this.sb_senderConstructor(service);
        }
        private void sb_senderConstructor(SharedLibrary.Models.vw_servicesServicesInfo service)
        {
            this.v_service = service;
            this.v_aggregator = SharedLibrary.SharedVariables.fnc_getAggregator(this.v_service.aggregatorName);
            //this.v_aggregator.ev_requestFinished += aggregator_ev_requestFinished;
        }

        private void sb_sendingMessageIsFinished(object sender, EventArgs e)
        {
            //if (e.prp_webRequestType == SharedLibrary.Aggregators.enum_webRequestParameterType.message)
            //{
            System.Threading.Interlocked.Decrement(ref this.v_messageCount);
            //}
        }

        public virtual void SendHandler()
        {
            this.SendHandler(null, null);
        }

        public virtual void SendHandler(TimeSpan? timeStart, TimeSpan? timeEnd)
        {
            try
            {
                string connectionStringInAppConfig = this.v_service.ServiceCode;
                var today = DateTime.Now.Date;
                List<AutochargeMessagesBuffer> autochargeMessages;
                List<EventbaseMessagesBuffer> eventbaseMessages;
                List<OnDemandMessagesBuffer> onDemandMessages;

                DateTime timerStart, timer;
                using (var serviceEntity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(connectionStringInAppConfig))
                {
                    onDemandMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(serviceEntity, SharedLibrary.MessageHandler.MessageType.OnDemand, 0)).OfType<OnDemandMessagesBuffer>().ToList();
                    //SharedLibrary.MessageHandler.SendSelectedMessages(service, onDemandMessages);

                    System.Threading.Interlocked.Exchange(ref this.v_messageCount, 0);
                    if (onDemandMessages != null && onDemandMessages.Count() > 0)
                        System.Threading.Interlocked.Exchange(ref this.v_messageCount, onDemandMessages.Count());
                    foreach (var message in onDemandMessages)
                    {
                        this.v_aggregator.sb_sendMessage(this.v_service, message.Id, message.MobileNumber
                            , SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageSender.retryCountMax
                            , message.Content, message.DateAddedToQueue, message.Price, message.ImiChargeKey, null, false, message.RetryCount
                            , new EventHandler(this.sb_sendingMessageIsFinished));

                    }
                    timer = timerStart = DateTime.Now;
                    while (this.v_messageCount > 0)
                    {
                        if ((DateTime.Now - timer).TotalMinutes == 1)
                        {
                            logs.Debug("locked sending ondemand. remaining message count =" + this.v_messageCount + " waiting time in minute " + (DateTime.Now - timerStart).TotalMinutes.ToString());
                            timer = DateTime.Now;
                        }
                    }
                    if ((!timeStart.HasValue || timeStart.Value <= DateTime.Now.TimeOfDay) && (!timeEnd.HasValue || DateTime.Now.TimeOfDay <= timeEnd.Value))
                    {
                        autochargeMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(serviceEntity, SharedLibrary.MessageHandler.MessageType.AutoCharge, 0)).OfType<AutochargeMessagesBuffer>().ToList();
                        eventbaseMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(serviceEntity, SharedLibrary.MessageHandler.MessageType.EventBase, 0)).OfType<EventbaseMessagesBuffer>().ToList();
                        //eventbaseMessages.RemoveAll(o => o.MobileNumber == "09122137327");
                        if (autochargeMessages != null && autochargeMessages.Count() > 0)
                            System.Threading.Interlocked.Exchange(ref this.v_messageCount, autochargeMessages.Count());
                        foreach (var message in autochargeMessages)
                        {
                            this.v_aggregator.sb_sendMessage(this.v_service, message.Id, message.MobileNumber
                                , SharedLibrary.MessageHandler.MessageType.AutoCharge, SharedLibrary.MessageSender.retryCountMax
                                , message.Content, message.DateAddedToQueue, message.Price, message.ImiChargeKey, null, false, message.RetryCount
                                ,  new EventHandler(this.sb_sendingMessageIsFinished));

                        }
                        timer = timerStart = DateTime.Now;
                        while (this.v_messageCount > 0)
                        {
                            if ((DateTime.Now - timer).TotalMinutes == 1)
                            {
                                logs.Debug("locked sending autocharge. remaining message count =" + this.v_messageCount + " waiting time in minute " + (DateTime.Now - timerStart).TotalMinutes.ToString());
                                timer = DateTime.Now;
                            }
                        }

                        //SharedLibrary.MessageHandler.SendSelectedMessages(this.v_service, eventbaseMessages);
                        if (eventbaseMessages != null && eventbaseMessages.Count() > 0)
                            System.Threading.Interlocked.Exchange(ref this.v_messageCount, eventbaseMessages.Count());
                        foreach (var message in eventbaseMessages)
                        {
                            this.v_aggregator.sb_sendMessage(this.v_service, message.Id, message.MobileNumber
                                , SharedLibrary.MessageHandler.MessageType.EventBase, SharedLibrary.MessageSender.retryCountMax
                                , message.Content, message.DateAddedToQueue, message.Price, message.ImiChargeKey, null, false, message.RetryCount
                                , new EventHandler(this.sb_sendingMessageIsFinished));

                        }
                        timer = timerStart = DateTime.Now;
                        while (this.v_messageCount > 0)
                        {
                            if ((DateTime.Now - timer).TotalMinutes == 1)
                            {
                                logs.Debug("locked sending eventbase. remaining message count =" + this.v_messageCount + " waiting time in minute " + (DateTime.Now - timerStart).TotalMinutes.ToString());
                                timer = DateTime.Now;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in SendHandler:" + e);
            }
        }


    }
}
