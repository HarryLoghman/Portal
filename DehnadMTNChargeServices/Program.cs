using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace DehnadMTNChargeServices
{
    internal static class Program
    {
        internal static log4net.ILog logs;
      
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            XmlConfigurator.Configure();
            logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
    }
}
