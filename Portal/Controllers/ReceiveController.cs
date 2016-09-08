using System.Web.Http;
using SharedLibrary.Models;
using System.Web;

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
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                return "-1";
            messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
            return "1";
        }

        // /Receive/Delivery?PardisId=44353535&Status=DeliveredToNetwork&ErrorMessage=error
        [HttpGet]
        [AllowAnonymous]
        public string Delivery([FromUri]DeliveryObject delivery)
        {
            SharedLibrary.MessageHandler.SaveDeliveryStatus(delivery);
            return "1";
        }

        // /Receive/PardisIntegratedPanel?Address=09125612694&ServiceID=1245&EventId=error
        [HttpGet]
        [AllowAnonymous]
        public string PardisIntegratedPanel([FromUri]IntegratedPanel integratedPanelObj)
        {
            if (integratedPanelObj.EventID == "1.2" && integratedPanelObj.NewStatus == 5)
            {
                var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(integratedPanelObj.ServiceID);
                integratedPanelObj.Address = SharedLibrary.MessageHandler.ValidateNumber(integratedPanelObj.Address);
                if (integratedPanelObj.Address == "Invalid Mobile Number")
                    return "-1";

                var recievedMessage = new MessageObject();
                recievedMessage.Content = integratedPanelObj.ServiceID;
                recievedMessage.MobileNumber = integratedPanelObj.Address;
                recievedMessage.ShortCode = serviceInfo.ShortCode;
                recievedMessage.IsReceivedFromIntegratedPanel = true;
                recievedMessage.MobileNumber = integratedPanelObj.Address;
                SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
            }
            return "1";
        }

        // /Receive/ChargeUser?MobileNumber=09125612694&ShortCode=3071171&Content=1
        [HttpGet]
        [AllowAnonymous]
        public string ChargeUser([FromUri]MessageObject message)
        {
            message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            if (message.ReceivedFrom == "31.187.71.85")
            {
                message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(message.MobileNumber);
                if (message.MobileNumber == "Invalid Mobile Number")
                    return "-1";
                message = SharedLibrary.MessageHandler.ValidateMessage(message);
                message.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(message.ShortCode);
                message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                message.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                message.IsReceivedFromIntegratedPanel = false;
                var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode("Soltan");
                if (service == null)
                    return "-2";
                message.ServiceId = service.Id;
                var singlecharge = SoltanLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                if (singlecharge == null)
                    return "-3";
                using(var entity = new SoltanLibrary.Models.SoltanEntities())
                {
                    entity.Singlecharges.Attach(singlecharge);
                    singlecharge.IsCalledFromInAppPurchase = true;
                    entity.Entry(singlecharge).State = System.Data.Entity.EntityState.Modified;
                    entity.SaveChanges();   
                }
                if (singlecharge.IsSucceeded == true)
                    return "1";
                else
                    return "-4";
            }
            else
                return "-5";
        }
    }
}
