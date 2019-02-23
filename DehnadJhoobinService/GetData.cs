using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using SharedLibrary.Models;
using System.IO;
using System.Threading;
using System.Net.Http;
using CsvHelper;
using System.Net;

namespace DehnadJhoobinService
{
    public class GetData
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Dictionary<string, string> serviceIdsDic = new Dictionary<string, string>()
                {
                    { "TahChin", "262091"},
                    { "PorShetab", "268480"},
                    { "MusicYad", "267227"},
                    { "Medad", "269482"},
                    { "Dambel", "270281"},
                    { "Pin", "273329"},
                };

        public static Dictionary<string, string> marketsDic = new Dictionary<string, string>()
                {
                    { "ParsHub", "1"},
                    { "Charkhooneh", "2"},
                    { "ICharkhooneh", "3"},
                };
        public void GetJoobinData()
        {
            try
            {
                var jhoobinSettings = SharedLibrary.ServiceHandler.GetJhoobinSettings();
                var filePath = DehnadJhoobinService.Properties.Settings.Default.TempPath;
                var cookie = jhoobinSettings.Cookie;
                var viewState = jhoobinSettings.SubscriptionsViewState;
                var marketName = "Charkhooneh";
                //var date = DateTime.Now;
                var d = DateTime.Now.AddDays(-400);
                for (int i = 0; i < 450; i++)
                {
                    var date = d.AddDays(i);
                    if (date.Date < DateTime.Parse("2017-09-25"))
                        continue;
                    logs.Info(date);
                    if (DateTime.Now.Date < date.Date)
                        break;
                    foreach (var item in serviceIdsDic)
                    {
                        var persianDate = SharedLibrary.Date.GetPersianDate(date);
                        var folderPath = filePath + persianDate + " " + item.Key + @"\";
                        var fileName = persianDate + " " + item.Key;
                        string zipPath = filePath + fileName + ".zip";
                        bool exists = System.IO.Directory.Exists(folderPath);
                        if (exists)
                            Directory.Delete(folderPath, true);

                        persianDate = persianDate.Replace("-", "/");
                        var isSucceed = GetSubscriptionsDetailReportZipFile(item.Key, persianDate, marketName, cookie, viewState, filePath).Result;
                        if (isSucceed)
                        {
                            isSucceed = DecompressFromZipFile(zipPath, folderPath);
                            if (isSucceed)
                            {
                                var serviceCode = "Jhoobin" + item.Key;
                                JhoobinSubscriptionsDetailLogs(folderPath, serviceCode, jhoobinSettings);
                            }
                        }
                        else
                        {
                            logs.Info("Cannot get data from jhoobin, check cookie and viewstate: cookie=" + cookie + " viewState=" + viewState);
                        }
                        exists = System.IO.Directory.Exists(folderPath);
                        if (exists)
                            Directory.Delete(folderPath, true);

                        bool fileExists = System.IO.File.Exists(zipPath);
                        if (fileExists)
                            File.Delete(zipPath);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in GetJoobinData: ", e);
            }
        }

        public static async Task<bool> GetSubscriptionsDetailReportZipFile(string serviceName, string persianDate, string marketName, string cookie, string viewState, string filePath)
        {
            await Task.Delay(10);
            try
            {
                var serviceId = serviceIdsDic[serviceName];
                var marketId = marketsDic[marketName];
                var escapedPersianDate = persianDate;
                var fileName = string.Format("{0} {1}.zip", persianDate.Replace("/", "-"), serviceName);
                using (var client = new HttpClient(new HttpClientHandler() { UseCookies = false }))
                {
                    ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
                    var url = "https://seller.jhoobin.com/main/reportsubscribersdetail.jsf";
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                    requestMessage.Headers.TryAddWithoutValidation("cookie", cookie);
                    using (var response = await client.SendAsync(requestMessage))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var contentStream = await response.Content.ReadAsStreamAsync();
                        }
                        else
                        {
                            logs.Info("cannot get file from jhoobin(Get): responseCode=" + response);
                            return false;
                        }
                    }
                }
                Thread.Sleep(5 * 1000);
                using (var client = new HttpClient(new HttpClientHandler() { UseCookies = false }))
                {
                    ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
                    var url = "https://seller.jhoobin.com/main/reportsubscribersdetail.jsf";
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                    requestMessage.Headers.TryAddWithoutValidation("cookie", cookie);
                    var requestContent = string.Format("form=form&form%3Aj_idt158_focus=&form%3Aj_idt158_input={3}&form%3Ainapp_focus=&form%3Ainapp_input={0}&form%3Aj_idt163={1}&form%3Aj_idt165=&javax.faces.ViewState={2}", serviceId, escapedPersianDate, viewState, marketId);
                    requestMessage.Content = new StringContent(requestContent, Encoding.UTF8, "application/x-www-form-urlencoded");
                    using (var response = await client.SendAsync(requestMessage))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var contentStream = await response.Content.ReadAsStreamAsync();
                            using (var fileStream = new FileStream(filePath + fileName, FileMode.Create, FileAccess.Write))
                            {
                                contentStream.CopyTo(fileStream);
                            }
                        }
                        else
                        {
                            logs.Info("cannot get file from jhoobin(Post): responseCode=" + response);
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetSubscriptionsDetailReportZipFile: ", e);
                return false;
            }
            return true;
        }

        public static bool DecompressFromZipFile(string zipPath, string extractionPath)
        {
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractionPath);
                return true;
            }
            catch (Exception e)
            {
                logs.Error("Exception in DecompressFromZipFile-" + zipPath + ":", e);
                return false;
            }
        }

        public static bool JhoobinSubscriptionsDetailLogs(string folderPath, string serviceCode, SharedLibrary.Models.JhoobinSetting jhoobinSettings)
        {
            try
            {
                var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(serviceCode);
                var path = folderPath + @"newSubs.csv";
                var result = new List<string>();
                string value = "";

                using (TextReader fileReader = File.OpenText(path))
                {
                    fileReader.ReadLine();
                    fileReader.ReadLine();
                    var csv = new CsvReader(fileReader);
                    //csv.Configuration.HasHeaderRecord = true;
                    while (csv.Read())
                    {
                        var token = "";
                        var mobileNumber = "";
                        var startTime = "";
                        var expiryTime = "";
                        var autoRenew = "";
                        var cancelReason = "";
                        var cancelChannel = "";
                        var developerPayload = "";
                        for (int i = 0; csv.TryGetField<string>(i, out value); i++)
                        {
                            if (i == 0)
                                token = value;
                            else if (i == 1)
                                mobileNumber = value;
                            else if (i == 2)
                                startTime = value;
                            else if (i == 3)
                                expiryTime = value;
                            else if (i == 4)
                                autoRenew = value;
                            else if (i == 5)
                                cancelReason = value;
                            else if (i == 6)
                                cancelChannel = value;
                            else if (i == 7)
                                developerPayload = value;
                        }
                        if (token != "")
                        {
                            var message = new SharedLibrary.Models.MessageObject();
                            message.MobileNumber = mobileNumber;
                            message.ShortCode = "0";
                            message.Content = token;
                            message.MobileOperator = 2;
                            message.OperatorPlan = 0;
                            SharedLibrary.SubscriptionHandler.HandleSubscriptionContent(message, service, false);
                            var subscriber = SharedLibrary.SubscriptionHandler.GetSubscriber(mobileNumber, service.Id);
                            startTime = startTime.Replace("/", "-");
                            var splitedStartTime = startTime.Split(' ');
                            var georgianDate = SharedLibrary.Date.GetGregorianDate(splitedStartTime[0]);
                            var georgianDateTime = SharedLibrary.Date.GetGregorianDateTime(startTime);
                            subscriber.ActivationDate = georgianDateTime;
                            subscriber.PersianActivationDate = splitedStartTime[0];
                            using (var entity = new PortalEntities())
                            {
                                entity.Entry(subscriber).State = EntityState.Modified;
                                entity.SaveChanges();
                                var history = SharedLibrary.SubscriptionHandler.GetLastInsertedSubscriberHistory(message.MobileNumber, service.Id);
                                history.Date = georgianDate.Date;
                                history.Time = georgianDateTime.TimeOfDay;
                                history.DateTime = georgianDateTime;
                                history.PersianDateTime = startTime;
                                entity.Entry(history).State = EntityState.Modified;
                                entity.SaveChanges();
                            }
                            result.Add("Register " + serviceCode + ": MobileNumber=" + mobileNumber + ",date=" + georgianDateTime + ",persianDate=" + startTime + Environment.NewLine);
                        }
                    }
                }


                path = folderPath + @"unSubs.csv";
                result = new List<string>();
                value = "";
                using (TextReader fileReader = File.OpenText(path))
                {
                    fileReader.ReadLine();
                    fileReader.ReadLine();
                    var csv = new CsvReader(fileReader);
                    //csv.Configuration.HasHeaderRecord = true;
                    while (csv.Read())
                    {
                        var token = "";
                        var mobileNumber = "";
                        var startTime = "";
                        var expiryTime = "";
                        var autoRenew = "";
                        var cancelReason = "";
                        var cancelChannel = "";
                        var developerPayload = "";
                        for (int i = 0; csv.TryGetField<string>(i, out value); i++)
                        {
                            if (i == 0)
                                token = value;
                            else if (i == 1)
                                mobileNumber = value;
                            else if (i == 2)
                                startTime = value;
                            else if (i == 3)
                                expiryTime = value;
                            else if (i == 4)
                                autoRenew = value;
                            else if (i == 5)
                                cancelReason = value;
                            else if (i == 6)
                                cancelChannel = value;
                            else if (i == 7)
                                developerPayload = value;
                        }
                        if (token != "")
                        {
                            var subscriber = SharedLibrary.SubscriptionHandler.GetSubscriber(mobileNumber, service.Id);
                            if (subscriber == null)
                            {
                                var message = new SharedLibrary.Models.MessageObject();
                                message.MobileNumber = mobileNumber;
                                message.ShortCode = "0";
                                message.Content = token;
                                message.MobileOperator = 2;
                                message.OperatorPlan = 0;
                                SharedLibrary.SubscriptionHandler.HandleSubscriptionContent(message, service, false);

                                subscriber = SharedLibrary.SubscriptionHandler.GetSubscriber(mobileNumber, service.Id);
                                startTime = startTime.Replace("/", "-");
                                var splitedStartTime = startTime.Split(' ');
                                var georgianDate = SharedLibrary.Date.GetGregorianDate(splitedStartTime[0]);
                                var georgianDateTime = SharedLibrary.Date.GetGregorianDateTime(startTime);
                                subscriber.ActivationDate = georgianDateTime;
                                using (var entity = new PortalEntities())
                                {
                                    subscriber.PersianActivationDate = splitedStartTime[0];
                                    entity.Entry(subscriber).State = EntityState.Modified;
                                    entity.SaveChanges();
                                    var history = SharedLibrary.SubscriptionHandler.GetLastInsertedSubscriberHistory(message.MobileNumber, service.Id);
                                    history.Date = georgianDate.Date;
                                    history.Time = georgianDateTime.TimeOfDay;
                                    history.DateTime = georgianDateTime;
                                    history.PersianDateTime = startTime;
                                    entity.Entry(history).State = EntityState.Modified;
                                    entity.SaveChanges();
                                }
                                result.Add("Register " + serviceCode + ": MobileNumber=" + mobileNumber + ",date=" + georgianDateTime + ",persianDate=" + startTime + Environment.NewLine);
                            }
                            var message2 = new SharedLibrary.Models.MessageObject();
                            message2.MobileNumber = mobileNumber;
                            message2.ShortCode = "0";
                            message2.Content = cancelChannel;
                            message2.MobileOperator = 2;
                            message2.OperatorPlan = 0;
                            SharedLibrary.SubscriptionHandler.HandleSubscriptionContent(message2, service, true);

                            expiryTime = expiryTime.Replace("/", "-");
                            var splitedExpiryTime = expiryTime.Split(' ');
                            var georgianDate2 = SharedLibrary.Date.GetGregorianDate(splitedExpiryTime[0]);
                            var georgianDateTime2 = SharedLibrary.Date.GetGregorianDateTime(expiryTime);
                            subscriber.DeactivationDate = georgianDateTime2;
                            subscriber.PersianDeactivationDate = splitedExpiryTime[0];
                            using (var entity = new PortalEntities())
                            {
                                entity.Entry(subscriber).State = EntityState.Modified;
                                entity.SaveChanges();

                                var history2 = SharedLibrary.SubscriptionHandler.GetLastInsertedSubscriberHistory(message2.MobileNumber, service.Id);
                                history2.Date = georgianDate2.Date;
                                history2.Time = georgianDateTime2.TimeOfDay;
                                history2.DateTime = georgianDateTime2;
                                history2.PersianDateTime = expiryTime;
                                entity.Entry(history2).State = EntityState.Modified;
                                entity.SaveChanges();
                            }
                            result.Add("Unsub " + serviceCode + ": MobileNumber=" + mobileNumber + ",date=" + georgianDateTime2 + ",persianDate=" + expiryTime + Environment.NewLine);
                        }
                    }
                    //File.AppendAllLines(DehnadJhoobinService.Properties.Settings.Default.TempPath + "result.txt", result);
                }
                return true;
            }
            catch (Exception e)
            {
                logs.Error("Exception in JoobinSubscriptionsLogs-" + folderPath + ":", e);
                return false;
            }
        }
    }
}
