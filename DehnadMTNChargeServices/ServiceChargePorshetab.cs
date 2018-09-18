using PorShetabLibrary;
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

namespace DehnadMTNChargeServices
{
    class ServiceChargePorshetab : ServiceCharge
    {
        int v_isCampaignActive = 0;

        public override int prp_maxPrice
        {
            get
            {
                return 500;
            }
        }

        public ServiceChargePorshetab(int serviceId, int tpsService, string aggregatorServiceId, int maxTries, int cycleNumber)
            : base( serviceId, tpsService, aggregatorServiceId, maxTries, cycleNumber)
        {

            using (var entity = new PorShetabLibrary.Models.PorShetabEntities())
            {
                var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                if (campaign != null)
                    v_isCampaignActive = Convert.ToInt32(campaign.Value);
            }
        }
     
       

        protected override void afterSend(singleChargeRequest chargeRequest)
        {
            if (v_isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive || v_isCampaignActive == (int)CampaignStatus.MatchActiveReferralSuspend)
            {
                try
                {
                    var sub = SharedLibrary.HandleSubscription.GetSubscriber(chargeRequest.mobileNumber, this.prp_serviceId);
                    if (sub != null)
                    {
                        if (sub.SpecialUniqueId != null)
                        {
                            var sha = SharedLibrary.Security.GetSha256Hash(sub.SpecialUniqueId + chargeRequest.mobileNumber);
                            var price = 0;
                            if (chargeRequest.isSucceeded == true)
                                price = chargeRequest.Price.Value;
                            //logs.Warn(";porshetab;processMTNInstallment3;" + mobileNumber);
                            SharedLibrary.UsefulWebApis.DanoopReferralWithWebRequest("http://79.175.164.52/porshetab/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, chargeRequest.mobileNumber, price, sha));
                            //logs.Warn(";porshetab;processMTNInstallment4;" + mobileNumber);
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
