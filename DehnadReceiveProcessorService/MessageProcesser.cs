using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DehnadReceiveProcessorService
{
    public class MessageProcesser
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Process()
        {
            try
            {
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;
                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    Parallel.ForEach(receivedChunk, receivedMessage =>
                    {
                        HandleReceivedMessage(receivedMessage);
                    });
                }
            }
            catch (Exception e)
            {
                logs.Error("Exeption in RecieveProcessor: " + e);
            }
        }

        public static void HandleReceivedMessage(ReceievedMessage receivedMessage)
        {
            var message = new MessageObject();
            message.MobileNumber = receivedMessage.MobileNumber;
            message.ShortCode = receivedMessage.ShortCode;
            message.Content = receivedMessage.Content;
            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
            message.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
            message.IsReceivedFromIntegratedPanel = receivedMessage.IsReceivedFromIntegratedPanel;
            message.ReceivedFrom = receivedMessage.ReceivedFrom;
            message.IsReceivedFromWeb = receivedMessage.IsReceivedFromWeb;

            using (var entity = new PortalEntities())
            {
                if (message.ShortCode.StartsWith("2"))
                {
                    var pardisShortCode = entity.ParidsShortCodes.FirstOrDefault(o => o.ShortCode == message.ShortCode);
                    if (pardisShortCode != null)
                        message.ShortCode = entity.ServiceInfoes.FirstOrDefault(o => o.ServiceId == pardisShortCode.ServiceId).ShortCode;
                }
                var serviceShortCodes = entity.ServiceInfoes.Where(o => o.ShortCode == message.ShortCode);
                if (serviceShortCodes != null)
                {
                    RouteUserToDesiredService(message, serviceShortCodes);
                }
                receivedMessage.IsProcessed = true;
                entity.Entry(receivedMessage).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
            }
        }

        private static void RouteUserToDesiredService(MessageObject message, IQueryable<ServiceInfo> serviceShortCodes)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    long serviceId = 0;
                    message = SharedLibrary.MessageHandler.ValidateMessage(message);

                    var isUserSendedGeneralUnsubscribeKeyword = SharedLibrary.ServiceHandler.CheckIfUserSendedUnsubscribeContentToShortCode(message.Content);
                    if (isUserSendedGeneralUnsubscribeKeyword == true && message.IsReceivedFromIntegratedPanel == false && serviceShortCodes.Count() > 1)
                    {
                        UnsubscribeUserOnAllServicesForShortCode(message);
                        //var servicesThatUserSubscribedOnShortCode = SharedLibrary.ServiceHandler.GetServicesThatUserSubscribedOnShortCode(message.MobileNumber, message.ShortCode);
                        //message.Content = SharedLibrary.MessageHandler.PrepareGeneralOffMessage(message, servicesThatUserSubscribedOnShortCode);
                        //if(serviceShortCodes.FirstOrDefault().AggregatorId == 2)
                        //    message = MyLeagueLibrary.MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        //else if(serviceShortCodes.FirstOrDefault().AggregatorId == 5)
                        //    message = SoltanLibrary.MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        //else if (serviceShortCodes.FirstOrDefault().AggregatorId == 6)
                        //    message = ShahreKalamehLibrary.MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        //else
                        //    message = Tabriz2018Library.MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        //serviceId = entity.ServiceInfoes.FirstOrDefault(o => o.ShortCode == message.ShortCode).ServiceId;
                        //var serviceCode = entity.Services.FirstOrDefault(o => o.Id == serviceId).ServiceCode;
                        //SendMessageUsingServiceCode(serviceCode, message);
                        return;
                    }
                    else if (message.IsReceivedFromIntegratedPanel == true)
                    {
                        var serviceInfo = entity.ServiceInfoes.FirstOrDefault(o => o.AggregatorServiceId == message.Content);
                        if (serviceInfo == null)
                            return;
                        message.Content = "off";
                        serviceId = serviceInfo.ServiceId;
                    }
                    else
                    {
                        if (serviceShortCodes.Count() == 1)
                            serviceId = entity.ServiceInfoes.FirstOrDefault(o => o.ShortCode == message.ShortCode).ServiceId;
                        else
                            serviceId = SharedLibrary.MessageHandler.GetServiceIdFromUserMessage(message.Content, message.ShortCode);
                        if (serviceId == 0)
                            serviceId = entity.ServiceInfoes.FirstOrDefault(o => o.ShortCode == message.ShortCode).ServiceId;
                    }
                    var service = entity.Services.FirstOrDefault(o => o.Id == serviceId);
                    if (service != null)
                    {
                        message.ServiceId = service.Id;
                        ChooseService(message, service);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in RouteUserToDesiredService: " + e);
            }
        }

        private static void UnsubscribeUserOnAllServicesForShortCode(MessageObject message)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    var serviceIds = entity.ServiceInfoes.Where(o => o.ShortCode == message.ShortCode).Select(o => o.ServiceId);
                    var userSubscribedServices = entity.Subscribers.Where(o => o.MobileNumber == message.MobileNumber && serviceIds.Contains(o.ServiceId) && o.DeactivationDate == null).Select(o => o.ServiceId).ToList();
                    if (userSubscribedServices == null)
                    {
                        var service = entity.Services.FirstOrDefault(o => o.Id == serviceIds.FirstOrDefault());
                        ChooseService(message, service);
                    }
                    else
                    {
                        foreach (var serviceId in userSubscribedServices)
                        {
                            var service = entity.Services.FirstOrDefault(o => o.Id == serviceId);
                            ChooseService(message, service);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in UnsubscribeUserOnAllServicesForShortCode: " + e);
            }
        }

        private static void ChooseService(MessageObject message, SharedLibrary.Models.Service service)
        {
            try
            {
                if (service.ServiceCode == "MyLeague")
                    MyLeagueLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Danestaneh")
                    DanestanehLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Mobiliga")
                    MobiligaLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "MashinBazha")
                    MashinBazhaLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Soltan")
                    SoltanLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Tabriz2018")
                    Tabriz2018Library.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "SepidRood")
                    SepidRoodLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Tirandazi")
                    TirandaziLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "BimeKarbala")
                    BimeKarbalaLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Boating")
                    BoatingLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "DonyayeAsatir")
                    DonyayeAsatirLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "ShahreKalameh")
                    ShahreKalamehLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "BimeIran")
                    BimeIranLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "JabehAbzar")
                    JabehAbzarLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Tamly")
                    TamlyLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "ShenoYad")
                    ShenoYadLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "FitShow")
                    FitShowLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Takavar")
                    TakavarLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "MenchBaz")
                    MenchBazLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "AvvalPod")
                    AvvalPodLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "AvvalYad")
                    AvvalYadLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "IrancellTest")
                    IrancellTestLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Soraty")
                    SoratyLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "DefendIran")
                    DefendIranLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "TahChin")
                    TahChinLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Nebula")
                    NebulaLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Dezhban")
                    DezhbanLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "MusicYad")
                    MusicYadLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Phantom")
                    PhantomLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Medio")
                    MedioLibrary.HandleMo.ReceivedMessage(message, service);
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChooseService: " + e);
            }
        }

        private static void SendMessageUsingServiceCode(string serviceCode, MessageObject message)
        {
            try
            {
                if (serviceCode == "MyLeague")
                    MyLeagueLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Danestaneh")
                    DanestanehLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Mobiliga")
                    MobiligaLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "MashinBazha")
                    MashinBazhaLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Soltan")
                    SoltanLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Tabriz2018")
                    Tabriz2018Library.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "SepidRood")
                    SepidRoodLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Tirandazi")
                    TirandaziLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "BimeKarbala")
                    BimeKarbalaLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Boating")
                    BoatingLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "DonyayeAsatir")
                    DonyayeAsatirLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "ShahreKalameh")
                    ShahreKalamehLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "BimeIran")
                    BimeIranLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "JabehAbzar")
                    JabehAbzarLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Tamly")
                    TamlyLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "ShenoYad")
                    ShenoYadLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "FitShow")
                    FitShowLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Takavar")
                    TakavarLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "MenchBaz")
                    MenchBazLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "AvvalPod")
                    AvvalPodLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "AvvalYad")
                    AvvalYadLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "IrancellTest")
                    IrancellTestLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Soraty")
                    SoratyLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "DefendIran")
                    DefendIranLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "TahChin")
                    TahChinLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Nebula")
                    NebulaLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Dezhban")
                    DezhbanLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "MusicYad")
                    MusicYadLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Phantom")
                    PhantomLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Medio")
                    MedioLibrary.MessageHandler.InsertMessageToQueue(message);
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChooseService: " + e);
            }
        }
    }
}
