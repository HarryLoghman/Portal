using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Portal.Models;
using Portal.Services.RockPaperScissor.Model;

namespace Portal.Services.RockPaperScissor
{
    public class RpsServices
    {
        public static IQueryable<Service> GetRpsServiceInfo(PortalEntities entity)
        {
            return entity.Services.Include("RPS_ServiceAdditionalInfo").Where(o => o.ServiceCode == "RPS");
        }
    }
}