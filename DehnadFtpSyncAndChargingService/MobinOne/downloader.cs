﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DehnadFtpSyncAndChargingService.MobinOne
{
    class downloader
    {
        string ServerUrl;// = "172.17.252.201";
        string ServerIP;// = "ftp://172.17.252.201";
        string FtpUser;// = "DEH";
        string FtpPassword;// = "d9H&*&123";
        public downloader()
        {
            string userName, pwd;
            ServerUrl = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.mobinOneFtp, SharedLibrary.HelpfulFunctions.enumServersActions.mobineOneFtpSync, out userName, out pwd);
            Uri uri = new Uri(ServerUrl);
            ServerIP = uri.Host;

            FtpUser = userName;
            FtpPassword = pwd;
        }

        public void updateSingleChargeAndSubscription()
        {
            updateSingleChargeAndSubscription(DateTime.Now.ToString("yyyy-MM-dd"), null, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="winDirectory">send as date format yyyy-MM-dd</param>
        /// <param name="operatorSIDs">Operators service Id for example 9951 for achar if specfied it means we should get all file which starts with this sid no matter if we get before or not</param>
        public void updateSingleChargeAndSubscription(string winDirectory, string[] operatorSIDs, bool downloadAnyway)
        {
            try
            {
                using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                {
                    bool saveFailFilesToSingleCharge = Properties.Settings.Default.SaveFailFilesToSingleCharge;
                    if (operatorSIDs == null)
                    {
                        if (entityPortal.vw_servicesServicesInfo.Count(o => o.aggregatorName.ToLower() == "mobinone") == 0)
                        {
                            Program.logs.Warn("FtpSync:MobinOne:updateSingleCharge:NoService is defined for mobinone");
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, "FtpSync:MobinOne:updateSingleCharge:NoService is defined for mobinone");
                            return;
                        }
                        operatorSIDs = entityPortal.vw_servicesServicesInfo.Where(o => o.aggregatorName.ToLower() == "mobinone").Select(o => o.OperatorServiceId).ToArray();
                    }

                    foreach (string operatorSID in operatorSIDs)
                    {
                        List<FtpFile> newFtpFiles = this.downloadNewFiles(winDirectory, operatorSID, downloadAnyway);

                        if (newFtpFiles == null) return;
                        if (newFtpFiles.Count == 0)
                        {
                            Program.logs.Info("FtpSync:MobinOne:updateSingleCharge:There is no file to download for SID=" + operatorSID + " and date=" + winDirectory);
                            continue;
                        }

                        int i, j;
                        StreamReader sr;
                        string str;
                        string[] lines;
                        int chargeCount = 0;
                        int subCount = 0;
                        int unsubCount = 0;
                        ftpItemInfo ftpItem = new ftpItemInfo();
                        List<string> props;
                        SharedLibrary.Models.ServiceModel.Singlecharge singleCharge;
                        SharedLibrary.Models.ServiceModel.SinglechargeArchive singleChargeArchive;
                        SharedLibrary.Models.FtpSyncFile portalFtpFiles;
                        bool saveFtpFile;
                        string mobileNumber;
                        DateTime regDate;

                        for (i = 0; i <= newFtpFiles.Count - 1; i++)
                        {
                            chargeCount = 0;
                            subCount = 0;
                            unsubCount = 0;
                            saveFtpFile = true;
                            //Program.logs.Info("updateSingleCharge:Read File " + newFtpFiles[i]);
                            if (Path.GetFileName(newFtpFiles[i].winFilePath).ToLower().Contains("_fail_") && !saveFailFilesToSingleCharge)
                            {
                                //if filename contains _fail_ do not save to single charge
                                saveFtpFile = false;
                            }

                            using (sr = new StreamReader(newFtpFiles[i].winFilePath))
                            {
                                //lines = File.ReadAllLines(newFtpFiles[i].winFilePath);
                                str = sr.ReadToEnd();
                                props = ftpItem.GetType().GetProperties().Where(o => o.Name.Contains("_")).Select(o => o.Name).ToList();
                                for (j = 0; j <= props.Count - 1; j++)
                                {
                                    //e.g. replace trans-id with trans_id
                                    str = str.Replace(props[j].Replace("_", "-"), props[j]);
                                }
                                lines = str.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                Program.logs.Info("FtpSync:MobinOne:updateSingleCharge:Read " + newFtpFiles[i].fileName + " with " + lines.Length + " lines(remaining files = " + (newFtpFiles.Count - i - 1) + ")");

                                for (j = 0; j <= lines.Length - 1; j++)
                                {
                                    try
                                    {
                                        ftpItem = Newtonsoft.Json.JsonConvert.DeserializeObject<ftpItemInfo>(lines[j]);
                                    }
                                    catch (Exception ex)
                                    {
                                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "FtpSync:MobinOne:" + "downloader:updateSingleCharge:" + ex.Message);
                                        Program.logs.Error("FtpSync:MobinOne:downloader:updateSingleCharge:", ex);
                                    }
                                    var serviceItem = entityPortal.vw_servicesServicesInfo.Where(o => o.OperatorServiceId == ftpItem.sid.ToString()).FirstOrDefault();

                                    if (serviceItem == null || string.IsNullOrEmpty(serviceItem.ServiceCode))
                                    {
                                        Program.logs.Error("FtpSync:MobinOne:UpdateSingleChargeAndSubscription: no service is found with operatorServiceId" + ftpItem.sid);
                                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFTPDownloader:UpdateSingleChargeAndSubscription: no service is found with operatorServiceId " + ftpItem.sid);
                                        continue;
                                    }
                                    mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(ftpItem.msisdn);
                                    if (mobileNumber == "Invalid Mobile Number")
                                    {
                                        Program.logs.Error("FtpSync:MobinOne:UpdateSingleChargeAndSubscription: Invalid Mobile Number" + ftpItem.msisdn);
                                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFTPDownloader:UpdateSingleChargeAndSubscription: Invalid Mobile Number " + ftpItem.msisdn);
                                        continue;
                                    }
                                    regDate = DateTime.Now;
                                    this.AddFtpLog(ftpItem, newFtpFiles[i].winFilePath
                                            , regDate, serviceItem.Id);

                                    if (saveFtpFile)
                                    {

                                        if (ftpItem.event_type == "1.5")
                                        {

                                            #region update singlecharge
                                            chargeCount++;
                                            using (SharedLibrary.Models.ServiceModel.SharedServiceEntities entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(serviceItem.ServiceCode))
                                            {
                                                if (winDirectory == DateTime.Today.ToString("yyyy-MM-dd"))
                                                {
                                                    if (ftpItem.status == 0)
                                                    {
                                                        //success charging
                                                        // if we have another success charging in database with this price do not insert new charge record
                                                        singleCharge = entityService.Singlecharges.FirstOrDefault(o => o.MobileNumber == mobileNumber
                                                        && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(ftpItem.datetime)
                                                        && o.Price == (ftpItem.base_price_point / 10)
                                                        && o.IsSucceeded);
                                                    }
                                                    else
                                                    {
                                                        //unseccessfull charging
                                                        //check if we have antoher record with this mobilenumber/price/issucceeded with 30 mins before and after
                                                        singleCharge = entityService.Singlecharges.FirstOrDefault(o => o.MobileNumber == mobileNumber
                                                        && DbFunctions.AddMinutes(ftpItem.datetime, -30) <= o.DateCreated
                                                        && o.DateCreated <= DbFunctions.AddMinutes(ftpItem.datetime, 30)
                                                        && o.Price == (ftpItem.base_price_point / 10)
                                                        && !o.IsSucceeded);
                                                    }
                                                    if (singleCharge == null)
                                                    {
                                                        singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();

                                                        if (ftpItem.status != 0)
                                                            singleCharge.IsSucceeded = false;
                                                        else
                                                            singleCharge.IsSucceeded = true;

                                                        singleCharge.ReferenceId = ftpItem.trans_id;
                                                        singleCharge.Price = ftpItem.base_price_point.Value / 10;
                                                        singleCharge.IsApplicationInformed = false;
                                                        singleCharge.IsCalledFromInAppPurchase = false;
                                                        singleCharge.Description = "NewMethod";
                                                        singleCharge.DateCreated = ftpItem.datetime;
                                                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(ftpItem.datetime);
                                                        singleCharge.MobileNumber = mobileNumber;
                                                        entityService.Singlecharges.Add(singleCharge);
                                                    }
                                                    else
                                                    {
                                                        //singleCharge = new SharedShortCodeServiceLibrary.SharedModel.Singlecharge();

                                                        if (ftpItem.status != 0)
                                                            singleCharge.IsSucceeded = false;
                                                        else
                                                            singleCharge.IsSucceeded = true;

                                                        singleCharge.ReferenceId = ftpItem.trans_id;
                                                        singleCharge.Price = ftpItem.base_price_point.Value / 10;
                                                        singleCharge.IsApplicationInformed = false;
                                                        singleCharge.IsCalledFromInAppPurchase = false;
                                                        singleCharge.Description = "NewMethod";
                                                        singleCharge.DateCreated = ftpItem.datetime;
                                                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(ftpItem.datetime);
                                                        singleCharge.MobileNumber = mobileNumber;
                                                        entityService.Entry(singleCharge).State = System.Data.Entity.EntityState.Modified;
                                                    }
                                                }
                                                else
                                                {
                                                    if (ftpItem.status == 0)
                                                    {
                                                        //success charging
                                                        // if we have another success charging in database with this price do not insert new charge record
                                                        singleChargeArchive = entityService.SinglechargeArchives.FirstOrDefault(o => o.MobileNumber == mobileNumber
                                                        && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(ftpItem.datetime)
                                                        && o.Price == (ftpItem.base_price_point / 10)
                                                        && o.IsSucceeded);
                                                    }
                                                    else
                                                    {
                                                        //unseccessfull charging
                                                        //check if we have antoher record with this mobilenumber/price/issucceeded with 30 mins before and after
                                                        singleChargeArchive = entityService.SinglechargeArchives.FirstOrDefault(o => o.MobileNumber == mobileNumber
                                                        && DbFunctions.AddMinutes(ftpItem.datetime, -30) <= o.DateCreated
                                                        && o.DateCreated <= DbFunctions.AddMinutes(ftpItem.datetime, 30)
                                                        && o.Price == (ftpItem.base_price_point / 10)
                                                        && !o.IsSucceeded);
                                                    }

                                                    if (singleChargeArchive == null)
                                                    {
                                                        singleChargeArchive = new SharedLibrary.Models.ServiceModel.SinglechargeArchive();

                                                        if (ftpItem.status != 0)
                                                            singleChargeArchive.IsSucceeded = false;
                                                        else
                                                            singleChargeArchive.IsSucceeded = true;

                                                        singleChargeArchive.ReferenceId = ftpItem.trans_id;
                                                        singleChargeArchive.Price = ftpItem.base_price_point.Value / 10;
                                                        singleChargeArchive.IsApplicationInformed = false;
                                                        singleChargeArchive.IsCalledFromInAppPurchase = false;
                                                        singleChargeArchive.Description = "NewMethod";
                                                        singleChargeArchive.DateCreated = ftpItem.datetime;
                                                        singleChargeArchive.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(ftpItem.datetime);
                                                        singleChargeArchive.MobileNumber = mobileNumber;
                                                        entityService.SinglechargeArchives.Add(singleChargeArchive);
                                                    }
                                                    else
                                                    {
                                                        //singleChargeArchive = new SharedShortCodeServiceLibrary.SharedModel.SinglechargeArchive();

                                                        if (ftpItem.status != 0)
                                                            singleChargeArchive.IsSucceeded = false;
                                                        else
                                                            singleChargeArchive.IsSucceeded = true;

                                                        singleChargeArchive.ReferenceId = ftpItem.trans_id;
                                                        singleChargeArchive.Price = ftpItem.base_price_point.Value / 10;
                                                        singleChargeArchive.IsApplicationInformed = false;
                                                        singleChargeArchive.IsCalledFromInAppPurchase = false;
                                                        singleChargeArchive.Description = "NewMethod";
                                                        singleChargeArchive.DateCreated = ftpItem.datetime;
                                                        singleChargeArchive.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(ftpItem.datetime);
                                                        singleChargeArchive.MobileNumber = mobileNumber;
                                                        entityService.Entry(singleChargeArchive).State = System.Data.Entity.EntityState.Modified;
                                                    }
                                                }
                                                entityService.SaveChanges();
                                            }
                                            #endregion
                                        }
                                        #region update ftp last state
                                        this.UpdateFtpLastState(entityPortal, ftpItem, newFtpFiles[i].winFilePath
                                            , regDate, serviceItem.Id, ref subCount, ref unsubCount);
                                        #endregion

                                    }
                                }
                                string ftpUrl = newFtpFiles[i].ftpFilePath;
                                if (string.IsNullOrEmpty(ftpUrl)) continue;

                                Program.logs.Info(ftpUrl);

                                var ftpFile = entityPortal.FtpSyncFiles.Where(o => "ftp://" + o.serverIP + "/" + o.ftpDirectory + "/" + o.fileName == ftpUrl).FirstOrDefault();
                                if (ftpFile == null)
                                {
                                    //new file or modification detected
                                    portalFtpFiles = new SharedLibrary.Models.FtpSyncFile();
                                    portalFtpFiles.fileName = Path.GetFileName(newFtpFiles[i].winFilePath);
                                    portalFtpFiles.ftpDirectory = winDirectory;
                                    portalFtpFiles.processDateTime = DateTime.Now;
                                    portalFtpFiles.serverIP = ServerIP;
                                    portalFtpFiles.identifier = newFtpFiles[i].identifier;
                                    portalFtpFiles.processLines = lines.Count();
                                    portalFtpFiles.chargeCount = chargeCount;
                                    portalFtpFiles.subCount = subCount;
                                    portalFtpFiles.unsubCount = unsubCount;
                                    entityPortal.FtpSyncFiles.Add(portalFtpFiles);

                                }
                                else
                                {
                                    portalFtpFiles = ftpFile;
                                    portalFtpFiles.fileName = Path.GetFileName(newFtpFiles[i].winFilePath);
                                    portalFtpFiles.ftpDirectory = winDirectory;
                                    portalFtpFiles.processDateTime = DateTime.Now;
                                    portalFtpFiles.serverIP = ServerIP;
                                    portalFtpFiles.identifier = newFtpFiles[i].identifier;
                                    portalFtpFiles.processLines = lines.Count();
                                    portalFtpFiles.chargeCount = chargeCount;
                                    portalFtpFiles.subCount = subCount;
                                    portalFtpFiles.unsubCount = unsubCount;
                                    entityPortal.Entry(portalFtpFiles).State = System.Data.Entity.EntityState.Modified;
                                }

                                entityPortal.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "FtpSync:MobinOne:" + "downloader:updateSingleCharge:" + e.Message);
                Program.logs.Error("FtpSync:MobinOne:downloader:updateSingleCharge:", e);
                return;
            }
        }

        private void AddFtpLog(ftpItemInfo ftpItem, string winFilePath, DateTime regdate, long serviceId)
        {
            using (var entityFtp = new FtpLogEntities())
            {
                #region save ftp line into ftplog database
                var entryMCILog = new MCILog()
                {
                    base_price_point = ftpItem.base_price_point
                    ,
                    billed_price_point = ftpItem.billed_price_point
                    ,
                    channel = ftpItem.channel
                    ,
                    chargeCode = ftpItem.chargeCode
                    ,
                    datetime = ftpItem.datetime
                    ,
                    event_type = ftpItem.event_type
                    ,
                    filePath = winFilePath
                    ,
                    keyword = ftpItem.keyword
                    ,
                    mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(ftpItem.msisdn)
                    ,
                    msisdn = ftpItem.msisdn
                    ,
                    next_renewal_date = ftpItem.next_renewal_date
                    ,
                    regdate = regdate
                    ,
                    shortcode = ftpItem.shortcode
                    ,
                    sid = ftpItem.sid
                    ,
                    serviceId = serviceId
                    ,
                    status = ftpItem.status
                    ,
                    trans_id = ftpItem.trans_id
                    ,
                    trans_status = ftpItem.trans_status
                    ,
                    validity = ftpItem.validity
                };

                entityFtp.MCILogs.Add(entryMCILog);
                entityFtp.SaveChanges();
            }
            #endregion
        }

        private void UpdateFtpLastState(SharedLibrary.Models.PortalEntities entityPortal, ftpItemInfo ftpItem
            , string winFilePath, DateTime regdate, long serviceId, ref int subCount, ref int unsubCount)
        {
            bool addNewRecord = false;

            var entryLastFtp = entityPortal.FtpSubAndChargeLastStates.Where(o => o.sid == ftpItem.sid && o.msisdn == ftpItem.msisdn).OrderByDescending(o => o.datetime).FirstOrDefault();
            if (ftpItem.event_type == "1.5")
            {
                if (entryLastFtp == null || (entryLastFtp.datetime < ftpItem.datetime && entryLastFtp.event_type == "1.5"))
                {
                    //there is no item or there is try after this try
                    if (entryLastFtp == null)
                    {
                        entryLastFtp = new SharedLibrary.Models.FtpSubAndChargeLastState();
                        addNewRecord = true;
                    }
                    else addNewRecord = false;
                }
                else return;
            }
            else if (ftpItem.event_type == "1.1" || ftpItem.event_type == "1.2")
            {
                if (ftpItem.event_type == "1.2")
                {
                    unsubCount++;
                }
                else subCount++;

                if (entryLastFtp == null || (entryLastFtp.datetime < ftpItem.datetime) || (entryLastFtp.datetime >= ftpItem.datetime && entryLastFtp.event_type == "1.5"))
                {
                    //there is no item or there is sub/unsub before this item or there is try after ftp item
                    if (entryLastFtp == null)
                    {
                        entryLastFtp = new SharedLibrary.Models.FtpSubAndChargeLastState();
                        addNewRecord = true;
                    }

                }
                else
                {
                    //there is no newer state for this subscriber
                    return;
                }

            }
            entryLastFtp.base_price_point = ftpItem.base_price_point;
            entryLastFtp.billed_price_point = ftpItem.billed_price_point;
            entryLastFtp.channel = ftpItem.channel;
            entryLastFtp.chargeCode = ftpItem.chargeCode;
            entryLastFtp.datetime = ftpItem.datetime;
            entryLastFtp.event_type = ftpItem.event_type;
            entryLastFtp.filePath = winFilePath;
            entryLastFtp.keyword = ftpItem.keyword;
            entryLastFtp.mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(ftpItem.msisdn);
            entryLastFtp.msisdn = ftpItem.msisdn;
            entryLastFtp.next_renewal_date = ftpItem.next_renewal_date;
            entryLastFtp.regdate = regdate;
            entryLastFtp.shortcode = ftpItem.shortcode;
            entryLastFtp.serviceId = serviceId;
            entryLastFtp.sid = ftpItem.sid;
            entryLastFtp.status = ftpItem.status;
            entryLastFtp.trans_id = ftpItem.trans_id;
            entryLastFtp.trans_status = ftpItem.trans_status;
            entryLastFtp.validity = ftpItem.validity;

            if (addNewRecord)
            {
                entityPortal.FtpSubAndChargeLastStates.Add(entryLastFtp);
            }
            else
            {
                entityPortal.Entry(entryLastFtp).State = System.Data.Entity.EntityState.Modified;
            }
            entityPortal.SaveChanges();
        }
        private string getFtpUrl(string winDirectory)
        {
            try
            {
                winDirectory = winDirectory.Replace("/", "\\");
                //create directory if not does exist
                if (!this.createDirectory(Properties.Settings.Default.LocalPathMobinOne, winDirectory))
                    return null;

                string ftpDirectory = winDirectory.Replace("\\", "/");
                if (ftpDirectory.StartsWith("/"))
                    ftpDirectory = ftpDirectory.Remove(0, 1);
                if (ftpDirectory.EndsWith("/"))
                    ftpDirectory = ftpDirectory.Remove(winDirectory.Length - 1, 1);

                string ftpURL = ServerUrl + ftpDirectory + "/";
                return ftpURL;
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "FtpSync:MobinOne:" + "downloader:getFtpUrl:" + e.Message);
                Program.logs.Error("FtpSync:MobinOne:downloader:getFtpUrl:", e);
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="winDirectory"></param>
        /// <param name="operatorSIDs">Operators service Id for example 9951 for achar if specfied it means we should get all file which starts with this sid no matter if we get before or not</param>
        /// <returns></returns>
        private List<FtpFile> downloadNewFiles(string winDirectory, string operatorSID, bool downloadAnyway)
        {
            try
            {
                string localPath = Properties.Settings.Default.LocalPathMobinOne;
                if (localPath.EndsWith("\\"))
                    localPath = localPath.Remove(localPath.Length - 1, 1);

                //string ftpDirectory = getFtpUrl(winDirectory);
                string ftpUrl = getFtpUrl(winDirectory + "/" + operatorSID);
                if (string.IsNullOrEmpty(ftpUrl)) return null;

                int i;
                List<FtpFile> newFtpFiles = this.detectNewFtpFiles(localPath + "\\" + winDirectory, ftpUrl, operatorSID, downloadAnyway);
                if (newFtpFiles == null) return newFtpFiles;
                if (newFtpFiles != null && newFtpFiles.Count > 0)
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "FtpSync:MobinOne:downloadNewFiles:" + newFtpFiles.Count.ToString() + " new files have been detected for" + winDirectory);
                    Program.logs.Info("FtpSync:MobinOne:downloadNewFiles: " + newFtpFiles.Count.ToString() + " new files have been detected");
                }
                for (i = 0; i <= newFtpFiles.Count - 1; i++)
                {
                    Program.logs.Info("FtpSync:MobinOne:downloadNewFiles:downloadFile " + newFtpFiles[i].ftpFilePath + " is started");
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(newFtpFiles[i].ftpFilePath);
                    request.Method = WebRequestMethods.Ftp.DownloadFile;
                    request.Credentials = new NetworkCredential(this.FtpUser, this.FtpPassword);
                    request.KeepAlive = false;
                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(responseStream);

                    //string filePath = localPath + "\\" + winDirectory + "\\" + newFilesUri[i]..Split('/')[newFilesUri[i].Split('/').Length - 1];
                    if (File.Exists(newFtpFiles[i].winFilePath)) File.Delete(newFtpFiles[i].winFilePath);
                    using (FileStream writer = new FileStream(newFtpFiles[i].winFilePath, FileMode.Create))
                    {
                        long length = response.ContentLength;
                        int bufferSize = 2048;
                        int readCount;
                        byte[] buffer = new byte[2048];

                        readCount = responseStream.Read(buffer, 0, bufferSize);
                        while (readCount > 0)
                        {
                            writer.Write(buffer, 0, readCount);
                            readCount = responseStream.Read(buffer, 0, bufferSize);
                        }
                    }
                    reader.Close();
                    response.Close();
                    Program.logs.Info("FtpSync:MobinOne:downloadNewFiles:downloadFile " + newFtpFiles[i].fileName + " is finished");
                }
                return newFtpFiles;
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "FtpSync:MobinOne:" + "downloader:downloadNewFiles:" + e.Message);
                Program.logs.Error("FtpSync:MobinOne:downloader:downloadNewFiles:", e);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="windowsFilePath"></param>
        /// <param name="ftpDirectory"></param>
        /// <param name="operatorSID">Operators service Id for example 9951 for achar if specfied it means we should get all file which starts with this sid no matter if we get before or not</param>
        /// <returns></returns>
        private List<FtpFile> detectNewFtpFiles(string windowsFilePath, string ftpDirectory, string operatorSID, bool downloadAnyway)
        {
            FtpWebRequest ftpRequest = null;
            FtpWebResponse ftpResponse = null;
            FtpWebResponse response = null;
            StreamReader streamReader = null;
            try
            {
                Uri uri = new Uri(ftpDirectory);
                //Uri uri = new Uri("ftp://172.17.252.201/" + directoryName + "/");
                ftpRequest = (FtpWebRequest)WebRequest.Create(uri);
                ftpRequest.Credentials = new NetworkCredential(this.FtpUser, this.FtpPassword);
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                ftpRequest.KeepAlive = false;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                streamReader = new StreamReader(ftpResponse.GetResponseStream());
                string fileName = streamReader.ReadLine();

                List<FtpFile> newFtpFiles = new List<FtpFile>();

                using (var portal = new SharedLibrary.Models.PortalEntities())
                {
                    while (!string.IsNullOrEmpty(fileName))
                    {
                        if (operatorSID != null && !fileName.StartsWith(operatorSID))
                        {
                            //if operatorSIDs are specified and ftp file name not does not start with operatorSIDs do nothing and read next file
                            fileName = streamReader.ReadLine();
                            continue;
                        }
                        uri = new Uri(ftpDirectory + fileName);
                        ftpRequest = (FtpWebRequest)WebRequest.Create(uri);
                        ftpRequest.Credentials = new NetworkCredential(this.FtpUser, this.FtpPassword);
                        ftpRequest.Proxy = null;
                        ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;
                        response = (FtpWebResponse)ftpRequest.GetResponse();
                        long size = response.ContentLength;
                        response.Close();

                        uri = new Uri(ftpDirectory + fileName);
                        ftpRequest = (FtpWebRequest)WebRequest.Create(uri);
                        ftpRequest.Credentials = new NetworkCredential(this.FtpUser, this.FtpPassword);
                        ftpRequest.Proxy = null;
                        ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                        response = (FtpWebResponse)ftpRequest.GetResponse();
                        var modifiedTime = response.LastModified;
                        response.Close();
                        string identifier = size + ";" + modifiedTime.ToString();

                        SharedLibrary.Models.FtpSyncFile ftpFile = null;
                        if (operatorSID == null || !downloadAnyway)
                        {
                            //if operatorSIDs are not specified check if process this file before or not
                            //otherwise do not check and detect the file as a new file
                            ftpFile = portal.FtpSyncFiles.Where(o => "ftp://" + o.serverIP + "/" + o.ftpDirectory + "/" + o.fileName == uri.AbsoluteUri && (string.IsNullOrEmpty(o.identifier) || o.identifier == identifier)).FirstOrDefault();
                        }

                        if (ftpFile == null)
                        {
                            Program.logs.Info("FtpSync:MobinOne:downloader:detectNewFtpFiles: new or modified File is " + uri.AbsoluteUri + " with identifier " + identifier);
                            //new file or modification detected
                            newFtpFiles.Add(new FtpFile(fileName, windowsFilePath + (windowsFilePath.EndsWith("\\") ? "" : "\\") + operatorSID + "\\" + fileName
                                , ftpDirectory + (ftpDirectory.EndsWith("/") ? "" : "/") + fileName, identifier));
                        }

                        fileName = streamReader.ReadLine();
                    }
                }
                ftpResponse.Close();

                streamReader.Close();
                Program.logs.Info("FtpSync:MobinOne:downloader:detectNewFtpFiles:" + newFtpFiles.Count + " new or modified files have been detected ");
                return newFtpFiles;
            }
            catch (Exception e)
            {
                try
                {
                    if (streamReader != null)
                    {
                        streamReader.Close();
                        streamReader.Dispose();
                    }
                }
                catch
                {

                }

                try
                {
                    if (response != null)
                    {
                        response.Close();
                        response.Dispose();
                    }
                }
                catch
                {

                }
                try
                {
                    if (ftpResponse != null)
                    {
                        ftpResponse.Close();
                        ftpResponse.Dispose();
                    }
                }
                catch
                {

                }



                try
                {
                    if (ftpRequest != null)
                    {
                        ftpRequest.Abort();
                    }
                }
                catch
                {

                }
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "FtpSync:MobinOne:downloader:detectNewFilesUri:" + e.Message);
                Program.logs.Error("FtpSync:MobinOne:downloader:detectNewFilesUri:", e);

                return null;
            }
        }

        private bool createDirectory(string localPath, string directory)
        {
            try
            {
                string[] dirParts = directory.Split('\\');
                int i;
                if (localPath.EndsWith("\\"))
                    localPath = localPath.Remove(localPath.Length - 1, 1);

                string dirPath = localPath;
                for (i = 0; i <= dirParts.Length - 1; i++)
                {
                    if (string.IsNullOrEmpty(dirParts[i]))
                        break;
                    dirPath = dirPath + "\\" + dirParts[i];
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                        Program.logs.Info("FtpSync:MobinOne:downloader:createDirectory:" + dirPath);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "FtpSync:MobinOne:downloader:" + "createDirectory:" + e.Message);
                Program.logs.Error("FtpSync:MobinOne:downloader:createDirectory:", e);
                return false;
            }
        }
    }
}
