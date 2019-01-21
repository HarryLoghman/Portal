using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DehnadMCIFtpChargingService
{
    class downloader
    {
        string ServerFtpIP = "172.17.252.201";
        string FtpUser = "DEH";
        string FtpPassword = "d9H&*&123";
        public void updateSingleCharge()
        {
            updateSingleCharge(DateTime.Now.ToString("yyyyMMdd"), null, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="winDirectory">send as date format yyyyMMdd</param>
        /// <param name="operatorSIDs">Operators service Id for example 9951 for achar if specfied it means we should get all file which starts with this sid no matter if we get before or not</param>
        public void updateSingleCharge(string winDirectory, string[] operatorSIDs, bool downloadAnyway)
        {
            try
            {
                bool saveFailFilesToSingleCharge;
                if (!bool.TryParse(Properties.Settings.Default.SaveFailFilesToSingleCharge, out saveFailFilesToSingleCharge))
                {
                    saveFailFilesToSingleCharge = false;
                }
                List<FtpFile> newFtpFiles = this.downloadNewFiles(winDirectory, operatorSIDs, downloadAnyway);
                
                if (newFtpFiles == null) return;
                if (newFtpFiles.Count == 0)
                {
                    Program.logs.Info("updateSingleCharge:There is no file to download");
                    return;
                }

                int i, j;
                StreamReader sr;
                string str;
                string[] lines;
                int chargeCount = 0;
                ChargeInfo charge = new ChargeInfo();
                List<string> props;
                SharedLibrary.Models.ServiceModel.Singlecharge singleCharge;
                SharedLibrary.Models.ServiceModel.SinglechargeArchive singleChargeArchive;
                SharedLibrary.Models.MCISingleChargeFtpFile portalFtpFiles;
                bool saveSingleCharge;
                using (var portal = new SharedLibrary.Models.PortalEntities())
                {
                    for (i = 0; i <= newFtpFiles.Count - 1; i++)
                    {
                        chargeCount = 0;
                        saveSingleCharge = true;
                        //Program.logs.Info("updateSingleCharge:Read File " + newFtpFiles[i]);
                        if (Path.GetFileName(newFtpFiles[i].winFilePath).ToLower().Contains("_fail_") && !saveFailFilesToSingleCharge)
                        {
                            //if filename contains _fail_ do not save to single charge
                            saveSingleCharge = false;
                        }

                        using (sr = new StreamReader(newFtpFiles[i].winFilePath))
                        {
                            //lines = File.ReadAllLines(newFtpFiles[i].winFilePath);
                            str = sr.ReadToEnd();
                            props = charge.GetType().GetProperties().Where(o => o.Name.Contains("_")).Select(o => o.Name).ToList();
                            for (j = 0; j <= props.Count - 1; j++)
                            {
                                //e.g. replace trans-id with trans_id
                                str = str.Replace(props[j].Replace("_", "-"), props[j]);
                            }
                            lines = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                            Program.logs.Info("updateSingleCharge:Read " + newFtpFiles[i].fileName + " with " + lines.Length + " lines(remaining files = " + (newFtpFiles.Count - i - 1) + ")");
                            if (saveSingleCharge)
                            {
                                for (j = 0; j <= lines.Length - 1; j++)
                                {
                                    charge = Newtonsoft.Json.JsonConvert.DeserializeObject<ChargeInfo>(lines[j]);
                                    if (charge.event_type == "1.5")
                                    {
                                        chargeCount++;
                                        var serviceCode = portal.vw_servicesServicesInfo.Where(o => o.OperatorServiceId == charge.sid.ToString()).Select(o => o.ServiceCode).FirstOrDefault();
                                        if (string.IsNullOrEmpty(serviceCode))
                                        {
                                            Program.logs.Error("UpdateSingleCharge: no service is found with operatorServiceId" + charge.sid);
                                            continue;
                                        }
                                        using (SharedLibrary.Models.ServiceModel.SharedServiceEntities serviceDB = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Shared" + serviceCode + "Entities"))
                                        {
                                            if (winDirectory == DateTime.Today.ToString("yyyyMMdd"))
                                            {
                                                singleCharge = serviceDB.Singlecharges.Where(o => o.ReferenceId == charge.trans_id && o.Description == "NewMethod").FirstOrDefault();
                                                if (singleCharge == null)
                                                {
                                                    singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();

                                                    if (charge.status != 0)
                                                        singleCharge.IsSucceeded = false;
                                                    else
                                                        singleCharge.IsSucceeded = true;

                                                    singleCharge.ReferenceId = charge.trans_id;
                                                    singleCharge.Price = charge.base_price_point.Value / 10;
                                                    singleCharge.IsApplicationInformed = false;
                                                    singleCharge.IsCalledFromInAppPurchase = false;
                                                    singleCharge.Description = "NewMethod";
                                                    singleCharge.DateCreated = charge.datetime;
                                                    singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(charge.datetime);
                                                    singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(charge.msisdn);
                                                    serviceDB.Singlecharges.Add(singleCharge);
                                                }
                                                else
                                                {
                                                    //singleCharge = new SharedShortCodeServiceLibrary.SharedModel.Singlecharge();

                                                    if (charge.status != 0)
                                                        singleCharge.IsSucceeded = false;
                                                    else
                                                        singleCharge.IsSucceeded = true;

                                                    singleCharge.ReferenceId = charge.trans_id;
                                                    singleCharge.Price = charge.base_price_point.Value / 10;
                                                    singleCharge.IsApplicationInformed = false;
                                                    singleCharge.IsCalledFromInAppPurchase = false;
                                                    singleCharge.Description = "NewMethod";
                                                    singleCharge.DateCreated = charge.datetime;
                                                    singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(charge.datetime);
                                                    singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(charge.msisdn);
                                                    serviceDB.Entry(singleCharge).State = System.Data.Entity.EntityState.Modified;
                                                }
                                            }
                                            else
                                            {
                                                singleChargeArchive = serviceDB.SinglechargeArchives.Where(o => o.ReferenceId == charge.trans_id && o.Description == "NewMethod").FirstOrDefault();
                                                if (singleChargeArchive == null)
                                                {
                                                    singleChargeArchive = new SharedLibrary.Models.ServiceModel.SinglechargeArchive();

                                                    if (charge.status != 0)
                                                        singleChargeArchive.IsSucceeded = false;
                                                    else
                                                        singleChargeArchive.IsSucceeded = true;

                                                    singleChargeArchive.ReferenceId = charge.trans_id;
                                                    singleChargeArchive.Price = charge.base_price_point.Value / 10;
                                                    singleChargeArchive.IsApplicationInformed = false;
                                                    singleChargeArchive.IsCalledFromInAppPurchase = false;
                                                    singleChargeArchive.Description = "NewMethod";
                                                    singleChargeArchive.DateCreated = charge.datetime;
                                                    singleChargeArchive.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(charge.datetime);
                                                    singleChargeArchive.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(charge.msisdn);
                                                    serviceDB.SinglechargeArchives.Add(singleChargeArchive);
                                                }
                                                else
                                                {
                                                    //singleChargeArchive = new SharedShortCodeServiceLibrary.SharedModel.SinglechargeArchive();

                                                    if (charge.status != 0)
                                                        singleChargeArchive.IsSucceeded = false;
                                                    else
                                                        singleChargeArchive.IsSucceeded = true;

                                                    singleChargeArchive.ReferenceId = charge.trans_id;
                                                    singleChargeArchive.Price = charge.base_price_point.Value / 10;
                                                    singleChargeArchive.IsApplicationInformed = false;
                                                    singleChargeArchive.IsCalledFromInAppPurchase = false;
                                                    singleChargeArchive.Description = "NewMethod";
                                                    singleChargeArchive.DateCreated = charge.datetime;
                                                    singleChargeArchive.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(charge.datetime);
                                                    singleChargeArchive.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(charge.msisdn);
                                                    serviceDB.Entry(singleChargeArchive).State = System.Data.Entity.EntityState.Modified;
                                                }
                                            }
                                            serviceDB.SaveChanges();
                                        }
                                    }
                                }
                            }
                            string ftpUrl = newFtpFiles[i].ftpFilePath;
                            if (string.IsNullOrEmpty(ftpUrl)) continue;

                            Program.logs.Info(ftpUrl);

                            var ftpFile = portal.MCISingleChargeFtpFiles.Where(o => "ftp://" + o.serverIP + "/" + o.ftpDirectory + "/" + o.fileName == ftpUrl).FirstOrDefault();
                            if (ftpFile == null)
                            {
                                //new file or modification detected
                                portalFtpFiles = new SharedLibrary.Models.MCISingleChargeFtpFile();
                                portalFtpFiles.fileName = Path.GetFileName(newFtpFiles[i].winFilePath);
                                portalFtpFiles.ftpDirectory = winDirectory;
                                portalFtpFiles.processDateTime = DateTime.Now;
                                portalFtpFiles.serverIP = this.ServerFtpIP;
                                portalFtpFiles.identifier = newFtpFiles[i].identifier;
                                portalFtpFiles.processLines = lines.Count();
                                portalFtpFiles.chargeCount = chargeCount;
                                portal.MCISingleChargeFtpFiles.Add(portalFtpFiles);

                            }
                            else
                            {
                                portalFtpFiles = ftpFile;
                                portalFtpFiles.fileName = Path.GetFileName(newFtpFiles[i].winFilePath);
                                portalFtpFiles.ftpDirectory = winDirectory;
                                portalFtpFiles.processDateTime = DateTime.Now;
                                portalFtpFiles.serverIP = this.ServerFtpIP;
                                portalFtpFiles.identifier = newFtpFiles[i].identifier;
                                portalFtpFiles.processLines = lines.Count();
                                portalFtpFiles.chargeCount = chargeCount;
                                portal.Entry(portalFtpFiles).State = System.Data.Entity.EntityState.Modified;
                            }

                            portal.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpDownloader:" + "downloader:updateSingleCharge:" + e.Message);
                Program.logs.Error("downloader:updateSingleCharge:", e);
                return;
            }
        }

        private string getFtpUrl(string winDirectory)
        {
            try
            {
                winDirectory = winDirectory.Replace("/", "\\");
                //create directory if not does exist
                if (!this.createDirectory(Properties.Settings.Default.LocalPath, winDirectory))
                    return null;

                string ftpDirectory = winDirectory.Replace("\\", "/");
                if (ftpDirectory.StartsWith("/"))
                    ftpDirectory = ftpDirectory.Remove(0, 1);
                if (ftpDirectory.EndsWith("/"))
                    ftpDirectory = ftpDirectory.Remove(winDirectory.Length - 1, 1);

                string ftpURL = "ftp://" + this.ServerFtpIP + "/" + ftpDirectory + "/";
                return ftpURL;
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpDownloader:" + "downloader:getFtpUrl:" + e.Message);
                Program.logs.Error("downloader:getFtpUrl:", e);
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="winDirectory"></param>
        /// <param name="operatorSIDs">Operators service Id for example 9951 for achar if specfied it means we should get all file which starts with this sid no matter if we get before or not</param>
        /// <returns></returns>
        private List<FtpFile> downloadNewFiles(string winDirectory, string[] operatorSIDs, bool downloadAnyway)
        {
            try
            {
                string localPath = Properties.Settings.Default.LocalPath;
                if (localPath.EndsWith("\\"))
                    localPath = localPath.Remove(localPath.Length - 1, 1);

                //string ftpDirectory = getFtpUrl(winDirectory);
                string ftpUrl = getFtpUrl(winDirectory);
                if (string.IsNullOrEmpty(ftpUrl)) return null;

                int i;
                List<FtpFile> newFtpFiles = this.detectNewFtpFiles(localPath + "\\" + winDirectory, ftpUrl, operatorSIDs, downloadAnyway);
                if (newFtpFiles == null) return newFtpFiles;
                if (newFtpFiles != null && newFtpFiles.Count > 0)
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "MCIFtpDownloader:" + newFtpFiles.Count.ToString() + " new files have been detected for" + winDirectory);
                    Program.logs.Info(newFtpFiles.Count.ToString() + " new files have been detected");
                }
                for (i = 0; i <= newFtpFiles.Count - 1; i++)
                {
                    Program.logs.Info("downloadNewFiles:downloadFile " + newFtpFiles[i].ftpFilePath + " is started");
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
                    Program.logs.Info("downloadNewFiles:downloadFile " + newFtpFiles[i].fileName + " is finished");
                }
                return newFtpFiles;
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpDownloader:" + "downloader:downloadNewFiles:" + e.Message);
                Program.logs.Error("downloader:downloadNewFiles:", e);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="windowsFilePath"></param>
        /// <param name="ftpDirectory"></param>
        /// <param name="operatorSIDs">Operators service Id for example 9951 for achar if specfied it means we should get all file which starts with this sid no matter if we get before or not</param>
        /// <returns></returns>
        private List<FtpFile> detectNewFtpFiles(string windowsFilePath, string ftpDirectory, string[] operatorSIDs, bool downloadAnyway)
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
                        if ((operatorSIDs != null && operatorSIDs.Where(o => fileName.StartsWith(o)).Count() == 0))
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

                        SharedLibrary.Models.MCISingleChargeFtpFile ftpFile = null;
                        if (operatorSIDs == null || !downloadAnyway)
                        {
                            //if operatorSIDs are not specified check if process this file before or not
                            //otherwise do not check and detect the file as a new file
                            ftpFile = portal.MCISingleChargeFtpFiles.Where(o => "ftp://" + o.serverIP + "/" + o.ftpDirectory + "/" + o.fileName == uri.AbsoluteUri && (string.IsNullOrEmpty(o.identifier) || o.identifier == identifier)).FirstOrDefault();
                        }

                        if (ftpFile == null)
                        {
                            Program.logs.Info("detectNewFtpFiles: new or modified File is " + uri.AbsoluteUri + " with identifier " + identifier);
                            //new file or modification detected
                            newFtpFiles.Add(new FtpFile(fileName, windowsFilePath + (windowsFilePath.EndsWith("\\") ? "" : "\\") + fileName
                                , ftpDirectory + (ftpDirectory.EndsWith("/") ? "" : "/") + fileName, identifier));
                        }

                        fileName = streamReader.ReadLine();
                    }
                }
                ftpResponse.Close();

                streamReader.Close();
                Program.logs.Info("detectNewFtpFiles:" + newFtpFiles.Count + " new or modified files have been detected ");
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
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "MCIFtpDownloader:" + "downloader:detectNewFilesUri:" + e.Message);
                Program.logs.Error("downloader:detectNewFilesUri:", e);

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
                        Program.logs.Info("createDirectory:" + dirPath);
                    }
                }
                
                return true;
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpDownloader:" + "createDirectory:" + e.Message);
                Program.logs.Error("createDirectory:", e);
                return false;
            }
        }
    }
}
