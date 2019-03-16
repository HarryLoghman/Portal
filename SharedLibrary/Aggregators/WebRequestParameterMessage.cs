using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Aggregators
{
    internal class WebRequestParameterMessage : WebRequestParameter
    {
        internal WebRequestParameterMessage(long id, string mobileNumber, int maxTries, DateTime dateTimeCorrelator, string messageContent
           , enum_webRequestParameterType webRequestType, SharedLibrary.MessageHandler.MessageType messageType
            , string bodyString, int? bulkId, int? retryCount
            , SharedLibrary.Models.vw_servicesServicesInfo service
            , EventHandler handlerFinish
            , log4net.ILog logs
            ) :
            base(webRequestType, bodyString, service, handlerFinish, logs)
        {
            this.prp_id = id;
            this.prp_maxTries = maxTries;
            this.prp_mobileNumber = mobileNumber;
            this.prp_messageType = messageType;
            this.prp_dateTimeCorrelator = dateTimeCorrelator;
            this.prp_messageContent = messageContent;
            this.prp_bulkId = bulkId;
            this.prp_retryCount = retryCount;
        }

        internal SharedLibrary.MessageHandler.MessageType prp_messageType { get; set; }
        internal long prp_id { get; }
        internal int? prp_retryCount { get; set; }
        internal string prp_referenceId { get; set; }
        internal int prp_maxTries { get; }
        internal int? prp_messagePoint { get; set; }
        internal string prp_mobileNumber { get; }

        internal int? prp_bulkId { get; set; }
        internal DateTime prp_dateTimeCorrelator { get; set; }

        internal string prp_messageContent { get; set; }

    }
}
