using System.Web.Http;
using Portal.Models;

namespace Portal.Controllers
{
    public class ReceiveController : ApiController
    {
        // /Receive/Message?mobileNumber=09125612694&shortCode=2050&content=hi&receiveTime=22&messageId=45
        [HttpGet]
        [AllowAnonymous]
        public string Message([FromUri]MessageObject messageObj)
        {
            if (messageObj.MobileNumber == null && messageObj.Address != null)
            {
                messageObj.MobileNumber = messageObj.Address;
                messageObj.Content = messageObj.Message;
            }
            messageObj.MobileNumber = Shared.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                return "-1";
            messageObj.ShortCode = Shared.MessageHandler.ValidateShortCode(messageObj.ShortCode);
            Shared.MessageHandler.SaveReceivedMessage(messageObj);
            return "1";
        }

        // /Receive/Delivery?PardisId=44353535&Status=DeliveredToNetwork&ErrorMessage=error
        [HttpGet]
        [AllowAnonymous]
        public string Delivery([FromUri]DeliveryObject delivery)
        {
            Shared.MessageHandler.SaveDeliveryStatus(delivery);
            return "1";
        }

        // /Receive/PardisIntegratedPanel?Address=09125612694&ServiceID=1245&EventId=error
        [HttpGet]
        [AllowAnonymous]
        public string PardisIntegratedPanel([FromUri]IntegratedPanel integratedPanelObj)
        {
            if(integratedPanelObj.EventID == "1.2" && integratedPanelObj.NewStatus == 5)
            {
                var serviceInfo = Shared.ServiceHandler.GetServiceInfoFromAggregatorServiceId(integratedPanelObj.ServiceID);
                var recievedMessage = new ReceievedMessage();
                recievedMessage.Content = "off";
                recievedMessage.MobileNumber = integratedPanelObj.Address;
                recievedMessage.ShortCode = serviceInfo.ShortCode;
                recievedMessage.IsReceivedFromIntegratedPanel = true;
                Shared.MessageHandler.HandleReceivedMessage(recievedMessage);
            }
            return "1";
        }
    }
}
