using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChargingLibrary
{
    public class singleChargeRequest
    {
        public string mobileNumber{ get; set; }
        public WebExceptionStatus webStatus { get; set; }
        public bool internalServerError { get; set; }
        public string resultDescription { get; set; }
        public string payload { get; set; }
        public string referenceCode { get; set; }
        public int? Price { get; set; }
        public int installmentCycleNumber { get; set; }
        public int threadNumber { get; set; }
        public long installmentId { get; set; }
        public string guidStr { get; set; }
        public DateTime? timeAfterXML { get; set; }
        public DateTime? timeBeforeHTTPClient  { get; set; }
        public DateTime? timeBeforeSendRequest  { get; set; }
        public DateTime? timeAfterSendRequest  { get; set; }
        public DateTime? timeBeforeReadStringClient  { get; set; }
        public DateTime? timeAfterReadStringClient  { get; set; }
        public DateTime dateCreated { get; set; }
        public DateTime? timeLoop { get; set; }
        public DateTime? timeStartChargeMtnSubscriber { get; set; }
        public int loopNo { get; set; }
        public string url { get; set; }

        public DateTime? timeStartProcessMtnInstallment { get; set; }
        public DateTime? timeAfterEntity { get; set; }

        public DateTime? timeAfterWhere { get; set; }

        public bool isSucceeded { get; set; }
    }
}
