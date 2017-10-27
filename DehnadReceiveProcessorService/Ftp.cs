using System;
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
        public void ProcessTelepromoFtpFiles()
        {
            try
            {
                string filePath = @"E:\ImiFtps\";
                string yesterday = DateTime.Now.AddDays(-1).Date.ToString("yyyyMMdd");
                List<SharedLibrary.Models.ServiceInfo> serviceInfos = new List<SharedLibrary.Models.ServiceInfo>();
                List<SharedLibrary.Models.Service> services = new List<SharedLibrary.Models.Service>();
                using (var entity = new SharedLibrary.Models.PortalEntities())
                {
                    var telepromoId = entity.Aggregators.Where(o => o.AggregatorName == "Telepromo").Select(o => o.Id).FirstOrDefault();
                    serviceInfos = entity.ServiceInfoes.Where(o => o.AggregatorId == telepromoId).ToList();
                    services = entity.Services.ToList();
                }
                foreach (var service in serviceInfos)
                {
                    bool isSucceed = false;
                    int numberOfTries = 1;
                    string uri = String.Format("http://10.20.9.135:8600/ftp/{0}-{1}.txt.bz2", yesterday, service.AggregatorServiceId);
                    while (isSucceed == false && numberOfTries < 1000)
                    {
                        isSucceed = SharedLibrary.HelpfulFunctions.DownloadFileFromWeb(uri, filePath);
                        numberOfTries++;
                    }
                }
                var files = Directory.GetFiles(filePath);
                var fileNames = files.Select(f => Path.GetFileName(f));
                foreach (var item in fileNames)
                {
                    if (item.StartsWith(yesterday))
                    {
                        var imiBz2FileUri = filePath + item;
                        var decompressedFileName = imiBz2FileUri.Replace(".bz2", "");
                        SharedLibrary.HelpfulFunctions.DecompressFromBZ2File(imiBz2FileUri);
                        var imiDataList = SharedLibrary.HelpfulFunctions.ReadImiDataFile(decompressedFileName);
                        var serviceId = serviceInfos.FirstOrDefault(o => o.AggregatorServiceId.Contains(item)).ServiceId;
                        var serviceCode = services.FirstOrDefault(o => o.Id == serviceId).ServiceCode;
                        SaveSingleChargeFromImiFtp(serviceCode, imiDataList.Where(o => o.eventType == "1.5").ToList());
                        SharedLibrary.HelpfulFunctions.DeleteFile(decompressedFileName);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ProcessTelepromoFtpFiles:", e);
            }
        }

        public void SaveSingleChargeFromImiFtp(string serviceCode, List<SharedLibrary.Models.ImiData> imiDataList)
        {
            try
            {
                if (serviceCode == "Soltan")
                {
                    using (var entity = new SoltanLibrary.Models.SoltanEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new SoltanLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
                else if (serviceCode == "DonyayeAsatir")
                {
                    using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new DonyayeAsatirLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
                else if (serviceCode == "AvvalYad")
                {
                    using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new AvvalYadLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
                else if (serviceCode == "Dezhban")
                {
                    using (var entity = new DezhbanLibrary.Models.DezhbanEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new DezhbanLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
                else if (serviceCode == "FitShow")
                {
                    using (var entity = new FitShowLibrary.Models.FitShowEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new FitShowLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
                else if (serviceCode == "JabehAbzar")
                {
                    using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new JabehAbzarLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
                else if (serviceCode == "MenchBaz")
                {
                    using (var entity = new MenchBazLibrary.Models.MenchBazEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new MenchBazLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
                else if (serviceCode == "ShenoYad")
                {
                    using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new ShenoYadLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
                else if (serviceCode == "Takavar")
                {
                    using (var entity = new TakavarLibrary.Models.TakavarEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new TakavarLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
                else if (serviceCode == "Tamly")
                {
                    using (var entity = new TamlyLibrary.Models.TamlyEntities())
                    {
                        foreach (var item in imiDataList)
                        {
                            var singlecharge = new TamlyLibrary.Models.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.msisdn);
                            singlecharge.DateCreated = item.datetime;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(item.datetime);
                            singlecharge.Price = (item.billedPricePoint != null && item.billedPricePoint.Value == 0) ? 0 : item.billedPricePoint.Value / 10;
                            singlecharge.IsSucceeded = true;
                            singlecharge.Description = "channel:system";
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            entity.Singlecharges.Add(singlecharge);
                        }
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SaveSingleChargeFromImiFtp:", e);
            }
        }
    }
}
