using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AggregatorsLibrary
{
    class WebRequestParameter
    {
        public WebRequestParameter(enum_webRequestParameterType webRequestType, string bodyString , SharedLibrary.Models.vw_servicesServicesInfo service
            ,log4net.ILog logs)
        {
            if (service == null) throw new Exception("Service is not specified");
            if (string.IsNullOrEmpty(service.databaseName)) throw new Exception("database name of the service id " + service.Id.ToString() + " is not specified");

            this.prp_webRequestType = webRequestType;
            this.prp_bodyString = bodyString;
            this.prp_logs = logs;
            this.prp_service = service;
        }

        internal enum_webRequestParameterType prp_webRequestType { get; }
        internal SharedLibrary.Models.vw_servicesServicesInfo prp_service { get; }
        internal string prp_result { get; set; }
        internal bool prp_isSucceeded { get; set; }

        internal string prp_bodyString { get; }
        internal string prp_cnnStrService
        {
            get
            {
                return "Data source =.; initial catalog = " + this.prp_databaseName + "; integrated security = True; max pool size=4000; multipleactiveresultsets=True;connection timeout=180 ;";
            }
        }
        internal log4net.ILog prp_logs { get; }

        internal string prp_databaseName
        {
            get
            {
                return this.prp_service.databaseName;
            }
        }
        internal virtual void sb_save()
        {
            
        }

        internal virtual string fnc_parseResult(string result, out bool isSucceeded)
        {
            isSucceeded = false;
            return "";
        }

    }

    enum enum_webRequestParameterType
    {
        message,
        charge
    }
}

