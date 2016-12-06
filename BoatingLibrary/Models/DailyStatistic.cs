//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoatingLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DailyStatistic
    {
        public long Id { get; set; }
        public System.DateTime Date { get; set; }
        public string PersianDate { get; set; }
        public int NumberOfSubscriptions { get; set; }
        public int NumberOfUnsubscriptions { get; set; }
        public int NumberOfPostpaidSubscriptions { get; set; }
        public int NumberOfPrepaidSubscriptions { get; set; }
        public int NumberOfMessagesSent { get; set; }
        public int NumberOfPostpaidMessagesSent { get; set; }
        public int NumberOfPrepaidMessagesSent { get; set; }
        public int TotalSubscribers { get; set; }
        public int NumberOfOnDemandMessagesSent { get; set; }
        public int NumberOfOnDemandPostpaidMessagesSent { get; set; }
        public int NumberOfOnDemandPrepaidMessagesSent { get; set; }
        public int NumberOfAutochargeMessagesSent { get; set; }
        public int NumberOfAutochargePostpaidMessagesSent { get; set; }
        public int NumberOfAutochargePrepaidMessagesSent { get; set; }
        public int NumberOfEventbaseMessagesSent { get; set; }
        public int NumberOfEventbasePostpaidMessagesSent { get; set; }
        public int NumberOfEventbasePrepaidMessagesSent { get; set; }
        public int NumberOfPostpaidUnsubscriptions { get; set; }
        public int NumberOfPrepaidUnsubscriptions { get; set; }
        public int NumberOfMessagesFailed { get; set; }
        public int NumberOfPostpaidMessagesFailed { get; set; }
        public int NumberOfPrepaidMessagesFailed { get; set; }
        public int NumberOfOnDemandMessagesFailed { get; set; }
        public int NumberOfOnDemandPostpaidMessagesFailed { get; set; }
        public int NumberOfOnDemandPrepaidMessagesFailed { get; set; }
        public int NumberOfAutochargeMessagesFailed { get; set; }
        public int NumberOfAutochargePostpaidMessagesFailed { get; set; }
        public int NumberOfAutochargePrepaidMessagesFailed { get; set; }
        public int NumberOfEventbaseMessagesFailed { get; set; }
        public int NumberOfEventbasePostpaidMessagesFailed { get; set; }
        public int NumberOfEventbasePrepaidMessagesFailed { get; set; }
    }
}
