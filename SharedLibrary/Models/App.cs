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
    
    public partial class App
    {
        public int Id { get; set; }
        public string appName { get; set; }
        public int keySize { get; set; }
        public string IV { get; set; }
        public string keyVector { get; set; }
        public string enryptAlghorithm { get; set; }
        public string description { get; set; }
        public Nullable<int> state { get; set; }
        public string allowedServices { get; set; }
    }
}
