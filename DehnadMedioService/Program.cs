using System;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.ServiceProcess;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace DehnadMedioService
{
    static class Program
    {
        internal static string v_urlNotification = "http://84.22.111.11/notif/n6.php";
        internal static log4net.ILog logs= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service()
            };
            ServiceBase.Run(ServicesToRun);
        }

        internal static void sb_sendNotification(StandardEventLevel level, string message)
        {
            sb_sendNotification(v_urlNotification, level, message);
        }

        internal static void sb_sendNotification(string url, StandardEventLevel level, string message)
        {
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
