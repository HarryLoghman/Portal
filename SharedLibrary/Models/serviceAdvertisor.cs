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
    
    public partial class serviceAdvertisor
    {
        public int id { get; set; }
        public string ShortCode { get; set; }
        public Nullable<int> AdvertisorID { get; set; }
        public string validKeys { get; set; }
        public string asciiKey { get; set; }
        public Nullable<System.DateTime> fromDateTime { get; set; }
        public Nullable<System.DateTime> toDateTime { get; set; }
        public Nullable<bool> isValid { get; set; }
    }
}
