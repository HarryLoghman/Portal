using Audit.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal
{
    public class AuditCustomDataProvider : AuditDataProvider
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public override object InsertEvent(AuditEvent auditEvent)
        {
            try
            {
                using (var entity = new SharedLibrary.Models.PortalEntities())
                {
                    var newAudit = new SharedLibrary.Models.Audit();
                    newAudit.DateCreated = DateTime.Now;
                    newAudit.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    newAudit.Action = auditEvent.Environment.CallingMethodName;
                    if (SharedLibrary.HelpfulFunctions.IsPropertyExistInDynamicObject(auditEvent, "Action"))
                    {
                        dynamic dynamicAudit = auditEvent;
                        newAudit.UserName = dynamicAudit.Action.UserName;
                        newAudit.Ip = dynamicAudit.Action.IpAddress;
                        newAudit.Action = dynamicAudit.Action.RequestUrl;
                    }
                    newAudit.Description = auditEvent.ToJson().ToString();
                    entity.Audits.Add(newAudit);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in InsertEvent: ", e);
            }
            return null;
        }
    }
}