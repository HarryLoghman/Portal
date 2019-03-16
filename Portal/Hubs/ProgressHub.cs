using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace RealTimeProgressBar
{
    public class ProgressHub : Hub
    {

        public void SendMessage(string msg, int count)
        {
            var message = msg + " " + count.ToString() + "%";
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ProgressHub>();
            hubContext.Clients.All.sendMessage(string.Format(message), count);
        }
    }
}