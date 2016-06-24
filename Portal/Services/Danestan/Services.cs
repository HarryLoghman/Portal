using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Services.Danestan
{
    public class Services
    {
        public static IQueryable<Service> GetRpsServiceInfo(PortalEntities entity)
        {
            return entity.Services.Where(o => o.ServiceCode == "Danestan");
        }

        public static List<MessagesTemplate> GetServiceMessagesTemplate()
        {
            using (var entity = new DanestanEntities())
            {
                return entity.MessagesTemplates.ToList();
            }
        }

        public static bool CheckIsUserWantsToUnsubscribe(string content)
        {
            foreach (var offKeyword in ServiceOffKeywords())
            {
                if (content.Contains(offKeyword))
                    return true;
            }
            return false;
        }

        public static string[] ServiceOffKeywords()
        {
            var offContents = new string[]
            {
                "off",
                "of",
                "cancel",
                "stop",
                "laghv",
                "lagv",
                "khamoosh",
                "خاموش",
                "لغو",
                "کنسل"
            };
            return offContents;
        }
    }
}