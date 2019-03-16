using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DehnadFtpSyncAndChargingService.MobinOne
{
    class SyncMobinOne
    {
        string v_serverUrl;// = "http://cp.mobinone.org:33065/?username=dehnad&password=OC56i4yA&date=20180601";
        string v_userName;//= "dehnad";
        string v_pwd;// = "OC56i4yA";

        public SyncMobinOne()
        {
            string userName, pwd;
            this.v_serverUrl = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.mobinOneSync, SharedLibrary.HelpfulFunctions.enumServersActions.mobinOneSync, out userName, out pwd);
            this.v_userName = userName;
            this.v_pwd = pwd;
        }

        public void sb_sync(DateTime dateStart, DateTime dateEnd)
        {
            DateTime date = dateStart;
            string str;
            Uri uri;
            HttpWebRequest webRequest;
            WebResponse webResponse;

            List<MobinOneItemInfo> lstMobinOneItemInfo;
            string shortCode;
            SharedLibrary.Models.vw_servicesServicesInfo entryService;
            SharedLibrary.Models.MobinOneFtp entryMobinOneFtp;
            string[] contradictionsIdsArr;
            string contradictionsIds;

            using (var entityPortal = new SharedLibrary.Models.PortalEntities())
            {
                while (date.Date <= dateEnd.Date)
                {
                    try
                    {

                        uri = new Uri(v_serverUrl + "username=" + v_userName + "&password=" + v_pwd + "&date=" + date.ToString("yyyyMMdd"), UriKind.Absolute);
                        webRequest = (HttpWebRequest)WebRequest.Create(uri);
                        webRequest.Timeout = 60 * 1000;

                        //webRequest.Headers.Add("SOAPAction", action);
                        webRequest.Method = "GET";
                        webResponse = webRequest.GetResponse();
                        using (StreamReader sr = new StreamReader(webResponse.GetResponseStream()))
                        {
                            str = sr.ReadToEnd();
                        }

                        webResponse.Close();
                        if (string.IsNullOrEmpty(str))
                        {
                            continue;
                        }
                        if (str == "-1")
                        {
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "MobinOneSync Wrong userName password");
                            Program.logs.Error("DehnadFtpSyncAndChargingService:MobinOne:sb_download:Wrong userName password");
                        }
                        else if (str == "-2")
                        {
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "MobinOneSync UserName or password is empty");
                            Program.logs.Error("DehnadFtpSyncAndChargingService:MobinOne:sb_download:UserName or password is empty");
                        }
                        else if (str == "-3")
                        {
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "MobinOneSync Parameters are empty");
                            Program.logs.Error("DehnadFtpSyncAndChargingService:MobinOne:sb_download:Parameters are empty");
                        }
                        else if (str == "-5")
                        {

                        }
                        else
                        {

                            lstMobinOneItemInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MobinOneItemInfo>>(str);
                            foreach (var mobinOneItemInfo in lstMobinOneItemInfo)
                            {
                                shortCode = mobinOneItemInfo.shortcode;
                                if (shortCode.StartsWith("98"))
                                {
                                    shortCode = shortCode.Remove(0, 2);
                                }

                                if (entityPortal.MobinOneFtps.Any(o => o.date == mobinOneItemInfo.date
                                 && o.service_name == mobinOneItemInfo.service_name && o.event_type == mobinOneItemInfo.event_type
                                 && o.subscriber_type == mobinOneItemInfo.subscriber_type
                                 && o.download_channel == mobinOneItemInfo.download_channel
                                 && (o.downloads == mobinOneItemInfo.downloads && o.revenue == mobinOneItemInfo.revenue)
                                 ))
                                {
                                    //we saved this record before;
                                    continue;
                                }
                                contradictionsIds = "";
                                contradictionsIdsArr = entityPortal.MobinOneFtps.Where(o => o.date == mobinOneItemInfo.date
                                 && o.service_name == mobinOneItemInfo.service_name && o.event_type == mobinOneItemInfo.event_type
                                 && o.subscriber_type == mobinOneItemInfo.subscriber_type
                                 && o.download_channel == mobinOneItemInfo.download_channel
                                 && (o.downloads != mobinOneItemInfo.downloads || o.revenue != mobinOneItemInfo.revenue)).Select(o => o.Id.ToString()).ToArray();
                                if (contradictionsIdsArr != null && contradictionsIdsArr.Length > 0)
                                {
                                    //we saved this record before but with different revenue or download
                                    contradictionsIds = string.Join(",", contradictionsIdsArr);
                                }

                                entryMobinOneFtp = new SharedLibrary.Models.MobinOneFtp();
                                entryMobinOneFtp.action_type = mobinOneItemInfo.action_type;
                                entryMobinOneFtp.date = mobinOneItemInfo.date;
                                entryMobinOneFtp.downloads = mobinOneItemInfo.downloads;
                                entryMobinOneFtp.download_channel = mobinOneItemInfo.download_channel;
                                entryMobinOneFtp.eup = mobinOneItemInfo.eup;
                                entryMobinOneFtp.event_type = mobinOneItemInfo.event_type;
                                entryMobinOneFtp.regDate = DateTime.Now;
                                entryMobinOneFtp.requestedChannel = mobinOneItemInfo.requestedChannel;
                                entryMobinOneFtp.revenue = mobinOneItemInfo.revenue;
                                entryMobinOneFtp.service_name = mobinOneItemInfo.service_name;
                                entryMobinOneFtp.shortcode = mobinOneItemInfo.shortcode;
                                entryMobinOneFtp.subscriber_type = mobinOneItemInfo.subscriber_type;

                                entryService = entityPortal.vw_servicesServicesInfo.Where(o => o.ShortCode == shortCode).OrderByDescending(o => o.Id).FirstOrDefault();
                                if (entryService != null)
                                {
                                    entryMobinOneFtp.serviceCode = entryService.ServiceCode;
                                }
                                else
                                {
                                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MobinOneSync Unknown short code " + entryService.ShortCode);
                                    Program.logs.Error("DehnadFtpSyncAndChargingService:MobinOne:sb_download:Unknown short code " + entryService.ShortCode);
                                }

                                entityPortal.MobinOneFtps.Add(entryMobinOneFtp);
                                entityPortal.SaveChanges();
                                if (!string.IsNullOrEmpty(contradictionsIds))
                                {
                                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MobinOneSync Contradiction has been detected newID=" + entryMobinOneFtp.Id + " oldID(s)=" + contradictionsIds);
                                    Program.logs.Error("DehnadFtpSyncAndChargingService:MobinOne:sb_download:Contradiction has been detected newID=" + entryMobinOneFtp.Id + " oldID(s)=" + contradictionsIds);
                                }
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MobinOneSync " + e.Message);
                        Program.logs.Error("DehnadFtpSyncAndChargingService:MobinOne:sb_download:", e);
                    }
                    date = date.AddDays(1);
                }
            }
        }
    }
}
