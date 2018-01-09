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
    public class BotController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> GetNotification()
        {
            try
            {
                string message = "";
            }
            catch (Exception e)
            {
                logs.Error("Excepiton in GetNotification method: ", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return response;
        }
    }
}
