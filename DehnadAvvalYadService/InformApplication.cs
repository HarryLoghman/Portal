using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using SharedLibrary.Models.ServiceModel;
using System.Data.Entity;

namespace DehnadAvvalYadService
{
    public class InformApplication
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Inform()
        {
            try
            {
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    var unInformed = entity.Singlecharges.Where(o => o.IsApplicationInformed == false && o.IsSucceeded == true && o.IsCalledFromInAppPurchase == false).Take(100).ToList();
                    if (unInformed == null)
                        return;
                    List<Task> TaskList = new List<Task>();
                    foreach (var item in unInformed)
                    {
                        int package = item.Price;
                        if (item.InstallmentId != null)
                            package = entity.SinglechargeInstallments.Where(o => o.Id == item.InstallmentId).FirstOrDefault().TotalPrice;
                        TaskList.Add(CallApplicationUrlToInformSinglecharge(entity, item, package));
                    }
                    Task.WaitAll(TaskList.ToArray());
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in Infrom method: ", e);
            }
        }
        private async Task CallApplicationUrlToInformSinglecharge(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity, Singlecharge singlechargeItem, int package)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var values = new Dictionary<string, string>
                {
                   { "Sign", "ErhIvyN33DItV7OmYxoAZmzYzf0pdHagZMTmQCcKyfvdAPLpSOvqTDumSihaY13r15FXB3PlI32xwQVfRJ76hIq2dwpy9WtYZyaVFfNwTxjsjrbXYn0WiVZe76hIq2dw" },
                   { "MobileNumber", singlechargeItem.MobileNumber },
                   { "Price", (singlechargeItem.Price).ToString() },
                   { "Package", package.ToString() }
                };

                    var content = new FormUrlEncodedContent(values);
                    var url = "http://herosh.abrstudio.ir/api/purchase/newsms";
                    var response = await client.PostAsync(url, content);

                    var responseString = await response.Content.ReadAsStringAsync();
                    logs.Info("CallApplicationUrlToInformSinglecharge:" + responseString);
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                    if (jsonResponse.success == "true")
                    {
                        singlechargeItem.IsApplicationInformed = true;
                        entity.Singlecharges.Attach(singlechargeItem);
                        entity.Entry(singlechargeItem).State = EntityState.Modified;
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
