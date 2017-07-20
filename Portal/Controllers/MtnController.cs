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

namespace Portal.Controllers
{
    public class MtnController : ApiController
    {

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage SubUnsubNotify([FromBody] string deliveryData)
        {
            string result = @"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/notification/v2_2/local"">    
            <soapenv:Header/>    
<soapenv:Body>       
<loc:notifySmsDeliveryReceiptResponse/>    
</soapenv:Body> 
</soapenv:Envelope>";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/xml");
            return response;
        }
    }
}
