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
    
    public partial class serviceCyclesNew
    {
        public int id { get; set; }
        public string daysOfWeekOrDate { get; set; }
        public int cycleNumber { get; set; }
        public System.TimeSpan startTime { get; set; }
        public System.TimeSpan endTime { get; set; }
        public string servicesIDs { get; set; }
        public string minTPSs { get; set; }
        public string cycleChargePrices { get; set; }
        public Nullable<int> state { get; set; }
    }
}
