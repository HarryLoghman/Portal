using OfficeOpenXml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;

namespace DehnadReceiveProcessorService
{
    class Ftp
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //public static void GetImiFtpFiles(DateTime startDate)
        //{
        //    try
        //    {
        //        SessionOptions sessionOptions = new SessionOptions
        //        {
        //            Protocol = Protocol.Ftp,
        //            HostName = "172.17.252.201",
        //            UserName = "DEH",
        //            Password = "d9H&*&123",

        //        };

        //        using (Session session = new Session())
        //        {
        //            session.Open(sessionOptions);

        //            for (var selectedDate = startDate; selectedDate.Date <= DateTime.Now.Date; selectedDate = selectedDate.AddDays(1))
        //            {
        //                var dateString = selectedDate.ToString("yyyyMMdd");
        //                string subPath = string.Format(@"E:\ImiFtps\Mci Direct\{0}", dateString);

        //                bool exists = System.IO.Directory.Exists(subPath);
        //                if (exists)
        //                    continue;
        //                else
        //                    System.IO.Directory.CreateDirectory(subPath);
                        
        //                session.GetFiles(string.Format("/{0}/*", dateString), string.Format(@"E:\ImiFtps\Mci Direct\{0}\*", dateString)).Check();
        //                MakeMultipleFtpFilesToOneFilePerServiceAndProcess(string.Format(@"E:\ImiFtps\Mci Direct\{0}\", dateString));
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in GetImiFtpFiles: ", e);
        //    }
        //}

        //public static void MakeMultipleFtpFilesToOneFilePerServiceAndProcess(string path)
        //{
        //    try
        //    {
        //        var files = Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories);
        //        Dictionary<string, List<string>> filesDic = new Dictionary<string, List<string>>();
        //        foreach (var file in files)
        //        {
        //            var fileName = Path.GetFileName(file);
        //            var splittedFileName = fileName.Split('_');
        //            var serviceId = splittedFileName[0];
        //            var content = System.IO.File.ReadAllLines(file).ToList();
        //            if (filesDic.ContainsKey(serviceId))
        //            {
        //                var temp = filesDic[serviceId];
        //                temp.AddRange(content);
        //                filesDic[serviceId] = temp;
        //            }
        //            else
        //                filesDic[serviceId] = content;
        //        }
        //        foreach (string file in Directory.GetFiles(path, "*.txt"))
        //        {
        //            File.Delete(file);
        //        }

        //        foreach (KeyValuePair<string, List<string>> entry in filesDic)
        //        {
        //            File.WriteAllLines(path + entry.Key + ".txt", entry.Value);
        //            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromOperatorServiceId(entry.Key);
        //            if (serviceInfo != null)
        //            {
        //                var serviceCode = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceInfo.ServiceId).ServiceCode;
        //                var imiDataList = SharedLibrary.HelpfulFunctions.ReadImiDataFromMemory(serviceCode, entry.Value);
        //                ImiDataToSingleCharge(serviceCode, imiDataList);
        //            }
        //            else
        //            {
        //                logs.Info("No serviceInfo found!");
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in MakeMultipleFtpFilesToOneFilePerServiceAndProcess: ", e);
        //    }
        //}

        //public static void TelepromoDailyFtp()
        //{
        //    TelepromoGetDailyIncome("MenchBaz");
        //    TelepromoGetDailyIncome("ShenoYad");
        //    TelepromoGetDailyIncome("Tamly");
        //    TelepromoGetDailyIncome("JabehAbzar");
        //    TelepromoGetDailyIncome("FitShow");
        //    TelepromoGetDailyIncome("Takavar");
        //    TelepromoGetDailyIncome("DonyayeAsatir");
        //    TelepromoGetDailyIncome("Soltan");
        //    TelepromoGetDailyIncome("AvvalPod500");
        //    TelepromoGetDailyIncome("BehAmooz500");
        //    TelepromoGetDailyIncome("ShenoYad500");
        //    TelepromoGetDailyIncome("Tamly500");
        //    TelepromoGetDailyIncome("Aseman");
        //}

        //public static void TelepromoGetDailyIncome(string serviceCode)
        //{
        //    try
        //    {
        //        string filePath = @"E:\ImiFtps\";
        //        string fileArchivePath = @"E:\ImiFtps\Archive\";

        //        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(serviceCode);
        //        var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
        //        var date = DateTime.Now.ToString("yyyyMMdd");
        //        bool isSucceed = false;
        //        int numberOfTries = 1;
        //        //string uri = String.Format("http://10.20.9.135:8600/ftp/{0}-{1}.txt.bz2", date, serviceInfo.AggregatorServiceId);
        //        string uri = SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.telepromo, SharedLibrary.HelpfulFunctions.enumServersActions.ftp);
        //        var fileName = date + "-" + serviceInfo.AggregatorServiceId.ToString() + ".txt.bz2";
        //        var imiBz2FileUri = filePath + fileName;
        //        if (File.Exists(imiBz2FileUri))
        //        {
        //            File.Delete(imiBz2FileUri);
        //        }
        //        while (isSucceed == false && numberOfTries < 1000)
        //        {
        //            isSucceed = SharedLibrary.HelpfulFunctions.DownloadFileFromWeb(uri, filePath);
        //            numberOfTries++;
        //        }
        //        if (isSucceed == false)
        //            return;

        //        var decompressedFileName = imiBz2FileUri.Replace(".bz2", "");
        //        if (File.Exists(decompressedFileName))
        //        {
        //            File.Delete(decompressedFileName);
        //        }
        //        SharedLibrary.HelpfulFunctions.DecompressFromBZ2File(imiBz2FileUri);
        //        var imiDataList = SharedLibrary.HelpfulFunctions.ReadImiDataFile(decompressedFileName);
        //        ImiDataToSingleCharge(serviceCode, imiDataList);
        //        SharedLibrary.HelpfulFunctions.DeleteFile(decompressedFileName);
        //        var archiveUri = fileArchivePath + fileName;
        //        if (File.Exists(archiveUri))
        //        {
        //            File.Delete(archiveUri);
        //        }
        //        File.Move(imiBz2FileUri, archiveUri);
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in TelepromoGetDailyIncome:", e);
        //    }
        //}

        //public static void TelepromoDailyFtpTemp()
        //{
        //    var startDate = DateTime.Parse("2018-03-23");
        //    var endDate = DateTime.Parse("2018-06-21");
        //    int i = 0;
        //    while (true)
        //    {
        //        var d = startDate.AddDays(i);
        //        if (d >= endDate)
        //            break;
        //        try
        //        {
        //            var date = d.ToString("yyyyMMdd");
        //            logs.Info("TelepromoDailyFtpTemp date: " + date);
        //            var taskList = new List<Task>();

        //            //taskList.Add(TelepromoGetDailyIncomeTemp("MenchBaz", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("ShenoYad", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("Tamly", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("JabehAbzar", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("FitShow", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("Takavar", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("DonyayeAsatir", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("Soltan", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("AvvalPod500", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("BehAmooz500", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("ShenoYad500", date));
        //            taskList.Add(TelepromoGetDailyIncomeTemp("Tamly500", date));
        //            //taskList.Add(TelepromoGetDailyIncomeTemp("Aseman", date));
        //            Task.WaitAll(taskList.ToArray());
        //        }
        //        catch (Exception e)
        //        {
        //            logs.Error("TelepromoDailyFtpTemp: ", e);
        //        }
        //        i++;
        //    }
        //}

        //public static async Task TelepromoGetDailyIncomeTemp(string serviceCode, string date)
        //{
        //    logs.Info("TelepromoGetDailyIncomeTemp " + serviceCode + " " + date + " start");
        //    await Task.Delay(10);
        //    try
        //    {
        //        string filePath = @"E:\ImiFtps\";
        //        string fileArchivePath = @"E:\ImiFtps\Archive\";

        //        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(serviceCode);
        //        var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
        //        bool isSucceed = false;
        //        int numberOfTries = 1;
        //        //string uri = String.Format("http://10.20.9.135:8600/ftp/{0}-{1}.txt.bz2", date, serviceInfo.AggregatorServiceId);
        //        string uri = String.Format(SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.telepromo, SharedLibrary.HelpfulFunctions.enumServersActions.ftp), date, serviceInfo.AggregatorServiceId);
        //        var fileName = date + "-" + serviceInfo.AggregatorServiceId.ToString() + ".txt.bz2";
        //        var imiBz2FileUri = filePath + fileName;
        //        if (File.Exists(imiBz2FileUri))
        //        {
        //            File.Delete(imiBz2FileUri);
        //        }
        //        while (isSucceed == false && numberOfTries < 3)
        //        {
        //            isSucceed = SharedLibrary.HelpfulFunctions.DownloadFileFromWeb(uri, filePath);
        //            numberOfTries++;
        //        }
        //        if (isSucceed == false)
        //            return;
        //        if (!File.Exists(@"E:\ImiFtps\" + fileName))
        //            return;
        //        var decompressedFileName = imiBz2FileUri.Replace(".bz2", "");
        //        //if (File.Exists(decompressedFileName))
        //        //{
        //        //    File.Delete(decompressedFileName);
        //        //}
        //        SharedLibrary.HelpfulFunctions.DecompressFromBZ2File(imiBz2FileUri);
        //        var imiDataList = SharedLibrary.HelpfulFunctions.ReadImiDataFile(decompressedFileName);
        //        if (imiDataList.Count == 0)
        //            return;
        //        ImiDataToSingleChargeTemp(serviceCode, imiDataList);
        //        SharedLibrary.HelpfulFunctions.DeleteFile(decompressedFileName);
        //        var archiveUri = fileArchivePath + fileName;
        //        if (File.Exists(archiveUri))
        //        {
        //            File.Delete(archiveUri);
        //        }
        //        File.Move(imiBz2FileUri, archiveUri);
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in TelepromoGetDailyIncome:", e);
        //    }
        //    logs.Info("TelepromoGetDailyIncomeTemp " + serviceCode + " " + date + " end");
        //}

        //private static void ImiDataToSingleChargeTemp(string serviceCode, List<SharedLibrary.Models.ImiData> imiDataList)
        //{
        //    try
        //    {
        //        if (serviceCode == "Aseman")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Aseman"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "MenchBaz")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("MenchBaz"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "ShenoYad")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShenoYad"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "ShenoYad500")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShenoYad500"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Tamly")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Tamly"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Tamly500")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Tamly500"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "JabehAbzar")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("JabehAbzar"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "FitShow")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("FitShow"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Takavar")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Takavar"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "DonyayeAsatir")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("DonyayeAsatir"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Soltan")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Soltan"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "AvvalPod500")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("AvvalPod500"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "BehAmooz500")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("BehAmooz500"))
        //            {
        //                entity.Database.CommandTimeout = 240;
        //                var i = 0;
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        if (i > 1000)
        //                        {
        //                            entity.SaveChanges();
        //                            i = 0;
        //                        }
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                        i++;
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in ImiDataToSingleCharge: " + serviceCode + " : ", e);
        //    }
        //}

        //private static void ImiDataToSingleCharge(string serviceCode, List<SharedLibrary.Models.ImiData> imiDataList)
        //{
        //    try
        //    {
        //        if (serviceCode == "Aseman")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Aseman"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "MenchBaz")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("MenchBaz"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "ShenoYad")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShenoYad"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "ShenoYad500")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShenoYad500"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Tamly")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Tamly"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Tamly500")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Tamly500"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "JabehAbzar")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("JabehAbzar"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "FitShow")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("FitShow"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Takavar")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Takavar"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "DonyayeAsatir")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("DonyayeAsatir"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Soltan")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Soltan"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "AvvalPod500")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("AvvalPod500"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "BehAmooz500")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("BehAmooz500"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Soraty")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Soraty"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "ShahreKalameh")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShahreKalameh"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "DefendIran")
        //        {
        //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("DefendIran"))
        //            {
        //                foreach (var data in imiDataList)
        //                {
        //                    if (data.eventType == "1.5")
        //                    {
        //                        var isSingleChargeExists = entity.Singlecharges.FirstOrDefault(o => o.ReferenceId == data.transId);
        //                        if (isSingleChargeExists != null)
        //                            continue;

        //                        if (data.basePricePoint == null)
        //                            continue;
        //                        else if (data.basePricePoint == 0)
        //                            continue;
        //                        var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
        //                        if (data.status != 0)
        //                            singleCharge.IsSucceeded = false;
        //                        else
        //                            singleCharge.IsSucceeded = true;

        //                        singleCharge.ReferenceId = data.transId;
        //                        singleCharge.Price = data.basePricePoint.Value / 10;
        //                        singleCharge.IsApplicationInformed = false;
        //                        singleCharge.IsCalledFromInAppPurchase = false;
        //                        singleCharge.Description = null;
        //                        singleCharge.DateCreated = data.datetime;
        //                        singleCharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(data.datetime);
        //                        singleCharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
        //                        entity.Singlecharges.Add(singleCharge);
        //                    }
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in ImiDataToSingleCharge:", e);
        //    }
        //}

        //public void TelepromoIncomeReport()
        //{
        //    try
        //    {
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "MenchBaz");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "ShenoYad");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "Tamly");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "JabehAbzar");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "FitShow");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "Takavar");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "DonyayeAsatir");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "Soltan");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "AvvalPod");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "AvvalYad");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "Dezhban");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "AvvalPod500");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "BehAmooz500");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "ShenoYad500");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "Tamly500");
        //        TelepromoGetIncome(typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities), "Aseman");
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in TelepromoIncomeReport:", e);
        //    }
        //}

        //public static void TelepromoGetIncome(Type entityType, string serviceCode)
        //{
        //    try
        //    {
        //        var startDate = DateTime.Now.AddDays(-10).Date;
        //        string filePath = @"E:\ImiFtps\";
        //        string fileArchivePath = @"E:\ImiFtps\Archive\";
        //        using (dynamic entity = Activator.CreateInstance(entityType))
        //        {
        //            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(serviceCode);
        //            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
        //            var reports = ((IEnumerable<dynamic>)entity.DailyStatistics).Where(o => o.Date >= startDate && (o.SumOfSinglechargeSuccessfulCharge == null || o.SumOfSinglechargeSuccessfulCharge == 0)).ToList();
        //            foreach (var report in reports)
        //            {
        //                try
        //                {
        //                    var date = report.Date.ToString("yyyyMMdd");
        //                    bool isSucceed = false;
        //                    int numberOfTries = 1;
        //                    //string uri = String.Format("http://10.20.9.135:8600/ftp/{0}-{1}.txt.bz2", date, serviceInfo.AggregatorServiceId);
        //                    string uri = String.Format(SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.telepromo,SharedLibrary.HelpfulFunctions.enumServersActions.ftp), date, serviceInfo.AggregatorServiceId);
        //                    var fileName = date + "-" + serviceInfo.AggregatorServiceId.ToString() + ".txt.bz2";
        //                    var imiBz2FileUri = filePath + fileName;
        //                    if (File.Exists(imiBz2FileUri))
        //                    {
        //                        File.Delete(imiBz2FileUri);
        //                    }
        //                    while (isSucceed == false && numberOfTries < 1000)
        //                    {
        //                        isSucceed = SharedLibrary.HelpfulFunctions.DownloadFileFromWeb(uri, filePath);
        //                        numberOfTries++;
        //                    }
        //                    if (isSucceed == false)
        //                        continue;

        //                    var decompressedFileName = imiBz2FileUri.Replace(".bz2", "");
        //                    if (File.Exists(decompressedFileName))
        //                    {
        //                        File.Delete(decompressedFileName);
        //                    }
        //                    SharedLibrary.HelpfulFunctions.DecompressFromBZ2File(imiBz2FileUri);
        //                    var imiDataList = SharedLibrary.HelpfulFunctions.ReadImiDataFile(decompressedFileName);
        //                    var result = SharedLibrary.HelpfulFunctions.GetIncomeAndSubscriptionsFromImiDataFile(imiDataList, Service.prefix);
        //                    report.SumOfSinglechargeSuccessfulCharge = result["sumOfCharges"];
        //                    report.SumOfSinglechargeSuccessfulPostpaidCharge = result["postpaidCharges"];
        //                    report.SumOfSinglechargeSuccessfulPrepaidCharge = result["prepaidCharges"];
        //                    entity.Entry(report).State = System.Data.Entity.EntityState.Modified;
        //                    entity.SaveChanges();
        //                    ExportTelepromoIncomeToExcel(serviceCode, report.PersianDate, result);
        //                    SharedLibrary.HelpfulFunctions.DeleteFile(decompressedFileName);
        //                    var archiveUri = fileArchivePath + fileName;
        //                    if (File.Exists(archiveUri))
        //                    {
        //                        File.Delete(archiveUri);
        //                    }
        //                    File.Move(imiBz2FileUri, archiveUri);
        //                }
        //                catch (Exception e)
        //                {
        //                    logs.Error("Exception in TelepromoGetIncome foreach:", e);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in TelepromoGetIncome:", e);
        //    }
        //}

        //public static void ExportTelepromoIncomeToExcel(string serviceCode, string persianDate, Dictionary<string, int> imiDataDic)
        //{
        //    var path = @"E:\ImiFtps\Excel Export\" + serviceCode + ".xlsx";
        //    bool isFileExists = File.Exists(path);
        //    FileInfo file = new FileInfo(path);
        //    try
        //    {
        //        using (var package = new ExcelPackage(file))
        //        {
        //            ExcelWorkbook workBook = package.Workbook;
        //            ExcelWorksheet worksheet = workBook.Worksheets.SingleOrDefault(w => w.Name == "Sheet1");
        //            int totalRows = worksheet.Dimension.End.Row;
        //            int totalCols = worksheet.Dimension.End.Column;
        //            int rowNumber = totalRows + 1;
        //            if (isFileExists == false)
        //            {
        //                worksheet.Cells[1, 1].Value = "تاریخ";
        //                worksheet.Cells[1, 2].Value = "تعداد عضویت دائمی";
        //                worksheet.Cells[1, 3].Value = "تعداد عضویت اعتباری";
        //                worksheet.Cells[1, 4].Value = "تعداد کل عضویت";
        //                worksheet.Cells[1, 5].Value = "تعداد لغو عضویت دائمی";
        //                worksheet.Cells[1, 6].Value = "تعداد لغو عضویت اعتباری";
        //                worksheet.Cells[1, 7].Value = "تعداد کل لغو عضویت";
        //                worksheet.Cells[1, 8].Value = "مجموع درآمد دائمی";
        //                worksheet.Cells[1, 9].Value = "مجموع درآمد اعتباری";
        //                worksheet.Cells[1, 10].Value = "مجموع درآمد";
        //                rowNumber = 2;
        //            }
        //            worksheet.Cells[rowNumber, 1].Value = persianDate;
        //            worksheet.Cells[rowNumber, 2].Value = imiDataDic["postpaidSubscriptions"];
        //            worksheet.Cells[rowNumber, 3].Value = imiDataDic["prepaidSubscriptions"];
        //            worksheet.Cells[rowNumber, 4].Value = imiDataDic["postpaidSubscriptions"] + imiDataDic["prepaidSubscriptions"];
        //            worksheet.Cells[rowNumber, 5].Value = imiDataDic["postpaidUnsubscriptions"];
        //            worksheet.Cells[rowNumber, 6].Value = imiDataDic["prepaidUnsubscriptions"];
        //            worksheet.Cells[rowNumber, 7].Value = imiDataDic["postpaidUnsubscriptions"] + imiDataDic["prepaidUnsubscriptions"];
        //            worksheet.Cells[rowNumber, 8].Value = imiDataDic["postpaidCharges"];
        //            worksheet.Cells[rowNumber, 9].Value = imiDataDic["prepaidCharges"];
        //            worksheet.Cells[rowNumber, 10].Value = imiDataDic["sumOfCharges"];

        //            if (isFileExists)
        //                package.Save();
        //            else
        //                package.SaveAs(file);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in ExportTelepromoIncomeToExcel:" + e);
        //    }
        //}

        ////public void ProcessTelepromoFtpFiles()
        ////{
        ////    try
        ////    {
        ////        string filePath = @"E:\ImiFtps\";
        ////        string yesterday = DateTime.Now.AddDays(-1).Date.ToString("yyyyMMdd");
        ////        List<SharedLibrary.Models.ServiceInfo> serviceInfos = new List<SharedLibrary.Models.ServiceInfo>();
        ////        List<SharedLibrary.Models.Service> services = new List<SharedLibrary.Models.Service>();
        ////        using (var entity = new SharedLibrary.Models.PortalEntities())
        ////        {
        ////            var telepromoId = entity.Aggregators.Where(o => o.AggregatorName == "Telepromo").Select(o => o.Id).FirstOrDefault();
        ////            serviceInfos = entity.ServiceInfoes.Where(o => o.AggregatorId == telepromoId).ToList();
        ////            services = entity.Services.ToList();
        ////        }
        ////        foreach (var service in serviceInfos)
        ////        {
        ////            bool isSucceed = false;
        ////            int numberOfTries = 1;
        ////            string uri = String.Format("http://10.20.9.135:8600/ftp/{0}-{1}.txt.bz2", yesterday, service.AggregatorServiceId);
        ////            while (isSucceed == false && numberOfTries < 1000)
        ////            {
        ////                isSucceed = SharedLibrary.HelpfulFunctions.DownloadFileFromWeb(uri, filePath);
        ////                numberOfTries++;
        ////            }
        ////        }
        ////        var files = Directory.GetFiles(filePath);
        ////        var fileNames = files.Select(f => Path.GetFileName(f));
        ////        foreach (var item in fileNames)
        ////        {
        ////            if (item.StartsWith(yesterday))
        ////            {
        ////                var imiBz2FileUri = filePath + item;
        ////                var decompressedFileName = imiBz2FileUri.Replace(".bz2", "");
        ////                SharedLibrary.HelpfulFunctions.DecompressFromBZ2File(imiBz2FileUri);
        ////                var imiDataList = SharedLibrary.HelpfulFunctions.ReadImiDataFile(decompressedFileName);

        ////                var serviceId = serviceInfos.FirstOrDefault(o => o.AggregatorServiceId.Contains(item)).ServiceId;
        ////                var serviceCode = services.FirstOrDefault(o => o.Id == serviceId).ServiceCode;
        ////                SaveSingleChargeFromImiFtp(serviceCode, imiDataList.Where(o => o.eventType == "1.5").ToList());
        ////                SharedLibrary.HelpfulFunctions.DeleteFile(decompressedFileName);
        ////            }
        ////        }
        ////    }
        ////    catch (Exception e)
        ////    {
        ////        logs.Error("Exception in ProcessTelepromoFtpFiles:", e);
        ////    }
        ////}

        ////public void SaveSingleChargeFromImiFtp(string serviceCode, List<SharedLibrary.Models.ImiData> imiDataList)
        ////{
        ////    try
        ////    {
        ////        if (serviceCode == "Soltan")
        ////        {
        ////            using (var entity = new SoltanLibrary.Models.SoltanEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new SoltanLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "DonyayeAsatir")
        ////        {
        ////            using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new DonyayeAsatirLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "AvvalYad")
        ////        {
        ////            using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new AvvalYadLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "AvvalPod")
        ////        {
        ////            using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new AvvalPodLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "Dezhban")
        ////        {
        ////            using (var entity = new DezhbanLibrary.Models.DezhbanEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new DezhbanLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "FitShow")
        ////        {
        ////            using (var entity = new FitShowLibrary.Models.FitShowEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new FitShowLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "JabehAbzar")
        ////        {
        ////            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("JabehAbzar"))
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new JabehAbzarLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "MenchBaz")
        ////        {
        ////            using (var entity = new MenchBazLibrary.Models.MenchBazEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new MenchBazLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "ShenoYad")
        ////        {
        ////            using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new ShenoYadLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "Takavar")
        ////        {
        ////            using (var entity = new TakavarLibrary.Models.TakavarEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new TakavarLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////        else if (serviceCode == "Tamly")
        ////        {
        ////            using (var entity = new TamlyLibrary.Models.TamlyEntities())
        ////            {
        ////                foreach (var item in imiDataList)
        ////                {
        ////                    var singlecharge = new TamlyLibrary.Models.Singlecharge();
        ////                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        ////                    singlecharge.DateCreated = item.datetime;
        ////                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        ////                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        ////                    singlecharge.IsSucceeded = true;
        ////                    singlecharge.Description = "channel:system";
        ////                    singlecharge.IsApplicationInformed = false;
        ////                    singlecharge.IsCalledFromInAppPurchase = false;
        ////                    entity.Singlecharges.Add(singlecharge);
        ////                }
        ////                entity.SaveChanges();
        ////            }
        ////        }
        ////    }
        ////    catch (Exception e)
        ////    {
        ////        logs.Error("Exception in SaveSingleChargeFromImiFtp:", e);
        ////    }
        ////}
    }
}
