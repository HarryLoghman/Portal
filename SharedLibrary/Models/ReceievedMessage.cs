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
    
    public partial class ReceievedMessage
    {
        public long Id { get; set; }
        public string MobileNumber { get; set; }
        public string ShortCode { get; set; }
        public System.DateTime ReceivedTime { get; set; }
        public string MessageId { get; set; }
        public bool IsProcessed { get; set; }
        public string Content { get; set; }
        public bool IsReceivedFromIntegratedPanel { get; set; }
        public string PersianReceivedTime { get; set; }
        public string ReceivedFrom { get; set; }
        public Nullable<bool> IsReceivedFromWeb { get; set; }
        public Nullable<int> RetryCount { get; set; }
        public Nullable<System.DateTime> LastRetryDate { get; set; }
    }
}
