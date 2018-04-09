using OfficeOpenXml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadReceiveProcessorService
{
    class Ftp
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void TelepromoIncomeReport()
        {
            try
            {
                TelepromoGetIncome(typeof(MenchBazLibrary.Models.MenchBazEntities), "MenchBaz");
                TelepromoGetIncome(typeof(ShenoYadLibrary.Models.ShenoYadEntities), "ShenoYad");
                TelepromoGetIncome(typeof(TamlyLibrary.Models.TamlyEntities), "Tamly");
                TelepromoGetIncome(typeof(JabehAbzarLibrary.Models.JabehAbzarEntities), "JabehAbzar");
                TelepromoGetIncome(typeof(FitShowLibrary.Models.FitShowEntities), "FitShow");
                TelepromoGetIncome(typeof(TakavarLibrary.Models.TakavarEntities), "Takavar");
                TelepromoGetIncome(typeof(DonyayeAsatirLibrary.Models.DonyayeAsatirEntities), "DonyayeAsatir");
                TelepromoGetIncome(typeof(SoltanLibrary.Models.SoltanEntities), "Soltan");
                TelepromoGetIncome(typeof(AvvalPodLibrary.Models.AvvalPodEntities), "AvvalPod");
                TelepromoGetIncome(typeof(AvvalYadLibrary.Models.AvvalYadEntities), "AvvalYad");
                TelepromoGetIncome(typeof(DezhbanLibrary.Models.DezhbanEntities), "Dezhban");
                TelepromoGetIncome(typeof(AvvalPod500Library.Models.AvvalPod500Entities), "AvvalPod500");
                TelepromoGetIncome(typeof(BehAmooz500Library.Models.BehAmooz500Entities), "BehAmooz500");
                TelepromoGetIncome(typeof(ShenoYad500Library.Models.ShenoYad500Entities), "ShenoYad500");
                TelepromoGetIncome(typeof(Tamly500Library.Models.Tamly500Entities), "Tamly500");
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoIncomeReport:", e);
            }
        }

        public static void TelepromoGetIncome(Type entityType, string serviceCode)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-10).Date;
                string filePath = @"E:\ImiFtps\";
                string fileArchivePath = @"E:\ImiFtps\Archive\";
                using (dynamic entity = Activator.CreateInstance(entityType))
                {
                    var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(serviceCode);
                    var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                    var reports = ((IEnumerable)entity.DailyStatistics).Cast<dynamic>().Where(o => o.Date >= startDate && (o.SumOfSinglechargeSuccessfulCharge == null || o.SumOfSinglechargeSuccessfulCharge == 0)).ToList();
                    foreach (var report in reports)
                    {
                        var date = report.Date.ToString("yyyyMMdd");
                        bool isSucceed = false;
                        int numberOfTries = 1;
                        string uri = String.Format("http://10.20.9.135:8600/ftp/{0}-{1}.txt.bz2", date, serviceInfo.AggregatorServiceId);
                        var fileName = date + "-" + serviceInfo.AggregatorServiceId.ToString() + ".txt.bz2";
                        var imiBz2FileUri = filePath + fileName;
                        if (File.Exists(imiBz2FileUri))
                        {
                            File.Delete(imiBz2FileUri);
                        }
                        while (isSucceed == false && numberOfTries < 1000)
                        {
                            isSucceed = SharedLibrary.HelpfulFunctions.DownloadFileFromWeb(uri, filePath);
                            numberOfTries++;
                        }
                        if (isSucceed == false)
                            continue;

                        var decompressedFileName = imiBz2FileUri.Replace(".bz2", "");
                        if (File.Exists(decompressedFileName))
                        {
                            File.Delete(decompressedFileName);
                        }
                        SharedLibrary.HelpfulFunctions.DecompressFromBZ2File(imiBz2FileUri);
                        var imiDataList = SharedLibrary.HelpfulFunctions.ReadImiDataFile(decompressedFileName);
                        var result = SharedLibrary.HelpfulFunctions.GetIncomeAndSubscriptionsFromImiDataFile(imiDataList, Service.prefix);
                        report.SumOfSinglechargeSuccessfulCharge = result["sumOfCharges"];
                        report.SumOfSinglechargeSuccessfulPostpaidCharge = result["postpaidCharges"];
                        report.SumOfSinglechargeSuccessfulPrepaidCharge = result["prepaidCharges"];
                        entity.Entry(report).State = System.Data.Entity.EntityState.Modified;
                        entity.SaveChanges();
                        ExportTelepromoIncomeToExcel(serviceCode, report.PersianDate, result);
                        SharedLibrary.HelpfulFunctions.DeleteFile(decompressedFileName);
                        var archiveUri = fileArchivePath + fileName;
                        if (File.Exists(archiveUri))
                        {
                            File.Delete(archiveUri);
                        }
                        File.Move(imiBz2FileUri, archiveUri);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoGetIncome:", e);
            }
        }

        public static void ExportTelepromoIncomeToExcel(string serviceCode, string persianDate, Dictionary<string, int> imiDataDic)
        {
            var path = @"E:\ImiFtps\Excel Export\" + serviceCode + ".xlsx";
            bool isFileExists = File.Exists(path);
            FileInfo file = new FileInfo(path);
            try
            {
                using (var package = new ExcelPackage(file))
                {
                    ExcelWorkbook workBook = package.Workbook;
                    ExcelWorksheet worksheet = workBook.Worksheets.SingleOrDefault(w => w.Name == "Sheet1");
                    int totalRows = worksheet.Dimension.End.Row;
                    int totalCols = worksheet.Dimension.End.Column;
                    int rowNumber = totalRows + 1;
                    if (isFileExists == false)
                    {
                        worksheet.Cells[1, 1].Value = "تاریخ";
                        worksheet.Cells[1, 2].Value = "تعداد عضویت دائمی";
                        worksheet.Cells[1, 3].Value = "تعداد عضویت اعتباری";
                        worksheet.Cells[1, 4].Value = "تعداد کل عضویت";
                        worksheet.Cells[1, 5].Value = "تعداد لغو عضویت دائمی";
                        worksheet.Cells[1, 6].Value = "تعداد لغو عضویت اعتباری";
                        worksheet.Cells[1, 7].Value = "تعداد کل لغو عضویت";
                        worksheet.Cells[1, 8].Value = "مجموع درآمد دائمی";
                        worksheet.Cells[1, 9].Value = "مجموع درآمد اعتباری";
                        worksheet.Cells[1, 10].Value = "مجموع درآمد";
                        rowNumber = 2;
                    }
                    worksheet.Cells[rowNumber, 1].Value = persianDate;
                    worksheet.Cells[rowNumber, 2].Value = imiDataDic["postpaidSubscriptions"];
                    worksheet.Cells[rowNumber, 3].Value = imiDataDic["prepaidSubscriptions"];
                    worksheet.Cells[rowNumber, 4].Value = imiDataDic["postpaidSubscriptions"] + imiDataDic["prepaidSubscriptions"];
                    worksheet.Cells[rowNumber, 5].Value = imiDataDic["postpaidUnsubscriptions"];
                    worksheet.Cells[rowNumber, 6].Value = imiDataDic["prepaidUnsubscriptions"];
                    worksheet.Cells[rowNumber, 7].Value = imiDataDic["postpaidUnsubscriptions"] + imiDataDic["prepaidUnsubscriptions"];
                    worksheet.Cells[rowNumber, 8].Value = imiDataDic["postpaidCharges"];
                    worksheet.Cells[rowNumber, 9].Value = imiDataDic["prepaidCharges"];
                    worksheet.Cells[rowNumber, 10].Value = imiDataDic["sumOfCharges"];

                    if (isFileExists)
                        package.Save();
                    else
                        package.SaveAs(file);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ExportTelepromoIncomeToExcel:" + e);
            }
        }

        //public void ProcessTelepromoFtpFiles()
        //{
        //    try
        //    {
        //        string filePath = @"E:\ImiFtps\";
        //        string yesterday = DateTime.Now.AddDays(-1).Date.ToString("yyyyMMdd");
        //        List<SharedLibrary.Models.ServiceInfo> serviceInfos = new List<SharedLibrary.Models.ServiceInfo>();
        //        List<SharedLibrary.Models.Service> services = new List<SharedLibrary.Models.Service>();
        //        using (var entity = new SharedLibrary.Models.PortalEntities())
        //        {
        //            var telepromoId = entity.Aggregators.Where(o => o.AggregatorName == "Telepromo").Select(o => o.Id).FirstOrDefault();
        //            serviceInfos = entity.ServiceInfoes.Where(o => o.AggregatorId == telepromoId).ToList();
        //            services = entity.Services.ToList();
        //        }
        //        foreach (var service in serviceInfos)
        //        {
        //            bool isSucceed = false;
        //            int numberOfTries = 1;
        //            string uri = String.Format("http://10.20.9.135:8600/ftp/{0}-{1}.txt.bz2", yesterday, service.AggregatorServiceId);
        //            while (isSucceed == false && numberOfTries < 1000)
        //            {
        //                isSucceed = SharedLibrary.HelpfulFunctions.DownloadFileFromWeb(uri, filePath);
        //                numberOfTries++;
        //            }
        //        }
        //        var files = Directory.GetFiles(filePath);
        //        var fileNames = files.Select(f => Path.GetFileName(f));
        //        foreach (var item in fileNames)
        //        {
        //            if (item.StartsWith(yesterday))
        //            {
        //                var imiBz2FileUri = filePath + item;
        //                var decompressedFileName = imiBz2FileUri.Replace(".bz2", "");
        //                SharedLibrary.HelpfulFunctions.DecompressFromBZ2File(imiBz2FileUri);
        //                var imiDataList = SharedLibrary.HelpfulFunctions.ReadImiDataFile(decompressedFileName);

        //                var serviceId = serviceInfos.FirstOrDefault(o => o.AggregatorServiceId.Contains(item)).ServiceId;
        //                var serviceCode = services.FirstOrDefault(o => o.Id == serviceId).ServiceCode;
        //                SaveSingleChargeFromImiFtp(serviceCode, imiDataList.Where(o => o.eventType == "1.5").ToList());
        //                SharedLibrary.HelpfulFunctions.DeleteFile(decompressedFileName);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in ProcessTelepromoFtpFiles:", e);
        //    }
        //}

        //public void SaveSingleChargeFromImiFtp(string serviceCode, List<SharedLibrary.Models.ImiData> imiDataList)
        //{
        //    try
        //    {
        //        if (serviceCode == "Soltan")
        //        {
        //            using (var entity = new SoltanLibrary.Models.SoltanEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new SoltanLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "DonyayeAsatir")
        //        {
        //            using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new DonyayeAsatirLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "AvvalYad")
        //        {
        //            using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new AvvalYadLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "AvvalPod")
        //        {
        //            using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new AvvalPodLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Dezhban")
        //        {
        //            using (var entity = new DezhbanLibrary.Models.DezhbanEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new DezhbanLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "FitShow")
        //        {
        //            using (var entity = new FitShowLibrary.Models.FitShowEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new FitShowLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "JabehAbzar")
        //        {
        //            using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new JabehAbzarLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "MenchBaz")
        //        {
        //            using (var entity = new MenchBazLibrary.Models.MenchBazEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new MenchBazLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "ShenoYad")
        //        {
        //            using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new ShenoYadLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Takavar")
        //        {
        //            using (var entity = new TakavarLibrary.Models.TakavarEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new TakavarLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //        else if (serviceCode == "Tamly")
        //        {
        //            using (var entity = new TamlyLibrary.Models.TamlyEntities())
        //            {
        //                foreach (var item in imiDataList)
        //                {
        //                    var singlecharge = new TamlyLibrary.Models.Singlecharge();
        //                    singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
        //                    singlecharge.DateCreated = item.datetime;
        //                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
        //                    singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
        //                    singlecharge.IsSucceeded = true;
        //                    singlecharge.Description = "channel:system";
        //                    singlecharge.IsApplicationInformed = false;
        //                    singlecharge.IsCalledFromInAppPurchase = false;
        //                    entity.Singlecharges.Add(singlecharge);
        //                }
        //                entity.SaveChanges();
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in SaveSingleChargeFromImiFtp:", e);
        //    }
        //}
    }
}
