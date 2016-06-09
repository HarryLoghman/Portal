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
        // /Receive/Mo?mobileNumber=09125612694&shortCode=2050&content=hi&receiveTime=22&messageId=45
        [HttpGet]
        public string Mo([FromUri]Message message)
        {
            Shared.MessageHandler.SaveReceivedMessage(message);
            using (var entity = new PortalEntities())
            {
                var serviceShortCodes = entity.ServiceShortCodes.FirstOrDefault(o => o.ShortCode == message.ShortCode);
                if(serviceShortCodes == null)
                    Shared.PortalException.Throw("Invalid service short code : " + message.ShortCode);
                var serviceInfo = entity.Services.FirstOrDefault(o => o.Id == serviceShortCodes.ServiceId);
                if(serviceInfo == null)
                    Shared.PortalException.Throw("Invalid service for: " + message.ShortCode);
                message = Shared.MessageHandler.ValidateMessage(message);
                message.ServiceId = serviceInfo.Id;
                if (message.ServiceId == 1)
                    Services.RockPaperScissor.HandleMo.ReceivedMessage(message, serviceInfo);
            }
            return "Done";
        }

    }
}
