using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace BulkExecuter
{
    class Program
    {
        internal static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            string exceptionStr;
            if (args == null || args.Length == 0)
            {
                exceptionStr = "BulkExecuter:Program:Main,BulkId is not specified as a first argument";
                logs.Error(exceptionStr);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                Environment.Exit(1);
                return;
            }
            int bulkId;
            if (!int.TryParse(args[0], out bulkId))
            {
                exceptionStr = "BulkExecuter:Program:Main,BulkId is not an integer number(bulkdId='" + args[0] + "')";
                logs.Error(exceptionStr);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                Environment.Exit(1);
                return;
            }
            using (var entityPortal = new SharedLibrary.Models.PortalEntities())
            {
                SharedLibrary.MessageHandler.BulkStatus bulkStatus;
                bool canStart = BulkController.fnc_canStartSendingList(bulkId, out exceptionStr, out bulkStatus);
                if(!canStart)
                {
                    exceptionStr = "BulkExecuter:Program:Main,"+ exceptionStr;
                    logs.Error(exceptionStr);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                    Environment.Exit(1);
                    return;
                }
            }
        }
    }
}
