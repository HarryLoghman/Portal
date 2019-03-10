using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DehnadMonitoringService
{
    public class MonitorWindowsServices
    {
        public static void MonitorServices()
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.WindowsServiceDisplayNameIncludeFilters))
            {
                return;
            }

            ServiceController[] allWindowsServices;
            allWindowsServices = ServiceController.GetServices();

            int i;
            bool checkService;

            string[] arrIncludeFilters = Properties.Settings.Default.WindowsServiceDisplayNameIncludeFilters.Split(';');
            string[] arrExcludeFilters = string.IsNullOrEmpty(Properties.Settings.Default.WindowsServiceDisplayNameExcludeFilters) ? new string[] { } : Properties.Settings.Default.WindowsServiceDisplayNameExcludeFilters.Split(';');

            foreach (var windowsService in allWindowsServices)
            {
                checkService = false;

                if (arrExcludeFilters.Any(o => o == windowsService.DisplayName))
                {
                    checkService = false;
                }
                else if (arrIncludeFilters.Any(o => o == windowsService.DisplayName))
                {
                    checkService = true;
                }
                else
                {
                    //check filters with regex
                    for (i = 0; i <= arrExcludeFilters.Length - 1; i++)
                    {
                        if(Regex.IsMatch(windowsService.DisplayName,arrExcludeFilters[i],RegexOptions.IgnoreCase))
                        {
                            checkService = false;
                            break;
                        }
                    }

                    for (i = 0; i <= arrIncludeFilters.Length - 1; i++)
                    {
                        if (Regex.IsMatch(windowsService.DisplayName, arrIncludeFilters[i], RegexOptions.IgnoreCase))
                        {
                            checkService = true;
                            break;
                        }
                    }

                }

                if (checkService)
                {
                    if (windowsService.Status != ServiceControllerStatus.Running)
                    {
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_Monitoring(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, windowsService.DisplayName + " is not running");
                    }
                }
            }

        }
    }
}
