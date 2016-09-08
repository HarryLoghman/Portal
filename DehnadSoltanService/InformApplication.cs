using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using SoltanLibrary.Models;
using System.Data.Entity;

namespace DehnadSoltanService
{
    public class InformApplication
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Inform()
        {
            try
            {
                using (var entity = new SoltanEntities())
                {
                    var unInformed = entity.Singlecharges.Where(o => o.IsApplicationInformed == false).Take(100).ToList();
                    if (unInformed == null)
                        return;
                    List<Task> TaskList = new List<Task>();
                    foreach (var item in unInformed)
                    {
                        TaskList.Add(CallApplicationUrlToInformSinglecharge(entity, item));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in Infrom method: ", e);
            }
        }
        private async Task CallApplicationUrlToInformSinglecharge(SoltanEntities entity, Singlecharge singlechargeItem)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var values = new Dictionary<string, string>
                {
                   { "sign", "ErhIvyN33DItV7OmYxoAZmzYzf0pdHagZMTmQCcKyfvdAPLpSOvqTDumSihaY13r15FXB3PlI32xwQVfRJ76hIq2dwpy9WtYZyaVFfNwTxjsjrbXYn0WiVZe76hIq2dw" },
                   { "mobileNumber", singlechargeItem.MobileNumber },
                   { "price", (singlechargeItem.Price / 10).ToString() }
                };

                    var content = new FormUrlEncodedContent(values);
                    var url = "http://godfather.abrstudio.ir/api/purchase/newsms";
                    var response = await client.PostAsync(url, content);

                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                    if (jsonResponse.success == "true")
                    {
                        singlechargeItem.IsApplicationInformed = true;
                        entity.Singlecharges.Attach(singlechargeItem);
                        entity.Entry(singlechargeItem).State = EntityState.Modified;
                        entity.SaveChanges();
                    }
                    else
                    {
                        logs.Info(url + " responded with:" + responseString);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CallApplicationUrlToInformSinglecharge: ", e);
            }
        }
    }
}
