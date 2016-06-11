using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Services.RockPaperScissor.Model
{
    public class SubscriberWithAdditionalInfo
    {
        public Subscriber Subscriber { get; set; }
        public RPS_SubscribersAdditionalInfo RPS_SubscribersAdditionalInfo { get; set; }
    }
}