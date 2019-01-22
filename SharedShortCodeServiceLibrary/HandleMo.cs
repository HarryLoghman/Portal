using Microsoft.CSharp;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharedShortCodeServiceLibrary
{
    public class HandleMo
    {
        protected enum enumCommand
        {
            AppMessage = 1,
            AppVerification = 2,
            AppHelp = 4,
            OTPRequest = 8,
            OTPConfirm = 16,
            UserUnsubscription = 32,
            UserSubscription = 64,
            AggregatorSubscription = 128,
            AggregatorUnsubscription = 256,
            MatchParentIntroduction = 512,
            DoNothingReturnTrue = 1024,
            DoNothingReturnFalse = 2048,
            DoNothing = 4096,
            EmptyString = 8192,
            unknown = 0
        }
        protected virtual string prp_serviceCode { get; }
        protected virtual string prp_connectionStringeNameInAppConfig { get; }

        public HandleMo(string serviceCode)
        {
            this.prp_serviceCode = serviceCode;
            this.prp_connectionStringeNameInAppConfig = serviceCode;
            this.CompileSource();
        }

        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        CompilerResults v_compilerResult;
        protected virtual enumCommand EvaluateCommand(vw_servicesServicesInfo service, MessageObject message)
        {
            string aggregatorIntegratedPanelKeyword = "IMI";
            if (service.aggregatorName.ToLower() == "mtn")
            {
                aggregatorIntegratedPanelKeyword = "notify";
            }

            enumCommand command = enumCommand.unknown;
            if (message.ReceivedFrom.Contains("FromApp") && !message.Content.All(char.IsDigit))
            {
                command = (command == enumCommand.unknown ? enumCommand.AppMessage : command | enumCommand.AppMessage);
            }
            if (message.ReceivedFrom.Contains("AppVerification") && message.Content.Contains("sendverification"))
            {
                command = (command == enumCommand.unknown ? enumCommand.AppVerification : command | enumCommand.AppVerification);
                //command = enumCommand.AppVerification;
            }
            if (message.ReceivedFrom.Contains("Verification") && message.Content == "sendservicesubscriptionhelp")
            {
                command = (command == enumCommand.unknown ? enumCommand.AppHelp : command | enumCommand.AppHelp);
                //command = enumCommand.AppHelp;
            }
            if (((message.Content.Length == 7 || message.Content.Length == 8 || message.Content.Length == 9 || message.Content == message.ShortCode || message.Content.Length == 2) && message.Content.All(char.IsDigit))
                || message.Content.Contains("25000") || message.Content.ToLower().Contains("abc"))
            {
                command = (command == enumCommand.unknown ? enumCommand.OTPRequest : command | enumCommand.OTPRequest);
                //command = enumCommand.OTPRequest;
            }
            if (message.Content.Length == 4 && message.Content.All(char.IsDigit) && !message.ReceivedFrom.Contains("Register"))
            {
                command = (command == enumCommand.unknown ? enumCommand.OTPConfirm : command | enumCommand.OTPConfirm);
                //command = enumCommand.OTPConfirm;
            }

            if (SharedLibrary.ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content) && !message.ReceivedFrom.Contains(aggregatorIntegratedPanelKeyword))
            {
                command = (command == enumCommand.unknown ? enumCommand.UserUnsubscription : command | enumCommand.UserUnsubscription);
                //command = enumCommand.UserUnsubscription;
            }
            if (SharedLibrary.ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service) && !message.ReceivedFrom.Contains(aggregatorIntegratedPanelKeyword))
            {
                command = (command == enumCommand.unknown ? enumCommand.UserSubscription : command | enumCommand.UserSubscription);
                //command = enumCommand.UserUnsubscription;
            }
            if (message.ReceivedFrom.Contains("Register"))
            {
                command = (command == enumCommand.unknown ? enumCommand.AggregatorSubscription : command | enumCommand.AggregatorSubscription);
                //command = enumCommand.AggregatorSubscription;
            }
            if (message.ReceivedFrom.Contains("Unsubscribe") || message.ReceivedFrom.Contains("Unsubscription"))
            {
                command = (command == enumCommand.unknown ? enumCommand.AggregatorUnsubscription : command | enumCommand.AggregatorUnsubscription);
                //command = enumCommand.AggregatorUnsubscription;
            }
            if (message.Content == "30")
            {
                command = (command == enumCommand.unknown ? enumCommand.DoNothing : command | enumCommand.DoNothing);
                //command = enumCommand.DoNothing;
            }
            if (string.IsNullOrEmpty(message.Content) || message.Content == " ")
            {
                command = (command == enumCommand.unknown ? enumCommand.EmptyString : command | enumCommand.EmptyString);
                //command = enumCommand.EmptyString;
            }
            return command;
        }


        public static Singlecharge ReceivedMessageForSingleCharge(MessageObject message, vw_servicesServicesInfo service)
        {
            message.Content = message.Price.ToString();
            var content = message.Content;
            var singlecharge = new Singlecharge();
            if (message.Content.All(char.IsDigit))
            {
                var price = Convert.ToInt32(message.Content);
                var imiObject = MessageHandler.GetImiChargeObjectFromPrice(service.ServiceCode, price, null);
                message.Content = imiObject.ChargeCode.ToString();
            }
            var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate(service.ServiceCode);
            var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
            var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
                isUserSendsSubscriptionKeyword = true;
            if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
            {
                //if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                //{
                //    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                //    if (user != null && user.DeactivationDate == null)
                //    {
                //        message.Content = content;
                //        singlecharge = ContentManager.HandleSinglechargeContent(message, service, user, messagesTemplate);
                //        return singlecharge;
                //    }
                //}
                var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                {
                    if (message.IsReceivedFromIntegratedPanel)
                    {
                        message.SubUnSubMoMssage = "ارسال درخواست از طریق پنل تجمیعی غیر فعال سازی";
                        message.SubUnSubType = 2;
                    }
                    else
                    {
                        message.SubUnSubMoMssage = message.Content;
                        message.SubUnSubType = 1;
                    }
                }
                var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                {
                    Subscribers.CreateSubscriberAdditionalInfo(service.ServiceCode, message.MobileNumber, service.Id);
                    Subscribers.AddSubscriptionPointIfItsFirstTime(service.ServiceCode, message.MobileNumber, service.Id);
                    message = MessageHandler.SetImiChargeInfo(service.ServiceCode, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                }
                else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                {
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    message = MessageHandler.SetImiChargeInfo(service.ServiceCode, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                }
                else
                {
                    message = MessageHandler.SetImiChargeInfo(service.ServiceCode, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    //Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
                }
                //message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                //MessageHandler.InsertMessageToQueue(message);
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                {
                    message.Content = content;
                    singlecharge = ContentManager.HandleSinglechargeContent(service.ServiceCode, message, service, subsciber, messagesTemplate);
                }
                return singlecharge;
            }
            subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
            {
                message = MessageHandler.InvalidContentWhenNotSubscribed(service.ServiceCode, message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(service.ServiceCode, message);
                return null;
            }
            message.SubscriberId = subscriber.Id;
            if (subscriber.DeactivationDate != null)
            {
                message = MessageHandler.InvalidContentWhenNotSubscribed(service.ServiceCode, message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(service.ServiceCode, message);
                return null;
            }
            message.Content = content;
            singlecharge = ContentManager.HandleSinglechargeContent(service.ServiceCode, message, service, subscriber, messagesTemplate);
            return singlecharge;
        }


        private string getSourceFromDatabase()
        {
            string moduleSource = "";
            using (var entity = new SharedServiceEntities(this.prp_connectionStringeNameInAppConfig))
            {
                var rows = entity.ServiceCommands.Where(o => o.state == 1).OrderBy(o => o.priority);
                moduleSource = "using System;" +
                       "using System.Linq;" +
                       "namespace evaluatorNameSpace" +
                       "{" +
                       "    class runTimeEvaluation " +
                       "    {" +
                       "           public static string EvaluateCondition(string Content,string ReceivedFrom,string ShortCode)" +
                       "        { string strResult =\"\";";
                foreach (var row in rows)
                {
                    moduleSource = moduleSource + " if(" + row.condition + ")strResult+= \"" + row.commandTitle + "|\"; ";
                }
                moduleSource = moduleSource + " if(strResult.Length>0) {strResult = strResult.Remove(strResult.Length-1,1);} return strResult;" +
                 "       }/*evaluate condition*/" +
                 "   }/*class*/" +
                 "}/*nameSpace*/";


            }
            return moduleSource;
        }
        private void CompileSource()
        {
            try
            {
                CSharpCodeProvider code = new CSharpCodeProvider();
                CompilerParameters codeParameters = new CompilerParameters();
                codeParameters.GenerateExecutable = false;
                codeParameters.GenerateInMemory = true;
                //codeParameters.OutputAssembly = "TempModule";
                codeParameters.ReferencedAssemblies.Add("System.Core.dll");

                string moduleSource = this.getSourceFromDatabase();
                if (moduleSource == "")
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "There is no source specified for the " + this.prp_serviceCode);

                }
                else
                {
                    this.v_compilerResult = code.CompileAssemblyFromSource(codeParameters, moduleSource);
                    if (this.v_compilerResult.Errors.Count > 0)
                    {
                        foreach (CompilerError ce in this.v_compilerResult.Errors)
                        {
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Error in parsing " + ce.ErrorText + " in " + this.prp_serviceCode);
                            logs.Error("Error in parsing " + ce.ErrorText + " in " + this.prp_serviceCode);
                        }

                    }
                }

            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Error in creating Compiler Result for " + this.prp_serviceCode + ":" + e.Message);
                logs.Error(this.prp_serviceCode, e);
            }

        }

        protected virtual enumCommand EvaluateCommandFromDatabase(MessageObject message)
        {
            enumCommand command = enumCommand.unknown;
            try
            {
                enumCommand commandTemp;

                string evaluateResult;

                if (this.v_compilerResult == null)
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Cannot create CompilerResult for " + message.ServiceCode + ". Refer to previous Errors");
                    return enumCommand.unknown;
                }

                if (this.v_compilerResult.Errors.Count > 0)
                {
                    foreach (CompilerError ce in this.v_compilerResult.Errors)
                    {
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Error in parsing " + ce.ErrorText + " in " + message.ServiceCode);
                        logs.Error("Error in parsing " + ce.ErrorText + " in " + message.ServiceCode);
                    }

                }
                else
                {
                    try
                    {
                        MethodInfo method = this.v_compilerResult.CompiledAssembly.GetType("evaluatorNameSpace.runTimeEvaluation").GetMethod("EvaluateCondition");
                        //var obj = assembly.CreateInstance("evaluatorNameSpace.runTimeEvaluation");
                        evaluateResult = method.Invoke(null, new object[] { message.Content, message.ReceivedFrom, message.ShortCode }).ToString();

                        if (evaluateResult != "")
                        {
                            string[] evaluateResultArr = evaluateResult.Split('|');
                            foreach (string result in evaluateResultArr)
                            {
                                if (Enum.TryParse(result, out commandTemp))
                                {
                                    if (command == enumCommand.unknown)
                                        command = commandTemp;
                                    else
                                        command = command | commandTemp;
                                }
                            }
                            //logs.Info("EvaluateCommandFromDatabase" + row.commandTitle.ToString());

                        }
                    }
                    catch (Exception e)
                    {
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "EvaluateCommandFromDatabase:" + "Compiler Error1 " + this.prp_serviceCode + ":" + e.Message);
                        logs.Error(this.prp_connectionStringeNameInAppConfig + " " + this.prp_serviceCode, e);
                    }
                }
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "EvaluateCommandFromDatabase:" + "Compiler Error2 " + this.prp_serviceCode + ":" + e.Message);
                logs.Error("EvaluateCommandFromDatabase:" + this.prp_connectionStringeNameInAppConfig + " " + this.prp_serviceCode, e);
            }
            return command;

        }
        public async virtual Task<bool> ReceivedMessage(MessageObject message, vw_servicesServicesInfo service)
        {
            bool isSucceeded = true;
            string connectionStringeNameInAppConfig = this.prp_connectionStringeNameInAppConfig;
            bool mtnAggregator = false;
            if (service.aggregatorName.ToLower() == "mtn")
                mtnAggregator = true;
            try
            {

                var content = message.Content;
                message.ServiceCode = service.ServiceCode;
                message.ServiceId = service.Id;
                var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate(this.prp_connectionStringeNameInAppConfig);
                using (var entity = new SharedServiceEntities(this.prp_connectionStringeNameInAppConfig))
                {
                    int isCampaignActive = 0;
                    var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                    if (campaign != null)
                        isCampaignActive = Convert.ToInt32(campaign.Value);

                    int isMatchActive = 0;
                    var match = entity.Settings.FirstOrDefault(o => o.Name == "match");
                    if (match != null)
                    {
                        isMatchActive = Convert.ToInt32(match.Value);
                    }

                    var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(message.MobileNumber, service.Id);
                    if (isInBlackList == true)
                        isCampaignActive = (int)CampaignStatus.Deactive;
                    List<ImiChargeCode> imiChargeCodes = ServiceHandler.GetImiChargeCodes(connectionStringeNameInAppConfig).ToList();
                    //mycomment : List<ImiChargeCode> imiChargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                    message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);

                    enumCommand command = EvaluateCommandFromDatabase(message);
                    logs.Info("ReceivedMessage:Command:" + command.ToString() + "," + message.ShortCode + "," + message.MobileNumber);

                    if ((command & enumCommand.AppMessage) == enumCommand.AppMessage)
                    {
                        logs.Info("AppMessage");
                        return this.AppMessage(connectionStringeNameInAppConfig, message);
                    }
                    if ((command & enumCommand.AppVerification) == enumCommand.AppVerification)
                    {
                        logs.Info("AppVerificatiton");
                        return this.AppVerification(connectionStringeNameInAppConfig, message, messagesTemplate);
                    }
                    if ((command & enumCommand.AppHelp) == enumCommand.AppHelp)
                    {
                        logs.Info("AppHelp");
                        return this.AppHelp(connectionStringeNameInAppConfig, message, messagesTemplate);
                    }
                    if ((command & enumCommand.OTPRequest) == enumCommand.OTPRequest)
                    {
                        logs.Info("otpRequest");
                        return this.OTPRequest(connectionStringeNameInAppConfig, message, messagesTemplate, service, isCampaignActive).Result;
                    }
                    if ((command & enumCommand.OTPConfirm) == enumCommand.OTPConfirm)
                    {
                        return this.OTPConfirm(connectionStringeNameInAppConfig, message, messagesTemplate).Result;
                    }
                    if ((command & enumCommand.UserUnsubscription) == enumCommand.UserUnsubscription)
                    {
                        SharedLibrary.HandleSubscription.UnsubscribeUserFromTelepromoService(service.Id, message.MobileNumber);
                        return isSucceeded;
                    }
                    if ((command & enumCommand.UserSubscription) == enumCommand.UserSubscription)
                    {
                        return isSucceeded;
                    }
                    if ((command & enumCommand.MatchParentIntroduction) == enumCommand.MatchParentIntroduction
                        && isCampaignActive == (int)CampaignStatus.Active && isMatchActive == (int)CampaignStatus.Active)
                    {
                        return this.MatchParentIntroduction(connectionStringeNameInAppConfig, service, message, messagesTemplate).Result;
                    }
                    if ((command & enumCommand.DoNothing) == enumCommand.DoNothing)
                    {

                    }
                    if ((command & enumCommand.DoNothingReturnFalse) == enumCommand.DoNothingReturnFalse)
                    {
                        return false;
                    }
                    if ((command & enumCommand.DoNothingReturnTrue) == enumCommand.DoNothingReturnTrue)
                    {
                        return true;
                    }


                    if ((message.IsReceivedFromIntegratedPanel == true) && !message.ReceivedFrom.Contains("IMI"))
                    {
                        SharedLibrary.HandleSubscription.UnsubscribeUserFromTelepromoService(service.Id, message.MobileNumber);
                        return isSucceeded;
                    }

                    bool aggregatorSendsSubscriptionKeyword = false;
                    bool aggregatorSendsUnSubscriptionKeyword = false;

                    if ((command & enumCommand.AggregatorSubscription) == enumCommand.AggregatorSubscription)
                        aggregatorSendsSubscriptionKeyword = true;
                    else if ((command & enumCommand.AggregatorUnsubscription) == enumCommand.AggregatorUnsubscription)
                        aggregatorSendsUnSubscriptionKeyword = true;

                    var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                    #region user wants subscribe or unsubscribe
                    if (aggregatorSendsSubscriptionKeyword == true)
                    {
                        this.DeactiveOldServices(service, message);
                        if (this.Verfiy2Step(service, message, content))
                            return isSucceeded;
                    }

                    if (aggregatorSendsSubscriptionKeyword == true || aggregatorSendsUnSubscriptionKeyword == true)
                    {
                        var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, aggregatorSendsUnSubscriptionKeyword);

                        #region received content is sub/unsub/renewal set message.SubUnSubMoMssage and message.SubUnSubType
                        if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated
                            || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated
                            || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                        {
                            if (message.IsReceivedFromIntegratedPanel)
                            {
                                message.SubUnSubMoMssage = "ارسال درخواست از طریق پنل تجمیعی غیر فعال سازی";
                                message.SubUnSubType = 2;
                            }
                            else
                            {
                                message.SubUnSubMoMssage = message.Content;
                                message.SubUnSubType = 1;
                            }
                        }
                        #endregion
                        message = this.SetMessageDueToSubscriberStatus(connectionStringeNameInAppConfig, service, message, serviceStatusForSubscriberState, content);
                        this.CampaignManagment(connectionStringeNameInAppConfig, service, message, subscriber, messagesTemplate, serviceStatusForSubscriberState
                            , isCampaignActive, isMatchActive);
                        this.PrepareSubscriptionMessage(connectionStringeNameInAppConfig, entity, service, message, messagesTemplate
                            , serviceStatusForSubscriberState, isCampaignActive);

                        return isSucceeded;
                    }
                    #endregion

                    //var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

                    #region there is no such subscriber
                    if (subscriber == null)
                    {
                        if (isCampaignActive == (int)CampaignStatus.Active)
                        {
                            if ((command & enumCommand.EmptyString) == enumCommand.EmptyString)// message.Content == null || message.Content == "" || message.Content == " ")
                                message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                            else
                                message.Content = messagesTemplate.Where(o => o.Title == "CampaignInvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                        }
                        else
                        {
                            if ((command & enumCommand.EmptyString) == enumCommand.EmptyString)//message.Content == null || message.Content == "" || message.Content == " ")
                                message = SharedShortCodeServiceLibrary.MessageHandler.EmptyContentWhenNotSubscribed(service.ServiceCode, message, messagesTemplate);
                            else
                                message = SharedShortCodeServiceLibrary.MessageHandler.InvalidContentWhenNotSubscribed(connectionStringeNameInAppConfig, message, messagesTemplate);
                        }
                        if (!mtnAggregator)
                            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                        return isSucceeded;
                    }
                    #endregion

                    message.SubscriberId = subscriber.Id;

                    #region subscriber exists but deactivated check campaign status
                    if (subscriber.DeactivationDate != null)
                    {
                        if (isCampaignActive == (int)CampaignStatus.Active)
                        {
                            if ((command & enumCommand.EmptyString) == enumCommand.EmptyString)//message.Content == null || message.Content == "" || message.Content == " ")
                                message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                            else
                                message.Content = messagesTemplate.Where(o => o.Title == "CampaignInvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                        }
                        else
                        {
                            if ((command & enumCommand.EmptyString) == enumCommand.EmptyString)//message.Content == null || message.Content == "" || message.Content == " ")
                                message = SharedShortCodeServiceLibrary.MessageHandler.EmptyContentWhenNotSubscribed(service.ServiceCode, message, messagesTemplate);
                            else
                                message = SharedShortCodeServiceLibrary.MessageHandler.InvalidContentWhenNotSubscribed(connectionStringeNameInAppConfig, message, messagesTemplate);
                        }
                        MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                        return isSucceeded;
                    }
                    #endregion
                    message.Content = content;
                    ContentManager.HandleContent(connectionStringeNameInAppConfig, message, service, subscriber, messagesTemplate, imiChargeCodes);
                }
            }
            catch (Exception e)
            {
                logs.Error(connectionStringeNameInAppConfig + ":", e);
            }
            return isSucceeded;
        }

        protected virtual bool AppMessage(string connectionStringeNameInAppConfig, MessageObject message)
        {
            bool isSucceeded = true;
            message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
            return isSucceeded;
        }

        protected virtual bool AppVerification(string connectionStringeNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            bool isSucceeded = true;
            var verficationMessage = message.Content.Split('-');
            message.Content = messagesTemplate.Where(o => o.Title == "VerificationMessage").Select(o => o.Content).FirstOrDefault();
            message.Content = message.Content.Replace("{CODE}", verficationMessage[1]);
            message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
            return isSucceeded;
        }

        protected virtual bool AppHelp(string connectionStringeNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            bool isSucceeded = true;
            message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "SendServiceSubscriptionHelp").Select(o => o.Content).FirstOrDefault();
            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
            return isSucceeded;
        }

        protected async virtual Task<bool> OTPRequest(string connectionStringeNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate
            , vw_servicesServicesInfo service, int isCampaignActive)
        {
            bool isSucceeded = true;
            if (message.Content.Contains("25000"))
                message.Content = "25000";
            var logId = MessageHandler.OtpLog(connectionStringeNameInAppConfig, message.MobileNumber, "request", message.Content);
            var result = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
            MessageHandler.OtpLogUpdate(connectionStringeNameInAppConfig, logId, result.Status.ToString());
            if (result.Status == "User already subscribed")
            {
                message.Content = messagesTemplate.Where(o => o.Title == "OtpRequestForAlreadySubsceribed").Select(o => o.Content).FirstOrDefault();
                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
            }
            else if (result.Status == "Otp request already exists for this subscriber")
            {
                message.Content = messagesTemplate.Where(o => o.Title == "OtpRequestExistsForThisSubscriber").Select(o => o.Content).FirstOrDefault();
                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
            }
            else if (result.Status != "SUCCESS-Pending Confirmation")
            {
                if (result.Status == "Error" || result.Status == "Exception")
                    isSucceeded = false;
                else
                {
                    message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                }

            }
            else
            {
                if (isCampaignActive == (int)CampaignStatus.Active)
                {
                    SharedLibrary.HandleSubscription.AddToTempReferral(message.MobileNumber, service.Id, message.Content);
                    message.Content = messagesTemplate.Where(o => o.Title == "CampaignOtpFromUniqueId").Select(o => o.Content).FirstOrDefault();
                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                }
            }
            return isSucceeded;
        }

        protected async virtual Task<bool> OTPConfirm(string connectionStringeNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            bool isSucceeded = true;
            var confirmCode = message.Content;
            var logId = MessageHandler.OtpLog(connectionStringeNameInAppConfig, message.MobileNumber, "confirm", confirmCode);
            var result = await SharedLibrary.UsefulWebApis.MciOtpSendConfirmCode(message.ServiceCode, message.MobileNumber, confirmCode);
            MessageHandler.OtpLogUpdate(connectionStringeNameInAppConfig, logId, result.Status.ToString());
            if (result.Status == "Error" || result.Status == "Exception")
                isSucceeded = false;
            else if (result.Status.ToString().Contains("NOT FOUND IN LAST 5MINS") || result.Status == "No Otp Request Found")
            {
                var logId2 = MessageHandler.OtpLog(connectionStringeNameInAppConfig, message.MobileNumber, "request", message.Content);
                var result2 = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
                MessageHandler.OtpLogUpdate(connectionStringeNameInAppConfig, logId2, result2.Status.ToString());
                message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
            }
            else if (result.Status.ToString().Contains("PIN DOES NOT MATCH"))
            {
                message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
            }
            return isSucceeded;
        }

        protected virtual MessageObject SetMessageDueToSubscriberStatus(string connectionStringeNameInAppConfig, vw_servicesServicesInfo service, MessageObject message
            , SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState serviceStatusForSubscriberState
            , string content)
        {
            if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
            {
                Subscribers.CreateSubscriberAdditionalInfo(connectionStringeNameInAppConfig, message.MobileNumber, service.Id);
                Subscribers.AddSubscriptionPointIfItsFirstTime(connectionStringeNameInAppConfig, message.MobileNumber, service.Id);
                message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                ContentManager.AddSubscriberToSinglechargeQueue(connectionStringeNameInAppConfig, message.MobileNumber, content);
            }
            else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
            {
                ContentManager.DeleteFromSinglechargeQueue(connectionStringeNameInAppConfig, message.MobileNumber);
                ServiceHandler.CancelUserInstallments(connectionStringeNameInAppConfig, message.MobileNumber);
                var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
            }
            else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
            {
                message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                Subscribers.SetIsSubscriberSendedOffReason(connectionStringeNameInAppConfig, subscriberId.Value, false);
                ContentManager.AddSubscriberToSinglechargeQueue(connectionStringeNameInAppConfig, message.MobileNumber, content);
            }
            else
                message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            return message;
        }

        protected virtual async Task<bool> MatchParentIntroduction(string connectionStringInappConfig
            , vw_servicesServicesInfo service, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            bool isSucceeded = true;
            var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
            var sha = SharedLibrary.Security.GetSha256Hash("parent" + message.MobileNumber);
            //dynamic result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/parent.php", string.Format("code={0}&parent_code={1}&number={2}&kc={3}", sub.SpecialUniqueId, message.Content, message.MobileNumber, sha));
            dynamic result = await SharedLibrary.UsefulWebApis.DanoopReferral(service.referralUrl + (service.referralUrl.EndsWith("/") ? "" : "/") + "parent.php", string.Format("code={0}&parent_code={1}&number={2}&kc={3}", sub.SpecialUniqueId, message.Content, message.MobileNumber, sha));

            message = MessageHandler.SetImiChargeInfo(connectionStringInappConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = "";
            if (result.status.ToString() == "ok")
                message.Content = messagesTemplate.Where(o => o.Title == "ParentReferralCodeExists").Select(o => o.Content).FirstOrDefault();
            else
                message.Content = messagesTemplate.Where(o => o.Title == "ParentReferralCodeNotExists").Select(o => o.Content).FirstOrDefault();
            if (message.Content != "")
                MessageHandler.InsertMessageToQueue(connectionStringInappConfig, message);
            return isSucceeded;
        }

        protected virtual async void CampaignManagment(string connectionStringeNameInAppConfig
            , vw_servicesServicesInfo service, MessageObject message
            , Subscriber subscriber, List<MessagesTemplate> messagesTemplate
            , SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState serviceStatusForSubscriberState
            , int isCampaignActive, int isMatchActive)
        {
            #region campaignActive and user is active or renewal
            if (isCampaignActive == (int)CampaignStatus.Active && (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal))
            {
                //create a special uniqueId for the mobilenumber in serviceid and save to DB
                SharedLibrary.HandleSubscription.CampaignUniqueId(message.MobileNumber, service.Id);
                subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                string parentId = "1";
                var subscriberInviterCode = SharedLibrary.HandleSubscription.IsSubscriberInvited(message.MobileNumber, service.Id);
                if (subscriberInviterCode != "")
                {
                    parentId = subscriberInviterCode;
                    SharedLibrary.HandleSubscription.AddReferral(subscriberInviterCode, subscriber.SpecialUniqueId);
                }
                var subId = "1";
                var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                if (sub != null)
                    subId = sub.SpecialUniqueId;
                var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
                if (!string.IsNullOrEmpty(service.referralUrl))
                {
                    //var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/ashpazkhoone/sub.php", string.Format("code={0}&number={1}&parent_code={2}&kc={3}", subId, message.MobileNumber, parentId, sha)); 
                    var result = await SharedLibrary.UsefulWebApis.DanoopReferral(service.referralUrl + (service.referralUrl.EndsWith("/") ? "" : "/") + "sub.php", string.Format("code={0}&number={1}&parent_code={2}&kc={3}", subId, message.MobileNumber, parentId, sha));
                    if (result.description == "success")
                    {
                        if (parentId != "1")
                        {
                            var parentSubscriber = SharedLibrary.HandleSubscription.GetSubscriberBySpecialUniqueId(parentId);
                            if (parentSubscriber != null)
                            {
                                var oldMobileNumber = message.MobileNumber;
                                var oldSubId = message.SubscriberId;
                                var newMessage = message;
                                newMessage.MobileNumber = parentSubscriber.MobileNumber;
                                newMessage.SubscriberId = parentSubscriber.Id;
                                newMessage = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                                newMessage.Content = messagesTemplate.Where(o => o.Title == "CampaignNotifyParentForNewReferral").Select(o => o.Content).FirstOrDefault();
                                if (newMessage.Content.Contains("{REFERRALCODE}"))
                                {
                                    newMessage.Content = message.Content.Replace("{REFERRALCODE}", parentSubscriber.SpecialUniqueId);
                                }
                                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, newMessage);
                                message.MobileNumber = oldMobileNumber;
                                message.SubscriberId = oldSubId;
                            }
                        }
                    }
                }
            }
            #endregion
            #region matchActive and user is active or renewal
            else if (isMatchActive == (int)CampaignStatus.Active && (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal))
            {
                var sha = SharedLibrary.Security.GetSha256Hash("match" + message.MobileNumber);
                //var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/sub.php", string.Format("number={0}&kc={1}", message.MobileNumber, sha));
                var result = await SharedLibrary.UsefulWebApis.DanoopReferral(service.referralUrl + (service.referralUrl.EndsWith("/") ? "" : "/") + "sub.php", string.Format("number={0}&kc={3}", message.MobileNumber, sha));
                if (result.description == "success")
                {
                }
            }
            #endregion
            #region campaignActive or suspend and user is deactivated
            else if ((isCampaignActive == (int)CampaignStatus.Active || isCampaignActive == (int)CampaignStatus.Suspend) && serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
            {
                var subId = "1";
                var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                if (sub != null && sub.SpecialUniqueId != null)
                {
                    subId = sub.SpecialUniqueId;
                    var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);

                    //var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/ashpazkhoone/unsub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));

                    if (!string.IsNullOrEmpty(service.referralUrl))
                    {
                        var result = await SharedLibrary.UsefulWebApis.DanoopReferral(service.referralUrl + (service.referralUrl.EndsWith("/") ? "" : "/") + "unsub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
                    }
                }
            }

            #endregion
        }

        protected virtual void PrepareSubscriptionMessage(string connectionStringeNameInAppConfig
            , SharedServiceEntities entity, vw_servicesServicesInfo service, MessageObject message
            , List<MessagesTemplate> messagesTemplate
            , SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState serviceStatusForSubscriberState
            , int isCampaignActive)
        {
            message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState, isCampaignActive);
            if (message.Content.Contains("{REFERRALCODE}"))
            {
                var subId = "1";
                var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                if (sub != null && sub.SpecialUniqueId != null)
                    subId = sub.SpecialUniqueId;
                message.Content = message.Content.Replace("{REFERRALCODE}", subId);
            }

            Setting set = entity.Settings.Where(o => o.Name == "CreateDeactivatedMessage").FirstOrDefault();
            if (serviceStatusForSubscriberState != SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated
                || (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated
                        && (set == null || string.IsNullOrEmpty(set.Value) || set.Value == "1")))
                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        }

        protected async virtual void DeactiveOldServices(vw_servicesServicesInfo service, MessageObject message)
        {
            var oldServicesStr = service.oldServiceCodes;
            if (!string.IsNullOrEmpty(oldServicesStr))
            {
                int i;
                string[] oldServicesArr = oldServicesStr.Split(';');
                for (i = 0; i <= oldServicesArr.Length - 1; i++)
                {

                    var oldService = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(oldServicesArr[i]);
                    var oldServiceSubscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, oldService.Id);
                    if (oldServiceSubscriber != null && oldServiceSubscriber.DeactivationDate == null)
                    {
                        await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(oldService.ServiceCode, message.MobileNumber, "-1");
                    }

                }
            }
        }

        protected virtual bool Verfiy2Step(vw_servicesServicesInfo service, MessageObject message, string content)
        {
            var isSucceeded = true;
            if (service.Enable2StepSubscription == true)
            {
                bool isSubscriberdVerified = ServiceHandler.IsUserVerifedTheSubscription(message.MobileNumber, message.ServiceId, content);
                if (isSubscriberdVerified == false)
                {
                    return isSucceeded;
                }
            }
            return false;
        }
        //public async virtual Task<bool> ReceivedMessageOld(MessageObject message, Service service)
        //{
        //    string connectionStringeNameInAppConfig = service.ServiceCode + "Entities";
        //    bool isSucceeded = true;
        //    var content = message.Content;
        //    message.ServiceCode = service.ServiceCode;
        //    message.ServiceId = service.Id;
        //    var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate(connectionStringeNameInAppConfig);
        //    using (var entity = new SharedServiceEntities(connectionStringeNameInAppConfig))
        //    {
        //        int isCampaignActive = 0;
        //        var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
        //        if (campaign != null)
        //            isCampaignActive = Convert.ToInt32(campaign.Value);
        //        var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(message.MobileNumber, service.Id);
        //        if (isInBlackList == true)
        //            isCampaignActive = (int)CampaignStatus.Deactive;
        //        List<ImiChargeCode> imiChargeCodes = ServiceHandler.GetImiChargeCodes(connectionStringeNameInAppConfig).ToList();
        //        //mycomment : List<ImiChargeCode> imiChargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
        //        message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);

        //        enumCommand command = EvaluateCommand(service, message);

        //        if (command == enumCommand.AppMessage)
        //        {
        //            return this.AppMessage(connectionStringeNameInAppConfig, message);
        //        }
        //        else if (command == enumCommand.AppVerification)
        //        {
        //            return this.AppVerification(connectionStringeNameInAppConfig, message, messagesTemplate);
        //        }
        //        else if (command == enumCommand.AppHelp)
        //        {
        //            return this.AppHelp(connectionStringeNameInAppConfig, message, messagesTemplate);
        //        }
        //        else if (command == enumCommand.OTPRequest)
        //        {
        //            return await this.OTPRequest(connectionStringeNameInAppConfig, message, messagesTemplate, service, isCampaignActive);
        //        }
        //        else if (command == enumCommand.OTPConfirm)
        //        {
        //            return await this.OTPConfirm(connectionStringeNameInAppConfig, message, messagesTemplate);
        //        }
        //        #region FromApp
        //        if (message.ReceivedFrom.Contains("FromApp") && !message.Content.All(char.IsDigit))
        //        {
        //            message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
        //            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //            return isSucceeded;
        //        }
        //        #endregion
        //        #region AppVerification
        //        else if (message.ReceivedFrom.Contains("AppVerification") && message.Content.Contains("sendverification"))
        //        {
        //            var verficationMessage = message.Content.Split('-');
        //            message.Content = messagesTemplate.Where(o => o.Title == "VerificationMessage").Select(o => o.Content).FirstOrDefault();
        //            message.Content = message.Content.Replace("{CODE}", verficationMessage[1]);
        //            message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
        //            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //            return isSucceeded;
        //        }
        //        #endregion
        //        #region App Subscription Help
        //        else if (message.ReceivedFrom.Contains("Verification") && message.Content == "sendservicesubscriptionhelp")
        //        {
        //            message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
        //            message.Content = messagesTemplate.Where(o => o.Title == "SendServiceSubscriptionHelp").Select(o => o.Content).FirstOrDefault();
        //            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //            return isSucceeded;
        //        }
        //        #endregion
        //        #region otp Request
        //        else if (((message.Content.Length == 7 || message.Content.Length == 8 || message.Content.Length == 9 || message.Content == message.ShortCode || message.Content.Length == 2) && message.Content.All(char.IsDigit)) || message.Content.Contains("25000") || message.Content.ToLower().Contains("abc"))
        //        {

        //            if (message.Content.Contains("25000"))
        //                message.Content = "25000";
        //            var logId = MessageHandler.OtpLog(connectionStringeNameInAppConfig, message.MobileNumber, "request", message.Content);
        //            var result = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
        //            MessageHandler.OtpLogUpdate(connectionStringeNameInAppConfig, logId, result.Status.ToString());
        //            if (result.Status == "User already subscribed")
        //            {
        //                message.Content = messagesTemplate.Where(o => o.Title == "OtpRequestForAlreadySubsceribed").Select(o => o.Content).FirstOrDefault();
        //                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //            }
        //            else if (result.Status == "Otp request already exists for this subscriber")
        //            {
        //                message.Content = messagesTemplate.Where(o => o.Title == "OtpRequestExistsForThisSubscriber").Select(o => o.Content).FirstOrDefault();
        //                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //            }
        //            else if (result.Status != "SUCCESS-Pending Confirmation")
        //            {
        //                if (result.Status == "Error" || result.Status == "Exception")
        //                    isSucceeded = false;
        //                else
        //                {
        //                    message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
        //                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //                }

        //            }
        //            else
        //            {
        //                if (isCampaignActive == (int)CampaignStatus.Active)
        //                {
        //                    SharedLibrary.HandleSubscription.AddToTempReferral(message.MobileNumber, service.Id, message.Content);
        //                    message.Content = messagesTemplate.Where(o => o.Title == "CampaignOtpFromUniqueId").Select(o => o.Content).FirstOrDefault();
        //                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //                }
        //            }
        //            return isSucceeded;
        //        }
        //        #endregion
        //        #region otp confirm
        //        else if (message.Content.Length == 4 && message.Content.All(char.IsDigit) && !message.ReceivedFrom.Contains("Register"))
        //        {
        //            var confirmCode = message.Content;
        //            var logId = MessageHandler.OtpLog(connectionStringeNameInAppConfig, message.MobileNumber, "confirm", confirmCode);
        //            var result = await SharedLibrary.UsefulWebApis.MciOtpSendConfirmCode(message.ServiceCode, message.MobileNumber, confirmCode);
        //            MessageHandler.OtpLogUpdate(connectionStringeNameInAppConfig, logId, result.Status.ToString());
        //            if (result.Status == "Error" || result.Status == "Exception")
        //                isSucceeded = false;
        //            else if (result.Status.ToString().Contains("NOT FOUND IN LAST 5MINS") || result.Status == "No Otp Request Found")
        //            {
        //                var logId2 = MessageHandler.OtpLog(connectionStringeNameInAppConfig, message.MobileNumber, "request", message.Content);
        //                var result2 = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
        //                MessageHandler.OtpLogUpdate(connectionStringeNameInAppConfig, logId2, result2.Status.ToString());
        //                message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
        //                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //            }
        //            else if (result.Status.ToString().Contains("PIN DOES NOT MATCH"))
        //            {
        //                message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
        //                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //            }
        //            return isSucceeded;
        //        }
        //        #endregion

        //        var isUserSendsSubscriptionKeyword = SharedLibrary.ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
        //        var isUserWantsToUnsubscribe = SharedLibrary.ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);

        //        #region (user wants to unsubscribe or message came from integrated panel) and it did not come from IMI UnsubscribeUserFromTelepromoService
        //        if ((isUserWantsToUnsubscribe == true || message.IsReceivedFromIntegratedPanel == true) && !message.ReceivedFrom.Contains("IMI"))
        //        {
        //            SharedLibrary.HandleSubscription.UnsubscribeUserFromTelepromoService(service.Id, message.MobileNumber);
        //            return isSucceeded;
        //        }
        //        #endregion
        //        #region (user wants to unsub or sub) and message did not come from IMI
        //        if ((isUserWantsToUnsubscribe == true || isUserSendsSubscriptionKeyword == true) && !message.ReceivedFrom.Contains("IMI"))
        //            return isSucceeded;
        //        #endregion

        //        if (message.ReceivedFrom.Contains("Register"))
        //            isUserSendsSubscriptionKeyword = true;
        //        else if (message.ReceivedFrom.Contains("Unsubscribe") || message.ReceivedFrom.Contains("Unsubscription"))
        //            isUserWantsToUnsubscribe = true;

        //        #region user wants subscribe or unsubscribe
        //        if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
        //        {
        //            if (isUserSendsSubscriptionKeyword == true)
        //            {
        //                #region unsub user from old corresponding services
        //                var oldServicesStr = service.oldServiceCodes;
        //                if (!string.IsNullOrEmpty(oldServicesStr))
        //                {
        //                    int i;
        //                    string[] oldServicesArr = oldServicesStr.Split(';');
        //                    for (i = 0; i <= oldServicesArr.Length - 1; i++)
        //                    {

        //                        var oldService = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(oldServicesArr[i]);
        //                        var oldServiceSubscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, oldService.Id);
        //                        if (oldServiceSubscriber != null && oldServiceSubscriber.DeactivationDate == null)
        //                        {
        //                            await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(oldService.ServiceCode, message.MobileNumber, "-1");
        //                        }

        //                    }
        //                }
        //                #endregion

        //                #region Enable2StepSubscription=true if user is verified return true
        //                if (service.Enable2StepSubscription == true)
        //                {
        //                    bool isSubscriberdVerified = ServiceHandler.IsUserVerifedTheSubscription(message.MobileNumber, message.ServiceId, content);
        //                    if (isSubscriberdVerified == false)
        //                    {
        //                        return isSucceeded;
        //                    }
        //                }
        //                #endregion
        //            }

        //            var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);

        //            #region received content is sub/unsub/renewal set message.SubUnSubMoMssage and message.SubUnSubType
        //            if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
        //            {
        //                if (message.IsReceivedFromIntegratedPanel)
        //                {
        //                    message.SubUnSubMoMssage = "ارسال درخواست از طریق پنل تجمیعی غیر فعال سازی";
        //                    message.SubUnSubType = 2;
        //                }
        //                else
        //                {
        //                    message.SubUnSubMoMssage = message.Content;
        //                    message.SubUnSubType = 1;
        //                }
        //            }
        //            #endregion

        //            var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

        //            #region subscriber additionalIndfo/subscriber points/add to single charge
        //            if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
        //            {
        //                Subscribers.CreateSubscriberAdditionalInfo(connectionStringeNameInAppConfig, message.MobileNumber, service.Id);
        //                Subscribers.AddSubscriptionPointIfItsFirstTime(connectionStringeNameInAppConfig, message.MobileNumber, service.Id);
        //                message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
        //                ContentManager.AddSubscriberToSinglechargeQueue(connectionStringeNameInAppConfig, message.MobileNumber, content);
        //            }
        //            else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
        //            {
        //                ContentManager.DeleteFromSinglechargeQueue(connectionStringeNameInAppConfig, message.MobileNumber);
        //                ServiceHandler.CancelUserInstallments(connectionStringeNameInAppConfig, message.MobileNumber);
        //                var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
        //                message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
        //            }
        //            else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
        //            {
        //                message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
        //                var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
        //                Subscribers.SetIsSubscriberSendedOffReason(connectionStringeNameInAppConfig, subscriberId.Value, false);
        //                ContentManager.AddSubscriberToSinglechargeQueue(connectionStringeNameInAppConfig, message.MobileNumber, content);
        //            }
        //            else
        //                message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
        //            #endregion

        //            #region campaignActive and user is active or renewal
        //            if (isCampaignActive == (int)CampaignStatus.Active && (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal))
        //            {
        //                //create a special uniqueId for the mobilenumber in serviceid and save to DB
        //                SharedLibrary.HandleSubscription.CampaignUniqueId(message.MobileNumber, service.Id);
        //                subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
        //                string parentId = "1";
        //                var subscriberInviterCode = SharedLibrary.HandleSubscription.IsSubscriberInvited(message.MobileNumber, service.Id);
        //                if (subscriberInviterCode != "")
        //                {
        //                    parentId = subscriberInviterCode;
        //                    SharedLibrary.HandleSubscription.AddReferral(subscriberInviterCode, subsciber.SpecialUniqueId);
        //                }
        //                var subId = "1";
        //                var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
        //                if (sub != null)
        //                    subId = sub.SpecialUniqueId;
        //                var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
        //                if (!string.IsNullOrEmpty(service.referralUrl))
        //                {
        //                    //var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/ashpazkhoone/sub.php", string.Format("code={0}&number={1}&parent_code={2}&kc={3}", subId, message.MobileNumber, parentId, sha)); 
        //                    var result = await SharedLibrary.UsefulWebApis.DanoopReferral(service.referralUrl + (service.referralUrl.EndsWith("/") ? "" : "/") + "sub.php", string.Format("code={0}&number={1}&parent_code={2}&kc={3}", subId, message.MobileNumber, parentId, sha));
        //                    if (result.description == "success")
        //                    {
        //                        if (parentId != "1")
        //                        {
        //                            var parentSubscriber = SharedLibrary.HandleSubscription.GetSubscriberBySpecialUniqueId(parentId);
        //                            if (parentSubscriber != null)
        //                            {
        //                                var oldMobileNumber = message.MobileNumber;
        //                                var oldSubId = message.SubscriberId;
        //                                var newMessage = message;
        //                                newMessage.MobileNumber = parentSubscriber.MobileNumber;
        //                                newMessage.SubscriberId = parentSubscriber.Id;
        //                                newMessage = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
        //                                newMessage.Content = messagesTemplate.Where(o => o.Title == "CampaignNotifyParentForNewReferral").Select(o => o.Content).FirstOrDefault();
        //                                if (newMessage.Content.Contains("{REFERRALCODE}"))
        //                                {
        //                                    newMessage.Content = message.Content.Replace("{REFERRALCODE}", parentSubscriber.SpecialUniqueId);
        //                                }
        //                                MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, newMessage);
        //                                message.MobileNumber = oldMobileNumber;
        //                                message.SubscriberId = oldSubId;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            #endregion
        //            #region campaignActive and user is deactivated
        //            else if ((isCampaignActive == (int)CampaignStatus.Active || isCampaignActive == (int)CampaignStatus.Suspend) && serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
        //            {
        //                var subId = "1";
        //                var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
        //                if (sub != null && sub.SpecialUniqueId != null)
        //                {
        //                    subId = sub.SpecialUniqueId;
        //                    var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);

        //                    //var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/ashpazkhoone/unsub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));

        //                    if (!string.IsNullOrEmpty(service.referralUrl))
        //                    {
        //                        var result = await SharedLibrary.UsefulWebApis.DanoopReferral(service.referralUrl + (service.referralUrl.EndsWith("/") ? "" : "/") + "unsub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
        //                    }
        //                }
        //            }
        //            #endregion


        //            message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState, isCampaignActive);
        //            if (message.Content.Contains("{REFERRALCODE}"))
        //            {
        //                var subId = "1";
        //                var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
        //                if (sub != null && sub.SpecialUniqueId != null)
        //                    subId = sub.SpecialUniqueId;
        //                message.Content = message.Content.Replace("{REFERRALCODE}", subId);
        //            }

        //            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);

        //            return isSucceeded;
        //        }
        //        #endregion

        //        var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

        //        #region there is no such subscriber check campaign status
        //        if (subscriber == null)
        //        {
        //            if (isCampaignActive == (int)CampaignStatus.Active)
        //            {
        //                if (message.Content == null || message.Content == "" || message.Content == " ")
        //                    message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
        //                else
        //                    message.Content = messagesTemplate.Where(o => o.Title == "CampaignInvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
        //            }
        //            else
        //            {
        //                if (message.Content == null || message.Content == "" || message.Content == " ")
        //                    message = SharedLibrary.MessageHandler.EmptyContentWhenNotSubscribed(entity, imiChargeCodes, message, messagesTemplate);
        //                else
        //                    message = MessageHandler.InvalidContentWhenNotSubscribed(connectionStringeNameInAppConfig, message, messagesTemplate);
        //            }
        //            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //            return isSucceeded;
        //        }
        //        #endregion

        //        message.SubscriberId = subscriber.Id;

        //        #region subscriber exists but deactivated check campaign status
        //        if (subscriber.DeactivationDate != null)
        //        {
        //            if (isCampaignActive == (int)CampaignStatus.Active)
        //            {
        //                if (message.Content == null || message.Content == "" || message.Content == " ")
        //                    message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
        //                else
        //                    message.Content = messagesTemplate.Where(o => o.Title == "CampaignInvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
        //            }
        //            else
        //            {
        //                if (message.Content == null || message.Content == "" || message.Content == " ")
        //                    message = SharedLibrary.MessageHandler.EmptyContentWhenNotSubscribed(entity, imiChargeCodes, message, messagesTemplate);
        //                else
        //                    message = MessageHandler.InvalidContentWhenNotSubscribed(connectionStringeNameInAppConfig, message, messagesTemplate);
        //            }
        //            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
        //            return isSucceeded;
        //        }
        //        #endregion 
        //        message.Content = content;
        //        ContentManager.HandleContent(connectionStringeNameInAppConfig, message, service, subscriber, messagesTemplate, imiChargeCodes);
        //    }
        //    return isSucceeded;
        //}
    }
}

public enum CampaignStatus
{
    Deactive = 0,
    Active = 1,
    Suspend = 2
}


