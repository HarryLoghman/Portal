using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace DehnadMTNChargeServices
{
    internal static class Program
    {
        internal static log4net.ILog logs;
        internal static string v_cnnStr = "Data source =.; initial catalog = portal; integrated security = True; max pool size=4000; multipleactiveresultsets=True;connection timeout=180 ;";
        internal static string v_urlNotification = "http://84.22.111.11/notif/n6.php";
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            sb_setLoggerQuarterNumber();
            if (Environment.UserInteractive)
            {
                ServiceMTN service1 = new ServiceMTN();
                service1.StartDebugging(null);
            }
            else
            {
                // Put the body of your old Main method here.  
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new ServiceMTN()
                };
                ServiceBase.Run(ServicesToRun);
            }

        }

        internal static void sb_setLoggerQuarterNumber()
        {
            DateTime now = DateTime.Now;
            int quarterNumber = 1;
            if (now.TimeOfDay >= TimeSpan.Parse("00:00:00.000") && now.TimeOfDay < TimeSpan.Parse("06:00:00"))
            {
                quarterNumber = 1;
            }
            else if (now.TimeOfDay >= TimeSpan.Parse("06:00:00.000") && now.TimeOfDay < TimeSpan.Parse("12:00:00"))
            {
                quarterNumber = 2;
            }
            else if (now.TimeOfDay >= TimeSpan.Parse("12:00:00.000") && now.TimeOfDay < TimeSpan.Parse("18:00:00"))
            {
                quarterNumber = 3;
            }
            else if (now.TimeOfDay >= TimeSpan.Parse("18:00:00.000") && now.TimeOfDay <= TimeSpan.Parse("23:59:59"))
            {
                quarterNumber = 4;
            }
            try
            {
                object obj = log4net.GlobalContext.Properties["QuarterNumber"];
                if (obj != null && !string.IsNullOrEmpty(obj.ToString()) && obj.ToString() == quarterNumber.ToString())
                {
                    return;
                }
            }
            catch
            {

            }
            log4net.GlobalContext.Properties["QuarterNumber"] = quarterNumber.ToString();
            log4net.Config.XmlConfigurator.Configure();
            logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        }
        internal static void sb_sendNotification(StandardEventLevel level, string message)
        {
            sb_sendNotification(v_urlNotification, level, message);
        }

        internal static void sb_sendNotification(string url, StandardEventLevel level, string message)
        {
            return;
            try
            {
                string icon = "";
                if (level == StandardEventLevel.Critical)
                {
                    icon = "🆘";
                }
                else if (level == StandardEventLevel.Error)
                {
                    icon = "🔴";
                }
                else if (level == StandardEventLevel.Informational)
                {
                    icon = "✅";
                }
                else if (level == StandardEventLevel.Warning)
                {
                    icon = "⚠";
                }
                Uri uri = new Uri(url + "?msg=" + icon + message, UriKind.Absolute);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "Get";
                webRequest.GetResponse();
            }
            catch (Exception e)
            {
                Program.logs.Error(e);
            }
        }
    }
}
