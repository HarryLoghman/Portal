using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadFtpSyncAndChargingService.MCI
{
    class SyncSubscription
    {
        public void syncSubscription()
        {
            try
            {
                List<SharedLibrary.Models.vw_servicesServicesInfo> lst_entryServices;
                string[] arrServicesString;
                SharedLibrary.Models.vw_servicesServicesInfo entryService;
                //string errorType, errorDescription;
                string serviceCode;
                bool autoSync;
                int i;
                string mobileNumber;
                int syncedCount;
                int operatorPlan, mobileOperator;
                string notifDescription;

                //DateTime? serviceSyncLastTime;
                List<SharedLibrary.Models.sp_MCIFtpLastState_getAsync_Result> lstSyncSubs;
                if (string.IsNullOrEmpty(Properties.Settings.Default.SyncServices))
                    return;
                using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                {
                    #region which service to sync
                    lst_entryServices = entityPortal.vw_servicesServicesInfo.OrderBy(o => o.ServiceCode).ToList();
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
                        arrServicesString = new string[0];
                        var arrTemp = Properties.Settings.Default.SyncServices.Split(';');
                        bool allServices = false;
                        bool allServicesAutoSync = false;
                        if (arrTemp.Any(o => o.ToLower() == "All".ToLower()))
                        {
                            //SyncServices = all;all:autosync;achar:autosync
                            //all is prior to all:autosync
                            //achar autosync all other services notif only
                            allServices = true;
                            allServicesAutoSync = false;
                        }
                        else if (arrTemp.Any(o => o.ToLower() == "All:AutoSync".ToLower()))
                        {
                            //SyncServices = all:autosync;achar
                            //achar notif all other services sync
                            allServices = false;
                            allServicesAutoSync = true;
                        }
                        for (i = 0; i <= lst_entryServices.Count - 1; i++)
                        {
                            //SyncServices = all:autosync;achar
                            //achar notif all other services sync
                            if (arrTemp.Any(o => o.ToLower() == lst_entryServices[i].ServiceCode.ToLower()))
                            {
                                //we are checking achar
                                //SyncServices = achar;achar:autosync
                                //notif achar
                                Array.Resize(ref arrServicesString, arrServicesString.Length + 1);
                                arrServicesString[arrServicesString.Length - 1] = lst_entryServices[i].ServiceCode;
                            }
                            else if (arrTemp.Any(o => o.ToLower() == lst_entryServices[i].ServiceCode.ToLower() + ":AutoSync".ToLower()))
                            {
                                //we are checking achar
                                //SyncServices = all;achar:autosync
                                //sync achar and notif other services
                                Array.Resize(ref arrServicesString, arrServicesString.Length + 1);
                                arrServicesString[arrServicesString.Length - 1] = lst_entryServices[i].ServiceCode + ":AutoSync";
                            }
                            else if (allServices)
                            {
                                //we are checking aseman
                                //SyncServices = all;achar:autosync
                                //notif aseman
                                Array.Resize(ref arrServicesString, arrServicesString.Length + 1);
                                arrServicesString[arrServicesString.Length - 1] = lst_entryServices[i].ServiceCode;
                            }
                            else if (allServicesAutoSync)
                            {
                                //we are checking aseman
                                //SyncServices = all:autosync;achar
                                //sync aseman
                                Array.Resize(ref arrServicesString, arrServicesString.Length + 1);
                                arrServicesString[arrServicesString.Length - 1] = lst_entryServices[i].ServiceCode + ":AutoSync";
                            }
                        }
                    }
                    #endregion

                    var lst_entryOperatorPrefix = entityPortal.OperatorsPrefixs.ToList();


                    for (i = 0; i <= arrServicesString.Length - 1; i++)
                    {
                        syncedCount = 0;
                        serviceCode = arrServicesString[i].Split(':')[0];
                        entryService = lst_entryServices.Where(o => o.ServiceCode == serviceCode).FirstOrDefault();
                        if (entryService == null) continue;
                        //serviceSyncLastTime = this.fnc_getServiceLastSyncTime(entryService.ServiceCode, out errorType, out errorDescription);
                        //if (!string.IsNullOrEmpty(errorType))
                        //    return;



                        lstSyncSubs = entityPortal.sp_MCIFtpLastState_getAsync(entryService.Id
                            , DateTime.Now
                            , Properties.Settings.Default.SyncFtpOldItemsInMins
                            , Properties.Settings.Default.SyncFtpWaitTimeInMins
                            , Properties.Settings.Default.SyncDBWaitTimeInMins
                            , Properties.Settings.Default.SyncChargedTriedNDaysBefore).ToList();

                        if (i == 0)
                        {
                            notifDescription = this.fnc_getNotifString(true, lstSyncSubs);
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning
                                , "MCI Sync setting " + notifDescription);
                        }
                        if (lstSyncSubs.Count() > 0)
                        {
                            if (!arrServicesString[i].Contains(":") || string.IsNullOrEmpty(arrServicesString[i].Split(':')[1])
                            || arrServicesString[i].Split(':')[1].ToLower() != "AutoSync".ToLower())
                                autoSync = false;
                            else autoSync = true;

                           

                            notifDescription = this.fnc_getNotifString(false, lstSyncSubs);
                            if (autoSync)
                            {
                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning
                                    , "MCI Sync Detail -Service:" + entryService.ServiceCode + ":" + lstSyncSubs.Count() + " Subscribers are going to be synchronized"
                                     + notifDescription);
                                Program.logs.Info("DehnadFtpSyncAndChargingService:syncSubscription:" + entryService.ServiceCode + ":" + lstSyncSubs.Count() + " Subscribers are going to be synchronized"
                                     + notifDescription);
                                foreach (var syncSub in lstSyncSubs)
                                {
                                    if (!syncSub.datetime.HasValue) continue;
                                    mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(syncSub.mobileNumber);
                                    SharedLibrary.MessageHandler.GetSubscriberOperatorInfo(mobileNumber, out mobileOperator, out operatorPlan, lst_entryOperatorPrefix);
                                    if (mobileNumber != "Invalid Mobile Number")
                                    {
                                        if (syncSub.Action.ToLower() == "SyncFtpDifference".ToLower()
                                            || syncSub.Action.ToLower() == "SyncFtpAddition".ToLower())
                                        {
                                            if (syncSub.event_type == "1.2")
                                            {
                                                if (SharedLibrary.SubscriptionFtpHandler.UnsubscribeFtp(mobileNumber
                                                      , syncSub.datetime.Value, syncSub.keyword, mobileOperator, operatorPlan, entryService))
                                                {
                                                    syncedCount++;
                                                }
                                            }
                                            else if (syncSub.event_type == "1.1")
                                            {
                                                if (SharedLibrary.SubscriptionFtpHandler.SubscribeFtp(mobileNumber
                                                    , syncSub.datetime.Value, syncSub.keyword, mobileOperator, operatorPlan, entryService))
                                                {
                                                    syncedCount++;
                                                }
                                            }
                                        }
                                        else if (syncSub.Action.ToLower() == "SyncChargeTryActivation".ToLower()
                                            || syncSub.Action.ToLower() == "SyncChargeTryAddition".ToLower())
                                        {
                                            if (syncSub.event_type == "1.5")
                                            {
                                                if (SharedLibrary.SubscriptionFtpHandler.SubscribeFtp(mobileNumber
                                                    , syncSub.datetime.Value, syncSub.Action, mobileOperator, operatorPlan, entryService))
                                                {
                                                    syncedCount++;
                                                }
                                            }
                                        }


                                    }
                                    //if (!this.fnc_setServiceLastSyncTime(entryService.ServiceCode, syncTime, out errorType, out errorDescription))
                                    //{
                                    //    return;
                                    //}
                                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "DehnadFtpSyncAndChargingService:syncSubscription:" + entryService.ServiceCode + ":" + lstSyncSubs.Count() + " Subscribers have been synchronized");
                                    Program.logs.Info("DehnadFtpSyncAndChargingService:syncSubscription:" + entryService.ServiceCode + ":" + syncedCount + " Subscribers have been synchronized");
                                }
                            }
                            else
                            {
                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning
                                    , "MCI Sync Detail -Service:" + entryService.ServiceCode + ":There are " + lstSyncSubs.Count() + " Subscribers need to be synchronized. Parameters "
                                   + notifDescription);
                                Program.logs.Info("DehnadFtpSyncAndChargingService: syncSubscription:" + entryService.ServiceCode + ":There are " + lstSyncSubs.Count() + " Subscribers need to be synchronized.Parameters "
                                   + notifDescription);
                            }

                        }

                    }

                }
            }
            catch (Exception e)
            {
                //SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "DehnadFtpSyncAndChargingService:syncSubscription:" + e.Message);
                Program.logs.Error("DehnadFtpSyncAndChargingService:syncSubscription:", e);
            }
        }

        private string fnc_getNotifString(bool settingParameters, List<SharedLibrary.Models.sp_MCIFtpLastState_getAsync_Result> lstSyncSubs)
        {
            string str = "";
            if (!settingParameters)
                str = "\n SyncFtpDifference = " + lstSyncSubs.Count(o => o.Action.ToLower() == "syncftpdifference").ToString()
                            + "(Need to be activated = " + lstSyncSubs.Count(o => o.Action.ToLower() == "syncftpdifference" && o.event_type == "1.1").ToString()
                            + " Need to be deactivated = " + lstSyncSubs.Count(o => o.Action.ToLower() == "syncftpdifference" && o.event_type == "1.2").ToString() + ")"
                            + "\n SyncFtpAddition = " + lstSyncSubs.Count(o => o.Action.ToLower() == "syncftpaddition").ToString()
                            + "\n SyncChargeTryActivation = " + lstSyncSubs.Count(o => o.Action.ToLower() == "syncchargetryactivation").ToString()
                            + "\n SyncChargeTryAddition = " + lstSyncSubs.Count(o => o.Action.ToLower() == "syncchargetryaddition").ToString();
            else
                str = "\n ChargeTryNDaysBefore = " + Properties.Settings.Default.SyncChargedTriedNDaysBefore.ToString()
                        + "\n DBWaitTimeInMins = " + Properties.Settings.Default.SyncDBWaitTimeInMins.ToString()
                        + "\n FtpOldItemsInMins = " + Properties.Settings.Default.SyncFtpOldItemsInMins.ToString()
                        + "\n FtpWaitTime = " + Properties.Settings.Default.SyncFtpWaitTimeInMins.ToString();
            return str;
        }
        //private DateTime? fnc_getServiceLastSyncTime(string serviceCode, out string errorType, out string errorDescription)
        //{
        //    errorType = "";
        //    errorDescription = "";
        //    if (string.IsNullOrEmpty(serviceCode))
        //    {
        //        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpCharging:syncSubscription:Servicecode is not specified");
        //        throw new Exception("MCIFtpCharging:getServiceLastSyncTime:Servicecode is not specified");
        //    }

        //    try
        //    {
        //        Dictionary<string, DateTime> dic = this.fnc_readSyncLastTimesFile(out errorType, out errorDescription);
        //        if (!string.IsNullOrEmpty(errorType)) return null;

        //        if (dic == null) return null;
        //        return dic.Where(o => o.Key == serviceCode).Select(o => o.Value).FirstOrDefault();
        //    }
        //    catch (Exception e)
        //    {
        //        errorType = "Exception is occurred";
        //        errorDescription = e.Message;
        //        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpCharging:getServiceLastSyncTime:" + errorDescription);
        //        Program.logs.Error("MCIFtpCharging:getServiceLastSyncTime:", e);
        //    }
        //    return null;
        //}

        //private Dictionary<string, DateTime> fnc_readSyncLastTimesFile(out string errorType, out string errorDescription)
        //{
        //    errorType = "";
        //    errorDescription = "";
        //    if (!File.Exists("SyncLastTimes.txt"))
        //    {
        //        var fl = File.Create("SyncLastTimes.txt");
        //        fl.Close();
        //    }

        //    try
        //    {
        //        string syncLastTimes = File.ReadAllText("SyncLastTimes.txt");
        //        if (string.IsNullOrEmpty(syncLastTimes))
        //            return null;
        //        Dictionary<string, DateTime> dic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(syncLastTimes);
        //        return dic;
        //    }
        //    catch (Exception e)
        //    {
        //        errorType = "Exception is occurred";
        //        errorDescription = e.Message;
        //        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpCharging:fnc_readSyncLastTimesFile:" + errorDescription);
        //        Program.logs.Error("MCIFtpCharging:fnc_readSyncLastTimesFile:", e);
        //    }
        //    return null;
        //}


        //private bool fnc_setServiceLastSyncTime(string serviceCode, DateTime syncLastTime, out string errorType, out string errorDescription)
        //{
        //    errorType = "";
        //    errorDescription = "";
        //    if (string.IsNullOrEmpty(serviceCode))
        //    {
        //        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpCharging:syncSubscription:Servicecode is not specified");
        //        throw new Exception("MCIFtpCharging:getServiceLastSyncTime:Servicecode is not specified");
        //    }

        //    try
        //    {
        //        Dictionary<string, DateTime> dic = this.fnc_readSyncLastTimesFile(out errorType, out errorDescription);
        //        if (!string.IsNullOrEmpty(errorType)) return false;

        //        if (dic == null)
        //        {
        //            dic = new Dictionary<string, DateTime>();
        //            dic.Add(serviceCode, syncLastTime);
        //        }
        //        else
        //        {
        //            var serviceLastTime = dic.Where(o => o.Key == serviceCode).FirstOrDefault();
        //            if (!dic.Any(o => o.Key == serviceCode))
        //            {
        //                dic.Add(serviceCode, syncLastTime);
        //            }
        //            else
        //            {
        //                dic[serviceCode] = syncLastTime;
        //            }

        //        }
        //        if (this.fnc_writeSyncLastTimesFile(dic, out errorType, out errorDescription))
        //            return true;

        //    }
        //    catch (Exception e)
        //    {
        //        errorType = "Exception is occurred";
        //        errorDescription = e.Message;
        //        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpCharging:fnc_setServiceLastSyncTime:" + errorDescription);
        //        Program.logs.Error("MCIFtpCharging:fnc_setServiceLastSyncTime:", e);
        //    }
        //    return false;
        //}

        //private bool fnc_writeSyncLastTimesFile(Dictionary<string, DateTime> dic, out string errorType, out string errorDescription)
        //{
        //    errorType = "";
        //    errorDescription = "";

        //    try
        //    {
        //        string content = Newtonsoft.Json.JsonConvert.SerializeObject(dic);
        //        File.WriteAllText("SyncLastTimes.txt", content);
        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        errorType = "Exception is occurred";
        //        errorDescription = e.Message;
        //        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpCharging:fnc_writeSyncLastTimesFile:" + errorDescription);
        //        Program.logs.Error("MCIFtpCharging:fnc_writeSyncLastTimesFile:", e);
        //    }
        //    return false;
        //}
    }
}
