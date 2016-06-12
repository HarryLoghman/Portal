using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;
using Portal.Services.RockPaperScissor.Model;

namespace Portal.Services.RockPaperScissor
{
    public class RpsServices
    {
        public static IQueryable<ServiceWithAdditionalInfo> GetRpsServiceInfo(PortalEntities entity)
        {
            //var a = entity.Services.Include("RPS_ServiceAdditionalInfo").Where(o => o.ServiceCode == "RPS");
            
            var service = entity.Services.Join(entity.RPS_ServiceAdditionalInfo, s => s.Id,
                    sai => sai.ServiceId, (s, sai) => new { s, sai }).Where(
                        o => o.s.ServiceCode == "RPS").Select(o => new ServiceWithAdditionalInfo() { Service = o.s, RPS_ServiceAdditionalInfo = o.sai });
            return service;
        }
    }
}