//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SharedLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Bulk
    {
        public int Id { get; set; }
        public string bulkName { get; set; }
        public Nullable<long> ServiceId { get; set; }
        public Nullable<int> tps { get; set; }
        public Nullable<System.DateTime> startTime { get; set; }
        public Nullable<System.DateTime> endTime { get; set; }
        public string message { get; set; }
        public Nullable<int> readSize { get; set; }
        public Nullable<int> retryCount { get; set; }
        public Nullable<int> retryTimeoutInSeconds { get; set; }
        public Nullable<int> TotalMessages { get; set; }
        public Nullable<int> TotalSuccessfullySent { get; set; }
        public Nullable<int> TotalFailed { get; set; }
        public Nullable<int> TotalRetry { get; set; }
        public Nullable<int> TotalRetryUnique { get; set; }
        public Nullable<int> TotalDelivery { get; set; }
        public string BulkFile { get; set; }
        public Nullable<int> status { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public string PersianDateCreated { get; set; }
        public Nullable<int> Tag { get; set; }
    }
}
