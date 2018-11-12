using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DehnadMCISingleChargeFTPService
{
    class downloader
    {
        public void updateSingleCharge()
        {
            updateSingleCharge(DateTime.Now.ToString("yyyyMMdd"));
        }
        public void updateSingleCharge(string winDirectory)
        {
            try
            {
                List<FtpFile> newFtpFiles = this.downloadNewFiles(winDirectory);
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
                SharedShortCodeServiceLibrary.SharedModel.Singlecharge singleCharge;
                SharedLibrary.Models.MCISingleChargeFtpFile portalFtpFiles;
                using (var portal = new SharedLibrary.Models.PortalEntities())
                {
                    for (i = 0; i <= newFtpFiles.Count - 1; i++)
                    {
                        chargeCount = 0;
                        Program.logs.Info("updateSingleCharge:Read File " + newFtpFiles[i]);
                        using (sr = new StreamReader(newFtpFiles[i].winFilePath))
                        {
                            str = sr.ReadToEnd();
                            props = charge.GetType().GetProperties().Where(o => o.Name.Contains("_")).Select(o => o.Name).ToList();
                            for (j = 0; j <= props.Count - 1; j++)
                            {
                                //e.g. replace trans-id with trans_id
                                str = str.Replace(props[j].Replace("_", "-"), props[j]);
                            }
                            lines = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                            Program.logs.Info("updateSingleCharge:Read File with " + newFtpFiles[i] + " lines");
                            for (j = 0; j <= lines.Length - 1; j++)
                            {
                                charge = Newtonsoft.Json.JsonConvert.DeserializeObject<ChargeInfo>(lines[j]);
                                if (charge.event_type == "1.5")
                                {
                                    chargeCount++;
                                    var serviceCode = portal.vw_servicesServicesInfo.Where(o => o.OperatorServiceId == charge.sid.ToString()).Select(o => o.ServiceCode).FirstOrDefault();
                                    using (SharedShortCodeServiceLibrary.SharedModel.ShortCodeServiceEntities serviceDB = new SharedShortCodeServiceLibrary.SharedModel.ShortCodeServiceEntities("Shared" + serviceCode + "Entities"))
                                    {
                                        if (serviceDB.Singlecharges.Where(o => o.ReferenceId == charge.trans_id).Count() == 0)
                                        {
                                            singleCharge = new SharedShortCodeServiceLibrary.SharedModel.Singlecharge();

                                            if (charge.status != 0)
                                                singleCharge.IsSucceeded = false;
                                            else
                                                singleCharge.IsSucceeded = true;

                                            singleCharge.ReferenceId = charge.trans_id;
                                            singleCharge.Price = charge.base_price_point.Value / 10;
                                            singleCharge.IsApplicationInformed = false;
                                            singleCharge.IsCalledFromInAppPurchase = false;
                                            singleCharge.Description = null;
                                            singleCharge.DateCreated = charge.datetime;
                                            singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(charge.datetime);
                                            singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(charge.msisdn);
                                            serviceDB.Singlecharges.Add(singleCharge);
                                        }
                                    }
                                }
                            }
                            string ftpUrl = this.getFtpUrl(winDirectory);
                            if (string.IsNullOrEmpty(ftpUrl)) return;

                            var ftpFile = portal.MCISingleChargeFtpFiles.Where(o => "ftp://" + o.serverIP + "/" + o.ftpDirectory + "/" + o.fileName == ftpUrl).FirstOrDefault();
                            if (ftpFile == null)
                            {
                                //new file or modification detected
                                portalFtpFiles = new SharedLibrary.Models.MCISingleChargeFtpFile();
                                portalFtpFiles.fileName = Path.GetFileName(newFtpFiles[i].winFilePath);
                                portalFtpFiles.ftpDirectory = winDirectory;
                                portalFtpFiles.processDateTime = DateTime.Now;
                                portalFtpFiles.serverIP = Properties.Settings.Default.ServerFtpIP;
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
                                portalFtpFiles.serverIP = Properties.Settings.Default.ServerFtpIP;
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

                string ftpURL = "ftp://" + Properties.Settings.Default.ServerFtpIP + "/" + ftpDirectory + "/";
                return ftpURL;
            }
            catch (Exception e)
            {
                Program.logs.Error("downloader:downloadNewFiles:", e);
                return null;
            }
        }
        private List<FtpFile> downloadNewFiles(string winDirectory)
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
                List<FtpFile> newFtpFiles = this.detectNewFtpFiles(winDirectory, ftpUrl);

                for (i = 0; i <= newFtpFiles.Count - 1; i++)
                {
                    Program.logs.Info("downloadNewFiles:downloadFile " + newFtpFiles[i] + " is started");
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(newFtpFiles[i].ftpFilePath);
                    request.Method = WebRequestMethods.Ftp.DownloadFile;
                    request.Credentials = new NetworkCredential(Properties.Settings.Default.FtpUser, Properties.Settings.Default.FtpPassword);

                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(responseStream);

                    //string filePath = localPath + "\\" + winDirectory + "\\" + newFilesUri[i]..Split('/')[newFilesUri[i].Split('/').Length - 1];
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
                    Program.logs.Info("downloadNewFiles:downloadFile " + newFtpFiles[i] + " is finished");
                }
                return newFtpFiles;
            }
            catch (Exception e)
            {
                Program.logs.Error("downloader:downloadNewFiles:", e);
                return null;
            }
        }

        private List<FtpFile> detectNewFtpFiles(string windowsFilePath, string ftpDirectory)
        {
            try
            {
                Uri uri = new Uri(ftpDirectory);
                //Uri uri = new Uri("ftp://172.17.252.201/" + directoryName + "/");
                FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(uri);
                ftpRequest.Credentials = new NetworkCredential(Properties.Settings.Default.FtpUser, Properties.Settings.Default.FtpPassword);
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                FtpWebResponse response;
                StreamReader streamReader = new StreamReader(ftpResponse.GetResponseStream());
                string fileName = streamReader.ReadLine();

                List<FtpFile> newFtpFiles = new List<FtpFile>();

                using (var portal = new SharedLibrary.Models.PortalEntities())
                {
                    while (!string.IsNullOrEmpty(fileName))
                    {
                        uri = new Uri(ftpDirectory + fileName);
                        ftpRequest = (FtpWebRequest)WebRequest.Create(uri);
                        ftpRequest.Credentials = new NetworkCredential(Properties.Settings.Default.FtpUser, Properties.Settings.Default.FtpPassword);
                        ftpRequest.Proxy = null;
                        ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;
                        response = (FtpWebResponse)ftpRequest.GetResponse();
                        long size = response.ContentLength;
                        response.Close();

                        uri = new Uri(ftpDirectory + fileName);
                        ftpRequest = (FtpWebRequest)WebRequest.Create(uri);
                        ftpRequest.Credentials = new NetworkCredential(Properties.Settings.Default.FtpUser, Properties.Settings.Default.FtpPassword);
                        ftpRequest.Proxy = null;
                        ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                        response = (FtpWebResponse)ftpRequest.GetResponse();
                        var modifiedTime = response.LastModified;
                        response.Close();
                        string identifier = size + ";" + modifiedTime.ToString();

                        var ftpFile = portal.MCISingleChargeFtpFiles.Where(o => "ftp://" + o.serverIP + "/" + o.ftpDirectory + "/" + o.fileName == uri.AbsoluteUri && (string.IsNullOrEmpty(o.identifier) || o.identifier == identifier)).FirstOrDefault();
                        if (ftpFile == null)
                        {
                            Program.logs.Info("detectNewFtpFiles: new or modified File is " + uri.AbsoluteUri + " with identifier " + identifier);
                            //new file or modification detected
                            newFtpFiles.Add(new FtpFile(windowsFilePath + (windowsFilePath.EndsWith("\\") ? "" : "\\") + fileName
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
                    }
                }
                Program.logs.Error("createDirectory:" + dirPath);
                return true;
            }
            catch (Exception e)
            {
                Program.logs.Error("createDirectory:", e);
                return false;
            }
        }
    }
}
