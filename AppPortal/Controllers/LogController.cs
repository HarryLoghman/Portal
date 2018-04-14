using System.Web.Http;
using SharedLibrary.Models;
using System.Web;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class LogController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage ChargeErrorCodes(string serviceCode, string fromDate, string toDate)
        {
            dynamic result = new ExpandoObject();
            result.Data = null;
            string jsonString = null;
            try
            {
                var serviceId = SharedLibrary.ServiceHandler.GetServiceId(serviceCode);
                if(serviceId == null)
                {
                    result.Success = "false";
                    result.Error = "service code not found";
                }
                else
                {
                    var s = 0;
                    if (serviceCode == "Aseman")
                        using (var entity = new AsemanLibrary.Models.AsemanEntities())
                            s = 1;
                    else if (serviceCode == "AvvalPod")
                        using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
                            s = 1;
                    else if (serviceCode == "AvvalPod500")
                        using (var entity = new AvvalPod500Library.Models.AvvalPod500Entities())
                            s = 1;
                    else if (serviceCode == "AvvalYad")
                        using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
                            s = 1;
                    else if (serviceCode == "BehAmooz500")
                        using (var entity = new BehAmooz500Library.Models.BehAmooz500Entities())
                            s = 1;
                    else if (serviceCode == "Dambel")
                        using (var entity = new DambelLibrary.Models.DambelEntities())
                            s = 1;
                    else if (serviceCode == "Darchin")
                        using (var entity = new DarchinLibrary.Models.DarchinEntities())
                            s = 1;
                    else if (serviceCode == "DefendIran")
                        using (var entity = new DefendIranLibrary.Models.DefendIranEntities())
                            s = 1;
                    else if (serviceCode == "Dezhban")
                        using (var entity = new DezhbanLibrary.Models.DezhbanEntities())
                            s = 1;
                    else if (serviceCode == "DonyayeAsatir")
                        using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                            s = 1;
                    else if (serviceCode == "FitShow")
                        using (var entity = new FitShowLibrary.Models.FitShowEntities())
                            s = 1;
                    else if (serviceCode == "JabehAbzar")
                        using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
                            s = 1;
                    else if (serviceCode == "Medad")
                        using (var entity = new MedadLibrary.Models.MedadEntities())
                            s = 1;
                    else if (serviceCode == "Medio")
                        using (var entity = new MedioLibrary.Models.MedioEntities())
                            s = 1;
                    else if (serviceCode == "MenchBaz")
                        using (var entity = new MenchBazLibrary.Models.MenchBazEntities())
                            s = 1;
                    else if (serviceCode == "MusicYad")
                        using (var entity = new MusicYadLibrary.Models.MusicYadEntities())
                            s = 1;
                    else if (serviceCode == "Nebula")
                        using (var entity = new NebulaLibrary.Models.NebulaEntities())
                            s = 1;
                    else if (serviceCode == "Phantom")
                        using (var entity = new PhantomLibrary.Models.PhantomEntities())
                            s = 1;
                    else if (serviceCode == "ShahreKalameh")
                        using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
                            s = 1;
                    else if (serviceCode == "ShenoYad500")
                        using (var entity = new ShenoYad500Library.Models.ShenoYad500Entities())
                            s = 1;
                    else if (serviceCode == "ShenoYad")
                        using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
                            s = 1;
                    else if (serviceCode == "Soltan")
                        using (var entity = new SoltanLibrary.Models.SoltanEntities())
                            s = 1;
                    else if (serviceCode == "Soraty")
                        using (var entity = new SoratyLibrary.Models.SoratyEntities())
                            s = 1;
                    else if (serviceCode == "TahChin")
                        using (var entity = new TahChinLibrary.Models.TahChinEntities())
                            s = 1;
                    else if (serviceCode == "Takavar")
                        using (var entity = new TakavarLibrary.Models.TakavarEntities())
                            s = 1;
                    else if (serviceCode == "Tamly500")
                        using (var entity = new Tamly500Library.Models.Tamly500Entities())
                            s = 1;
                    else if (serviceCode == "Tamly")
                        using (var entity = new TamlyLibrary.Models.TamlyEntities())
                            s = 1;
                    result.Success = "true";
                    result.Description = "";
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChargeErrorCodes:" + e);
                result.Success = "false";
                result.Error = "contact system administrator";
            }
            var json = JsonConvert.SerializeObject(jsonString, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects });
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
