﻿using System.ServiceProcess;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace DehnadAcharService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DehnadAcharService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
