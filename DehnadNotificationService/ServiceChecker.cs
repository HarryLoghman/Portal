using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DehnadNotificationService
{
    class ServiceChecker
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void Job()
        {
            try
            {
                var services = GetDehnadServicesStatus();
                var stoppedServices = services.Where(o => !o.Contains("Running")).ToList();
                if(stoppedServices.Count > 0)
                {
                    Thread.Sleep(5 * 60 * 1000);
                    var serv = GetDehnadServicesStatus();
                    var faultyServices = serv.Where(o => !o.Contains("Running")).ToList();
                    if (faultyServices.Count > 0)
                    {
                        var message = "";
                        foreach (var item in faultyServices)
                        {
                            message += item + Environment.NewLine;
                        }
                        DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in Job: " + e);
            }
        }

        public static List<string> GetDehnadServicesStatus()
        {
            var result = new List<string>();
            try
            {
                var services = ServiceController.GetServices().Where(o => o.ServiceName.Contains("Dehnad") || o.ServiceName == "SQLSERVERAGENT" || o.ServiceName == "MSSQLSERVER").ToList();
                foreach (ServiceController service in services)
                {
                    try
                    {
                        ServiceControllerStatus status;

                        service.Refresh();
                        status = service.Status;

                        switch (status)
                        {
                            case ServiceControllerStatus.Running:
                                result.Add(service.ServiceName + "=" + "Running");
                                break;
                            case ServiceControllerStatus.Stopped:
                                result.Add(service.ServiceName + "=" + "Stopped");
                                break;
                            case ServiceControllerStatus.Paused:
                                result.Add(service.ServiceName + "=" + "Paused");
                                break;
                            case ServiceControllerStatus.StopPending:
                                result.Add(service.ServiceName + "=" + "Stopping");
                                break;
                            case ServiceControllerStatus.StartPending:
                                result.Add(service.ServiceName + "=" + "Starting");
                                break;
                            default:
                                result.Add(service.ServiceName + "=" + "Status Changing");
                                break;
                        }
                    }
                    catch (Win32Exception e)
                    {
                        logs.Error("Exception in  ServiceChecker Job : ", e);
                    }
                    service.Dispose();
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in GetDehnadServicesStatus: " + e);
            }
            return result;
        }
    }
}
