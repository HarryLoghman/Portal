//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SoltanLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SinglechargeInstallment
    {
        public long Id { get; set; }
        public string MobileNumber { get; set; }
        public System.DateTime DateCreated { get; set; }
        public string PersianDateCreated { get; set; }
        public int TotalPrice { get; set; }
        public int PricePayed { get; set; }
        public bool IsFullyPaid { get; set; }
        public bool IsExceededDailyChargeLimit { get; set; }
        public int PriceTodayCharged { get; set; }
        public bool IsUserDailyChargeBalanced { get; set; }
        public bool IsUserCanceledTheInstallment { get; set; }
        public Nullable<System.DateTime> CancelationDate { get; set; }
        public string PersianCancelationDate { get; set; }
    }
}