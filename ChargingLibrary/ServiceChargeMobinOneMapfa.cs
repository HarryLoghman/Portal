using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;

namespace ChargingLibrary
{
    public class ServiceChargeMobinOneMapfa : ServiceCharge
    {
        string v_url;
       
        int isCampaignActive;
        public override string[] prp_wipeDescription
        {
            get
            {
                return new string[] { "1001", "1006", "1007","1012","1013", "1015", "1016", "1018", "1027", "12","-2"
        , "-30000", "3002", "3004", "-4", "4998","4999","5030","-999" , "Exception"
        , "The request channel timed out while waiting for a reply after [%]"/*starts with 'The request channel timed out while waiting for a reply after' */
        , "The underlying connection was closed: A connection that was expected to be kept alive was closed by the server[%]"/*starts with 'The underlying connection was closed: A connection that was expected to be kept alive was closed by the server'*/
        }; ;
            }
        }
        public Aggregator prp_aggregator { get; set; }
        public ServiceChargeMobinOneMapfa(int serviceId, int tpsService, int maxTries, int cycleNumber, int cyclePrice)
            : base(serviceId, tpsService, maxTries, cycleNumber, cyclePrice)
        {
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                this.prp_aggregator = portal.Aggregators.Where(o => o.Id == this.prp_service.AggregatorId).FirstOrDefault();
                if (this.prp_aggregator == null)
                    throw new Exception("There is no aggregator with id =" + this.prp_service.AggregatorId + " in " + this.prp_service.ServiceCode);
            }
            isCampaignActive = 0;
            using (var entity = new SharedServiceEntities("Shared" + this.prp_service.databaseName + "Entities"))
            {
                var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                if (campaign != null)
                    isCampaignActive = Convert.ToInt32(campaign.Value);
            }
            this.v_url = HelpfulFunctions.fnc_getServerURL(HelpfulFunctions.enumServers.mobinOneMapfa, HelpfulFunctions.enumServersActions.charge);
        }

        public override void sb_charge(ServiceHandler.SubscribersAndCharges subscriber, int installmentCycleNumber, int loopNo
            , int threadNumber, DateTime timeLoop, long installmentId = 0)
        {

            var message = this.ChooseSinglechargePrice(subscriber);
            if (message == null) return;

            ServiceHandler.SubscribersAndCharges localSubscriber = subscriber;
            int localInstallmentCycleNumber = installmentCycleNumber;
            int localLoopNo = loopNo;
            int localThreadNumber = threadNumber;
            DateTime localTimeLoop = timeLoop;

            long localInstallmentId = installmentId;

            System.Threading.Interlocked.Increment(ref ChargingController.v_taskCount);
            //object obj = new object();
            //lock (obj) { int t = chargeServices.v_taskCount; chargeServices.v_taskCount = t + 1; }
            Task tsk = new Task(() =>
            {
                this.sb_chargeMobinOneMapfa(localSubscriber, message, localInstallmentCycleNumber, localLoopNo, localThreadNumber, localTimeLoop
                , localInstallmentId);
            });

            tsk.Start();
            while (tsk.Status != TaskStatus.Running)
            {

            }
        }

        public void sb_chargeMobinOneMapfa(SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber, MessageObject message
         , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, long installmentId = 0)
        {
            DateTime timeStartChargeMtnSubscriber = DateTime.Now;
            Nullable<DateTime> timeBeforeSendMTNClient = null;

            string guidStr = Guid.NewGuid().ToString();

            var dateCreated = DateTime.Now;
            singleChargeRequest singleChargeReq = new singleChargeRequest();
            try
            {
                singleChargeReq.dateCreated = dateCreated;
                singleChargeReq.guidStr = guidStr;
                singleChargeReq.resultDescription = "";
                singleChargeReq.isSucceeded = false;
                singleChargeReq.installmentCycleNumber = installmentCycleNumber;
                singleChargeReq.installmentId = installmentId;
                singleChargeReq.internalServerError = true;
                singleChargeReq.loopNo = loopNo;
                singleChargeReq.mobileNumber = message.MobileNumber;
                singleChargeReq.payload = "";
                singleChargeReq.Price = message.Price;
                singleChargeReq.referenceCode = guidStr;
                singleChargeReq.threadNumber = threadNumber;
                singleChargeReq.timeAfterEntity = null;
                singleChargeReq.timeAfterReadStringClient = null;
                singleChargeReq.timeAfterSendMTNClient = null;
                singleChargeReq.timeAfterXML = null;
                singleChargeReq.timeBeforeHTTPClient = null;
                singleChargeReq.timeBeforeReadStringClient = null;
                singleChargeReq.timeBeforeSendMTNClient = timeBeforeSendMTNClient;
                singleChargeReq.timeLoop = timeLoop;
                singleChargeReq.timeStartChargeMtnSubscriber = timeStartChargeMtnSubscriber;
                singleChargeReq.timeStartProcessMtnInstallment = null;
                singleChargeReq.webStatus = WebExceptionStatus.UnknownError;
                singleChargeReq.url = this.v_url;

                var serivceId = this.prp_service.Id;
                var paridsShortCodes = SharedLibrary.ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                var aggregatorServiceId = paridsShortCodes.FirstOrDefault(o => o.Price == message.Price.Value).PardisServiceId;
                var username = this.prp_aggregator.AggregatorUsername;
                var password = this.prp_aggregator.AggregatorPassword;
                var aggregatorId = this.prp_service.AggregatorId;
                var channelType = (int)SharedLibrary.MessageHandler.MapfaChannels.SMS;
                var domain = "";
                if (aggregatorId.ToString() == "3")
                    domain = "pardis1";
                else
                    domain = "alladmin";
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                using (var client = new SharedLibrary.MobinOneMapfaChargingServiceReference.ChargingClient())
                {
                    var result = client.singleCharge(username, password, domain, channelType, mobileNumber, aggregatorServiceId);
                    singleChargeReq.timeAfterWhere = DateTime.Now;
                    if (result > 10000)
                        singleChargeReq.isSucceeded = true;
                    else
                        singleChargeReq.isSucceeded = false;

                    singleChargeReq.resultDescription = result.ToString();
                    singleChargeReq.internalServerError = false;
                    singleChargeReq.webStatus = WebExceptionStatus.Success;


                }
            }
            catch (System.Net.WebException ex)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in chargeMobinOneMapfa in webException: ", ex);

                singleChargeReq.webStatus = ex.Status;
                singleChargeReq.internalServerError = true;
                singleChargeReq.resultDescription = "";
                singleChargeReq.isSucceeded = false;
                try
                {
                    if (ex.Response != null)
                    {
                        using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            singleChargeReq.resultDescription = rd.ReadToEnd();
                        }
                        ex.Response.Close();
                    }
                    else singleChargeReq.resultDescription = "";
                }
                catch (Exception e1)
                {
                    Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetRequestStreamCallBack inner try in webException: ", e1);
                }

            }
            catch (Exception e)
            {
                //Program.logs.Error("Exception in MapfaStaticPriceSinglecharge: " + e);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in chargeMobinOneMapfa: ", e);
                singleChargeReq.webStatus = WebExceptionStatus.UnknownError;
                singleChargeReq.resultDescription = e.Message + "\r\n" + e.StackTrace;
                singleChargeReq.isSucceeded = false;
                singleChargeReq.internalServerError = true;
                singleChargeReq.isSucceeded = false;

                //singlecharge.Description = "Exception";
            }
            this.saveResponseToDB(singleChargeReq);
            if (!singleChargeReq.internalServerError)
            {
                this.afterSend(singleChargeReq);
            }
        }

        protected override void afterSend(singleChargeRequest chargeRequest)
        {
            if (isCampaignActive == 1 || isCampaignActive == 2)
            {
                try
                {
                    var serviceId = Convert.ToInt64(this.prp_service.Id);
                    var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(chargeRequest.mobileNumber, serviceId);
                    if (isInBlackList != true)
                    {
                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(chargeRequest.mobileNumber, serviceId);
                        if (sub != null)
                        {
                            if (sub.SpecialUniqueId != null)
                            {
                                var sha = SharedLibrary.Security.GetSha256Hash(sub.SpecialUniqueId + chargeRequest.mobileNumber);
                                var price = 0;
                                if (chargeRequest.isSucceeded == true)
                                    price = chargeRequest.Price.Value;
                                var result = SharedLibrary.UsefulWebApis.DanoopReferral(this.prp_service.referralUrl + (this.prp_service.referralUrl.EndsWith("/") ? "" : "/") + "platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, chargeRequest.mobileNumber, chargeRequest.Price, sha)).Result;
                                //if (serviceAdditionalInfo["serviceCode"] == "Phantom")
                                //    await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/phantom/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, message.MobileNumber, price, sha));
                                //else if (serviceAdditionalInfo["serviceCode"] == "Medio")
                                //    await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/medio/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, message.MobileNumber, price, sha));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Program.logs.Error("Exception in calling danoop charge service: " + e);
                }
            }
        }
    }
}
