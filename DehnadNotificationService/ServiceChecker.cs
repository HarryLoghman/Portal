using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DehnadNotificationService
{
    public class ServiceChecker
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void Job()
        {
            try
            {
                var services = GetDehnadServicesStatus();
                var stoppedServices = services.Where(o => !o.Contains("Running")).ToList();
                if (stoppedServices.Count > 0)
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
                        service.Refresh();
                        switch (service.Status)
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
                    service.Close();
                    if (service != null)
                        service.Dispose();
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in GetDehnadServicesStatus: " + e);
            }
            return result;
        }

        public static void KillProcess(string processName)
        {
            try
            {
                if (processName.ToLower() == "mssqlserver")
                    processName = "sqlservr";
                else if (processName.ToLower() == "sqlserveragent")
                    processName = "sqlagent";

                foreach (Process proc in Process.GetProcessesByName(processName))
                {
                    proc.Kill();
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in KillProcess " + processName + ": " + e);
            }
        }

        public static void MoQueueCheck()
        {
            try
            {
                using (var entity = new SharedLibrary.Models.PortalEntities())
                {
                    var message = "";
                    bool? isOverQueued = SharedLibrary.Notify.MoQueueCheck(entity);
                    if (isOverQueued == null)
                    {
                        message = "<b>Exception in MoQueueCheck</b>";
                        DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                    }
                    else
                    {
                        if (isOverQueued == true)
                        {
                            message = "<b>OverQueue Mo</b>";
                            DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in MoQueueCheck in notification:", e);
                var message = "<b>Exception in MoQueueCheck</b>";
                DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
            }
        }

        public static void StopService(string serviceName)
        {
            try
            {
                var theController = new System.ServiceProcess.ServiceController(serviceName);
                if (!theController.Status.Equals(ServiceControllerStatus.Stopped))
                {
                    theController.Stop();
                    theController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    if (!theController.Status.Equals(ServiceControllerStatus.Stopped))
                    {
                        var processName = serviceName;
                        if (serviceName.ToLower() == "mssqlserver")
                            processName = "sqlservr";
                        else if (serviceName.ToLower() == "sqlserveragent")
                            processName = "sqlagent";

                        KillProcess(processName);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in StopService " + serviceName + ": " + e);
            }
        }
        public static void StartService(string serviceName)
        {
            try
            {
                var theController = new System.ServiceProcess.ServiceController(serviceName);
                if (!theController.Status.Equals(ServiceControllerStatus.Running))
                {
                    theController.Start();
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in StartService " + serviceName + ": " + e);
            }
        }

        public static void RestartService(string serviceName)
        {
            try
            {
                var theController = new System.ServiceProcess.ServiceController(serviceName);
                if (!theController.Status.Equals(ServiceControllerStatus.Stopped))
                {
                    theController.Stop();
                    theController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    if (!theController.Status.Equals(ServiceControllerStatus.Stopped))
                    {
                        var processName = serviceName;
                        if (serviceName.ToLower() == "mssqlserver")
                            processName = "sqlservr";
                        else if (serviceName.ToLower() == "sqlserveragent")
                            processName = "sqlagent";

                        KillProcess(processName);
                    }
                }
                if (!theController.Status.Equals(ServiceControllerStatus.Running))
                {
                    theController.Start();
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in RestartService " + serviceName + ": " + e);
            }
        }
    }
}
