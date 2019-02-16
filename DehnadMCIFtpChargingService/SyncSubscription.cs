using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadMCIFtpChargingService
{
    class SyncSubscription
    {
        public void syncSubscription()
        {
            try
            {
                List<SharedLibrary.Models.vw_servicesServicesInfo> lst_entryServices;
                if (string.IsNullOrEmpty(Properties.Settings.Default.SyncSubStateServices))
                    return;
                DateTime SyncStartTime;
                using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                {
                    
                    if (Properties.Settings.Default.SyncSubStateServices != "All")
                    {
                        var arrTemp = Properties.Settings.Default.SyncSubStateServices.Split(';');
                        lst_entryServices = entityPortal.vw_servicesServicesInfo.Where(o => arrTemp.Contains(o.ServiceCode)).ToList();
                    }
                    else
                    {
                        lst_entryServices = entityPortal.vw_servicesServicesInfo.ToList();
                    }
                    DateTime? startTime;
                    DateTime time;
                    string str;
                    int i;
                    string[] strServiceLastTimeArr;
                    List<SharedLibrary.Models.sp_MCISubsLastStateFtpFiles_getAsyncSubs_Result> lstSyncSubs;
                    foreach (var entryService in lst_entryServices)
                    {
                        
                        startTime = null;
                        if (!string.IsNullOrEmpty(Properties.Settings.Default.SyncServiceLastTime))
                        {
                            //Program.logs.Error("7324789273984728934");
                            Program.logs.Error(Properties.Settings.Default.SyncServiceLastTime);
                            str = Properties.Settings.Default.SyncServiceLastTime.Split(';').FirstOrDefault(o => o.Split('-')[0] == entryService.ServiceCode);
                            
                            if (DateTime.TryParse(str.Split('-')[1], out time))
                            {
                                startTime = time;
                            }
                        }
                        SyncStartTime = DateTime.Now;
                        
                        lstSyncSubs = entityPortal.sp_MCISubsLastStateFtpFiles_getAsyncSubs(entryService.Id
                            , startTime
                            , DateTime.Now.AddSeconds(-1 * int.Parse(Properties.Settings.Default.SyncNSecondsBefore))).ToList();
                        Program.logs.Error("sadasl;dk;alskd;laksd");
                        foreach (var syncSub in lstSyncSubs)
                        {
                            SharedLibrary.Models.ReceievedMessage entryReceievedMessage = new SharedLibrary.Models.ReceievedMessage();
                            entryReceievedMessage.Content = syncSub.keyword;
                            entryReceievedMessage.description = "ftpMethod-" + syncSub.filePath;
                            entryReceievedMessage.IsProcessed = false;
                            entryReceievedMessage.IsReceivedFromIntegratedPanel = (string.IsNullOrEmpty(syncSub.channel) || syncSub.channel.ToLower() != "TAJMI".ToLower() ? false : true);
                            entryReceievedMessage.LastRetryDate = null;
                            entryReceievedMessage.MessageId = syncSub.trans_id;
                            entryReceievedMessage.MobileNumber = syncSub.mobileNumber;
                            entryReceievedMessage.PersianReceivedTime = SharedLibrary.Date.GetPersianDate(syncSub.datetime);
                            entryReceievedMessage.ReceivedFrom = downloader.ServerFtpIP + "-FromFtp-" + (syncSub.event_type == "1.2" ? "Unsubscribe" : "Register");
                            entryReceievedMessage.ReceivedFromSource = 0;
                            entryReceievedMessage.ReceivedTime = syncSub.datetime.Value;
                            entryReceievedMessage.RetryCount = null;
                            entryReceievedMessage.ShortCode = syncSub.shortcode;

                            entityPortal.ReceievedMessages.Add(entryReceievedMessage);
                            entityPortal.SaveChanges();

                        }
                        if (lstSyncSubs.Count() > 0)
                        {
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "DehnadMCIFtpChargingService:syncSubscription:" + entryService.ServiceCode + ":" + lstSyncSubs.Count() + " Subscribers synchronized");
                            Program.logs.Info("DehnadMCIFtpChargingService:syncSubscription:" + entryService.ServiceCode + ":" + lstSyncSubs.Count() + " Subscribers synchronized");
                        }
                        if (string.IsNullOrEmpty(Properties.Settings.Default.SyncServiceLastTime))
                        {
                            Properties.Settings.Default.SyncServiceLastTime = entryService.ServiceCode + "-" + SyncStartTime.ToString("yyyy/MM/dd HH:mm:ss");
                        }
                        else
                        {
                            strServiceLastTimeArr = Properties.Settings.Default.SyncServiceLastTime.Split(';');
                            for (i = 0; i <= strServiceLastTimeArr.Length - 1; i++)
                            {
                                if (strServiceLastTimeArr[i].Split('-')[0] == entryService.ServiceCode)
                                {
                                    strServiceLastTimeArr[i] = entryService.ServiceCode + "-" + SyncStartTime.ToString("yyyy/MM/dd HH:mm:ss");
                                    break;
                                }
                            }
                            if (i == strServiceLastTimeArr.Length)
                            {
                                Array.Resize(ref strServiceLastTimeArr, strServiceLastTimeArr.Length + 1);
                                strServiceLastTimeArr[strServiceLastTimeArr.Length - 1] = entryService.ServiceCode + "-" + SyncStartTime.ToString("yyyy/MM/dd HH:mm:ss");
                            }
                            Properties.Settings.Default.SyncServiceLastTime = string.Join(";", strServiceLastTimeArr);
                        }
                        Program.logs.Error("sad");
                        Properties.Settings.Default.Save();
                        Program.logs.Error("sad11233");
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
