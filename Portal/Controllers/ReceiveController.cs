using System.Web.Http;
using Portal.Models;

namespace Portal.Controllers
{
    public class ReceiveController : ApiController
    {
        // /Receive/Message?mobileNumber=09125612694&shortCode=2050&content=hi&receiveTime=22&messageId=45
        [HttpGet]
        public string Message([FromUri]MessageObject message)
        {
            if (message.MobileNumber == null && message.Address != null)
            {
                message.MobileNumber = message.Address;
                message.Content = message.Message;
            }
            message.MobileNumber = Shared.MessageHandler.ValidateNumber(message.MobileNumber);
            if (message.MobileNumber == "Invalid Mobile Number")
                return "-1";
            Shared.MessageHandler.SaveReceivedMessage(message);
            return "1";
        }
    }
}
