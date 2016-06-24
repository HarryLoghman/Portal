using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Services.Danestan
{
    public class Subscribers
    {
        public static Subscriber GetSubscriber(string mobileNumber, long serviceId)
        {
            using(var entity = new PortalEntities())
                return entity.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId).FirstOrDefault();
        }
    }
}