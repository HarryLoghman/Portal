using SharedLibrary;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ChargingLibrary
{
    public class ServiceChargeMTNPorShetab : ServiceChargeMTN
    {
        int v_isCampaignActive = 0;
        int v_isMatchActive = 0;

        public ServiceChargeMTNPorShetab(int serviceId, int tpsService, int maxTries, int cycleNumber, int cyclePrice)
            : base(serviceId, tpsService, maxTries, cycleNumber, cyclePrice)
        {

            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(this.prp_service.ServiceCode))
            {
                var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                if (campaign != null)
                    v_isCampaignActive = Convert.ToInt32(campaign.Value);
                var match = entity.Settings.FirstOrDefault(o => o.Name == "match");
                if (match != null)
                    v_isMatchActive = Convert.ToInt32(match.Value);
            }
        }



        protected override void afterSend(singleChargeRequest chargeRequest)
        {
            try
            {
                if (v_isMatchActive == (int)CampaignStatus.Active && (v_isCampaignActive == (int)CampaignStatus.Deactive || v_isCampaignActive == (int)CampaignStatus.Suspend))
                {

                    var sub = SharedLibrary.SubscriptionHandler.GetSubscriber(chargeRequest.mobileNumber, this.prp_service.Id);
                    if (sub != null)
                    {
                        if (sub.SpecialUniqueId != null)
                        {
                            var sha = SharedLibrary.Encrypt.GetSha256Hash(sub.SpecialUniqueId + chargeRequest.mobileNumber);
                            var price = 0;
                            if (chargeRequest.isSucceeded == true)
                                price = chargeRequest.Price.Value;
                            //logs.Warn(";PorShetab;processMTNInstallment3;" + mobileNumber);
                            //SharedLibrary.UsefulWebApis.DanoopReferralWithWebRequest("/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, chargeRequest.mobileNumber, price, sha));
                            SharedLibrary.UsefulWebApis.DanoopReferralWithWebRequest(this.prp_service.referralUrl + (this.prp_service.referralUrl.EndsWith("/") ? "" : "/") + "platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, chargeRequest.mobileNumber, price, sha));

                            //logs.Warn(";PorShetab;processMTNInstallment4;" + mobileNumber);
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
