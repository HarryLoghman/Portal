//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace JhoobinMedadLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SinglechargeWaiting
    {
        public long Id { get; set; }
        public string MobileNumber { get; set; }
        public int Price { get; set; }
        public System.DateTime DateAdded { get; set; }
        public string PersianDateAdded { get; set; }
        public bool IsLastDayWarningSent { get; set; }
    }
}
