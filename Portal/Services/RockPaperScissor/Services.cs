using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Services.RockPaperScissor
{
    public class Services
    {
        public static IQueryable<Service> GetRpsServiceInfo(PortalEntities entity)
        {
            return entity.Services.Include("RPS_ServiceAdditionalInfo").Where(o => o.ServiceCode == "RPS");
        }

        public static List<MessagesTemplate> GetServiceMessagesTemplate()
        {
            using (var entity = new RockPaperScissorEntities())
                return entity.MessagesTemplates.ToList();
        }
    }
}