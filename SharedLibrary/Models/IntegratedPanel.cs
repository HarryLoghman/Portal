using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SharedLibrary.Models
{
    public class IntegratedPanel
    {
        public string Address {get;set;}
        public string ServiceID {get;set;}
        public string EventID {get;set;}
        public int OldStatus {get;set;}
        public int NewStatus {get;set; }
    }
}