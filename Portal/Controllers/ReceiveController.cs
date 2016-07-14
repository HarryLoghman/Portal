using System.Web.Http;
using Portal.Models;

namespace Portal.Controllers
{
    public class ReceiveController : ApiController
    {
        // /Receive/Message?mobileNumber=09125612694&shortCode=2050&content=hi&receiveTime=22&messageId=45
        [HttpGet]
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
    }
}
