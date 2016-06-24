using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Portal.Models;

namespace Portal.Controllers
{
    public class ReceiveController : ApiController
    {
        // /Receive/Message?mobileNumber=09125612694&shortCode=2050&content=hi&receiveTime=22&messageId=45
        [HttpGet]
        public string Message([FromUri]Message message)
        {
            Shared.MessageHandler.SaveReceivedMessage(message);
            using (var entity = new PortalEntities())
            {
                var serviceShortCodes = entity.ServiceInfoes.FirstOrDefault(o => o.ShortCode == message.ShortCode);
                if(serviceShortCodes == null)
                    Shared.PortalException.Throw("Invalid service short code : " + message.ShortCode);
                var service = entity.Services.FirstOrDefault(o => o.Id == serviceShortCodes.ServiceId);
                if(service == null)
                    Shared.PortalException.Throw("Invalid service for: " + message.ShortCode);
                message = Shared.MessageHandler.ValidateMessage(message);
                message.ServiceId = service.Id;

                //if (service.ServiceCode == "RPS")
                //    Services.RockPaperScissor.HandleMo.ReceivedMessage(message, service);
                //else if (service.ServiceCode == "Danestan")
                    Services.Danestan.HandleMo.ReceivedMessage(message, service);
            }
            return "Done";
        }

    }
}
