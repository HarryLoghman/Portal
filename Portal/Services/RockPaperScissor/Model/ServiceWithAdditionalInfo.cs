using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Services.RockPaperScissor.Model
{
    public class ServiceWithAdditionalInfo
    {
        public Service Service { get; set; }
        public RPS_ServiceAdditionalInfo RPS_ServiceAdditionalInfo { get; set; }
    }
}