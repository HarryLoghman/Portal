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

        public ServiceChargeMTNPorShetab(int serviceId, int tpsService, int maxTries, int cycleNumber, int cyclePrice)
            : base(serviceId, tpsService, maxTries, cycleNumber, cyclePrice)
        {

            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Shared" + this.prp_service.ServiceCode + "Entities"))
            {
                var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                if (campaign != null)
                    v_isCampaignActive = Convert.ToInt32(campaign.Value);
            }
        }



        protected override void afterSend(singleChargeRequest chargeRequest)
        {
            try
            {
                if (v_isCampaignActive == (int)PorShetabLibrary.CampaignStatus.MatchActiveReferralActive || v_isCampaignActive == (int)PorShetabLibrary.CampaignStatus.MatchActiveReferralSuspend)
                {

                    var sub = SharedLibrary.HandleSubscription.GetSubscriber(chargeRequest.mobileNumber, this.prp_service.Id);
                    if (sub != null)
                    {
                        if (sub.SpecialUniqueId != null)
                        {
                            var sha = SharedLibrary.Security.GetSha256Hash(sub.SpecialUniqueId + chargeRequest.mobileNumber);
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
