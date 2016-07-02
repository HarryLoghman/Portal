using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Shared
{
    public class ServiceHandler
    {
        public static Dictionary<string, string> GetAdditionalServiceInfoForSendingMessage(string serviceCode, string aggregatorName)
        {
            var sendInfoDic = new Dictionary<string, string>();
            using (var entity = new PortalEntities())
            {
                var aggregatorInfo = entity.Aggregators.FirstOrDefault(o => o.AggregatorName == aggregatorName);
                sendInfoDic["username"] = aggregatorInfo.AggregatorUsername;
                sendInfoDic["password"] = aggregatorInfo.AggregatorPassword;
                var service = entity.Services.FirstOrDefault(o => o.ServiceCode == serviceCode);
                var serviceInfo = entity.ServiceInfoes.FirstOrDefault(o => o.AggregatorId == aggregatorInfo.Id && o.ServiceId == service.Id);
                sendInfoDic["shortCode"] = serviceInfo.ShortCode;
                sendInfoDic["aggregatorServiceId"] = serviceInfo.AggregatorServiceId.ToString();
            }
            return sendInfoDic;
        }
    }
}