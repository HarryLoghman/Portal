using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SharedLibrary
{
    public class Security
    {
        public static string fnc_tpsRatePassed(HttpContext context, Dictionary<string, string> requestParams, string destinationIP, string methodName)
        {
            return fnc_tpsRatePassed(context, requestParams, destinationIP, methodName, DateTime.Now);
        }
        public static string fnc_tpsRatePassed(HttpContext context, Dictionary<string, string> requestParams, string destinationIP, string methodName
            , DateTime regdate)
        {

            string srcIP = (context != null ? context.Request.UserHostAddress : null);
            //if (//string.IsNullOrEmpty(srcIP)|| 
            //    srcIP == "127.0.0.1" || HelpfulFunctions.fnc_getLocalServers().Any(o => o.IP == "http://" + srcIP))
            //    //local request is ok
            //    return null;

            string requestParamsStr = null;
            if (requestParams != null)
                requestParamsStr = string.Join(";", requestParams.Select(x => x.Key + ":" + x.Value).ToArray());
            //SharedVariables.logs.Error(requestParamsStr);
            using (var portal = new Models.PortalEntities())
            {
                //SharedVariables.logs.Error(string.IsNullOrEmpty(requestParamsStr) ? null : requestParamsStr.Substring(0, Math.Min(requestParamsStr.Length, 200)));
                var requestLog = new Models.RequestsLog();
                requestLog.destinationIP = destinationIP;
                requestLog.methodName = methodName;
                requestLog.regDate = regdate;
                requestLog.requestParams = string.IsNullOrEmpty(requestParamsStr) ? null : requestParamsStr.Substring(0, Math.Min(requestParamsStr.Length, 200));
                requestLog.sourceIP = srcIP;
                requestLog.isProcessed = null;
                requestLog.description = null;
                requestLog.x_forwarder = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];


                if (requestParams != null)
                {
                    var param = requestParams.FirstOrDefault(o => !string.IsNullOrEmpty(o.Value) && (o.Value.Contains(";") || o.Value.Contains(":")));
                    if (!param.Equals(default(KeyValuePair<string, string>)))
                    {
                        requestLog.isProcessed = false;
                        requestLog.description = param.Key + " has invalid value (':' or ';')" + param.Value;

                    }
                }
                portal.RequestsLogs.Add(requestLog);
                portal.SaveChanges();
                if (requestLog.isProcessed.HasValue && !requestLog.isProcessed.Value)
                {
                    return requestLog.description;
                }

                System.Data.Entity.Core.Objects.ObjectParameter action = new System.Data.Entity.Core.Objects.ObjectParameter("action", typeof(int));
                System.Data.Entity.Core.Objects.ObjectParameter actionMessage = new System.Data.Entity.Core.Objects.ObjectParameter("actionMessage", typeof(string));
                var serverTps = portal.sp_RequestsRulesChecker(requestLog.Id, regdate, srcIP, requestParamsStr, destinationIP, methodName
                    , action, actionMessage);

                if (action.Value == null || action.Value == DBNull.Value)
                {
                    return null;
                }
                else
                {
                    string returnValue = (actionMessage == null ? "TCL Rate has been passed " : actionMessage.Value.ToString());
                    SharedVariables.logs.Error("TCL Rate has been exceeded " + returnValue);
                    if (action.Value.ToString() == "0")
                    {
                        SharedVariables.logs.Error("TCL Rate has been exceeded:Request is rejected" + returnValue);
                        return "TCL Rate has been passed";
                    }
                    else if (action.Value.ToString() == "1")
                    {
                        //only notify
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, "Notif," + returnValue);
                        return null;
                    }
                    else if (action.Value.ToString() == "2")
                    {
                        SharedVariables.logs.Error("TCL Rate has been exceeded:Request is rejected" + returnValue);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error,"Reject,"+ returnValue);
                        return "TPS Rate has been passed"; ;
                    }

                }
            }
            return null;
        }
    }
}
