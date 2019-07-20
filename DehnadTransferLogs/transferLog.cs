using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DehnadTransferLogs
{
    class transferLog
    {
        string ServerUrl;// = "172.17.252.201";
        string ServerIP;// = "ftp://172.17.252.201";
        string FtpUser;// = "DEH";
        string FtpPassword;// = "d9H&*&123";

        public transferLog()
        {
            string userName, pwd;
            ServerUrl = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.ftpInternal53, SharedLibrary.HelpfulFunctions.enumServersActions.ftpInternalTransferLog, out userName, out pwd);
            Uri uri = new Uri(ServerUrl);
            ServerIP = uri.Host;

            FtpUser = userName;
            FtpPassword = pwd;
        }

        public void sb_transferLogFiles(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Program.Log(dirPath + " does not exist", true);
                return;
            }
            Program.Log("Start Transfering");

            string[] dirs = Directory.GetDirectories(dirPath);
            int i, j;
            //DateTime minDate 
            DateTime dateMinimum = DateTime.Now.Date.AddDays(-1 * Settings.Default.TransferNDaysBefore);
            string[] fileNames;
            string[] fileParts;
            DateTime dateFile;
            try
            {
                for (i = 0; i <= dirs.Length - 1; i++)
                {
                    if (Directory.Exists(dirs[i] + "\\logs"))
                    {
                        fileNames = Directory.GetFiles(dirs[i] + "\\logs", "*.log");
                        for (j = 0; j <= fileNames.Length - 1; j++)
                        {
                            fileParts = Path.GetFileNameWithoutExtension(fileNames[j]).Split('_');
                            if (fileParts.Length >= 1)
                            {
                                if (DateTime.TryParse(fileParts[1].Replace(".", " "), out dateFile))
                                {
                                    if (dateFile < dateMinimum)
                                    {
                                        if (this.fnc_transferFile(fileNames[j]))
                                        {
                                            try
                                            {
                                                File.Delete(fileNames[j]);
                                            }
                                            catch (Exception ex)
                                            {
                                                Program.Log("sb_transferLogFiles.DeleteFile", true, ex);
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log("sb_transferLogFiles", true, ex);
            }
            Program.Log("Transfering Ends!!!!");
        }

        private bool fnc_transferFile(string windowsFilePath)
        {
            string dir = "";
            int i;
            try
            {
                string[] fileParts = windowsFilePath.Split('\\');
                dir = string.Join("/", fileParts, 1, fileParts.Length - 2);
                if (this.ServerUrl.EndsWith("/")) this.ServerUrl = this.ServerUrl.Remove(this.ServerUrl.Length - 1, 1);

                using (WebClient client = new WebClient())
                {

                    client.Credentials = new NetworkCredential(this.FtpUser, this.FtpPassword);
                    client.UploadFile(this.ServerUrl + "/" + dir + "/" + Path.GetFileName(windowsFilePath), WebRequestMethods.Ftp.UploadFile, windowsFilePath);
                    Program.Log("File Uploaded:" + windowsFilePath);
                }

                return true;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    FtpWebResponse response = (FtpWebResponse)ex.Response;
                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        Program.Log("fnc_transferFile.WebException.inner.Folder does not exist", true, ex);
                        //directory does not exist
                        string ftpDir = dir;
                        string[] ftpDirParts = ftpDir.Split('/');
                        ftpDir = this.ServerUrl;
                        for (i = 0; i <= ftpDirParts.Length - 1; i++)
                        {
                            ftpDir = ftpDir + "/" + ftpDirParts[i];
                            if (!this.fnc_createDirectory(ftpDir))
                                return false;
                        }
                        return this.fnc_transferFile(windowsFilePath);
                    }
                }
                Program.Log("fnc_transferFile.WebException", true, ex);
                return false;
            }
            catch (Exception ex)
            {
                Program.Log("fnc_transferFile.Exception", true, ex);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ftpDirectory">ftp://....</param>
        /// <returns></returns>
        private bool fnc_createDirectory(string ftpDirectory)
        {
            try
            {


                FtpWebRequest ftpRequest = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpDirectory));
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                ftpRequest.Credentials = new NetworkCredential(this.FtpUser, this.FtpPassword);
                try
                {
                    using (FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                    {
                        //folder exists
                        return true;
                    }
                }
                catch
                {
                    ftpRequest = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpDirectory));
                    ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                    ftpRequest.Credentials = new NetworkCredential(this.FtpUser, this.FtpPassword);
                    ftpRequest.UsePassive = true;
                    ftpRequest.UseBinary = true;
                    ftpRequest.KeepAlive = false;
                    FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();
                    Stream ftpStream = response.GetResponseStream();

                    ftpStream.Close();
                    response.Close();

                    return true;
                }
            }
            catch (Exception ex)
            {

                Program.Log("fnc_createDirectory.Exception", true, ex);
                return false;
            }
        }

    }
}
