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
    
    public partial class ServiceInfo
    {
        public long Id { get; set; }
        public long AggregatorId { get; set; }
        public long ServiceId { get; set; }
        public string ShortCode { get; set; }
        public string AggregatorServiceId { get; set; }
        public string OperatorServiceId { get; set; }
        public string databaseName { get; set; }
        public Nullable<int> chargePrice { get; set; }
    
        public virtual Service Service { get; set; }
        public virtual Aggregator Aggregator { get; set; }
    }
}
