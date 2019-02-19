using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadMCIFtpChargingService
{
    class SyncCharged
    {
        public void activateChargedSubscribers()
        {
            try
            {
                List<SharedLibrary.Models.vw_servicesServicesInfo> lst_entryServices;
                string[] arrServicesString;
                SharedLibrary.Models.vw_servicesServicesInfo entryService;
                string serviceCode;
                bool autoSync;
                int i;
                string parametersStr;
                string mobileNumber;
                int syncedCount;
                int operatorPlan, mobileOperator;
                DateTime syncNotChargedFromDate;
                List<SharedLibrary.Models.sp_MCISubsLastStateFtpFiles_getNotCharged_Result> lstSyncSubs;
                if (string.IsNullOrEmpty(Properties.Settings.Default.SyncServices))
                    return;
                using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                {
                    #region which service to sync
                    lst_entryServices = entityPortal.vw_servicesServicesInfo.ToList();
                    if (Properties.Settings.Default.SyncServices.ToLower() == "All".ToLower())
                    {
                        arrServicesString = lst_entryServices.Select(o => o.ServiceCode).ToArray();

                    }
                    else if (Properties.Settings.Default.SyncServices.ToLower() == "All:AutoSync".ToLower())
                    {
                        arrServicesString = lst_entryServices.Select(o => o.ServiceCode + ":AutoSync").ToArray();
                    }
                    else
                    {
                        arrServicesString = Properties.Settings.Default.SyncServices.Split(';');
                    }
                    #endregion

                    var lst_entryOperatorPrefix = entityPortal.OperatorsPrefixs.ToList();


                    for (i = 0; i <= arrServicesString.Length - 1; i++)
                    {
                        syncedCount = 0;
                        serviceCode = arrServicesString[i].Split(':')[0];
                        entryService = lst_entryServices.Where(o => o.ServiceCode == serviceCode).FirstOrDefault();
                        if (entryService == null) continue;

                        syncNotChargedFromDate = DateTime.Now.Date.AddDays(-1 * Properties.Settings.Default.DeactiveNotChargedCheckNDaysBefore);
                        parametersStr = entryService.Id.ToString()
                                    + "," + syncNotChargedFromDate.ToString("yyyy-MM-dd HH:mm:ss.fff")
                                    + "," + DateTime.Now.AddMinutes(-1 * Properties.Settings.Default.DeactiveNotChargedForNMinsActivationDateBefore).ToString("yyyy-MM-dd HH:mm:ss.fff");

                        lstSyncSubs = entityPortal.sp_MCISubsLastStateFtpFiles_getNotCharged(entryService.Id
                            , syncNotChargedFromDate
                            , DateTime.Now.AddMinutes(-1 * Properties.Settings.Default.DeactiveNotChargedForNMinsActivationDateBefore)).ToList();

                        if (lstSyncSubs.Count() > 0)
                        {
                            if (!arrServicesString[i].Contains(":") || string.IsNullOrEmpty(arrServicesString[i].Split(':')[1])
                            || arrServicesString[i].Split(':')[1].ToLower() != "AutoSync".ToLower())
                                autoSync = false;
                            else autoSync = true;

                            if (autoSync)
                            {
                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, "DehnadMCIFtpChargingService:syncSubscription:" + entryService.ServiceCode + ":" + lstSyncSubs.Count() + " Subscribers are going to be synchronized");
                                Program.logs.Info("DehnadMCIFtpChargingService:syncNotCharged:" + entryService.ServiceCode + ":" + lstSyncSubs.Count() + " Subscribers are going to be synchronized");
                                foreach (var syncSub in lstSyncSubs)
                                {

                                    mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(syncSub.MobileNumber);
                                    SharedLibrary.MessageHandler.GetSubscriberOperatorInfo(mobileNumber, out mobileOperator, out operatorPlan, lst_entryOperatorPrefix);
                                    if (mobileNumber != "Invalid Mobile Number")
                                    {
                                        if (SharedLibrary.HandleSubscriptionFtp.UnsubscribeFtp(mobileNumber
                                                  , DateTime.Now, "NotCharged" + "From" + syncNotChargedFromDate.ToString("yyyy-MM-dd HH:mm:ss.fff"), mobileOperator, operatorPlan, entryService))
                                        {
                                            syncedCount++;
                                        }


                                    }


                                }

                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "DehnadMCIFtpChargingService:syncNotCharged:" + entryService.ServiceCode + ":" + lstSyncSubs.Count() + " Subscribers have been deactivated due to not charging from " + syncNotChargedFromDate.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                Program.logs.Info("DehnadMCIFtpChargingService:syncNotCharged:" + entryService.ServiceCode + ":" + syncedCount + " Subscribers have been deactivated due to not charging from " + syncNotChargedFromDate.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                            }
                            else
                            {
                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, "DehnadMCIFtpChargingService:syncNotCharged:" + entryService.ServiceCode + ":There are " + lstSyncSubs.Count() + " Subscribers have not been charged from " + syncNotChargedFromDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + " and need to be deactivated. Parameters "
                                   + parametersStr);
                                Program.logs.Info("DehnadMCIFtpChargingService:syncNotCharged:" + entryService.ServiceCode + ":There are " + lstSyncSubs.Count() + " Subscribers have not been charged from " + syncNotChargedFromDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + " and need to be deactivated.Parameters "
                                   + parametersStr);
                            }

                        }

                    }

                }
            }
            catch (Exception e)
            {
                //SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "DehnadMCIFtpChargingService:syncSubscription:" + e.Message);
                Program.logs.Error("DehnadMCIFtpChargingService:syncSubscription:", e);
            }
        }


    }

}
