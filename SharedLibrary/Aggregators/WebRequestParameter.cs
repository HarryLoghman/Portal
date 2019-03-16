using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Aggregators
{
    public class WebRequestParameter
    {
        public WebRequestParameter(enum_webRequestParameterType webRequestType, string bodyString
            , SharedLibrary.Models.vw_servicesServicesInfo service
            , EventHandler handlerFinish
            , log4net.ILog logs)
        {
            if (service == null) throw new Exception("Service is not specified");
            if (string.IsNullOrEmpty(service.databaseName)) throw new Exception("database name of the service id " + service.Id.ToString() + " is not specified");

            this.prp_webRequestType = webRequestType;
            this.prp_bodyString = bodyString;
            this.prp_logs = logs;
            this.prp_service = service;
            this.prp_handlerFinish = handlerFinish;
        }

        public enum_webRequestParameterType prp_webRequestType { get; }
        public SharedLibrary.Models.vw_servicesServicesInfo prp_service { get; }
        public string prp_result { get; set; }
        public bool prp_isSucceeded { get; set; }

        public string prp_bodyString { get; }
        public string prp_cnnStrService
        {
            get
            {
                return "Data source =.; initial catalog = " + this.prp_databaseName + "; integrated security = True; max pool size=4000; multipleactiveresultsets=True;connection timeout=180 ;";
            }
        }
        public log4net.ILog prp_logs { get; }

        public string prp_databaseName
        {
            get
            {
                return this.prp_service.databaseName;
            }
        }

        internal EventHandler prp_handlerFinish { get; set; }

    }

    public enum enum_webRequestParameterType
    {
        message,
        singleCharge
    }
}

