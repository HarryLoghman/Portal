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
    
    public partial class Aggregator
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Aggregator()
        {
            this.ServiceInfoes = new HashSet<ServiceInfo>();
        }
    
        public long Id { get; set; }
        public long OperatorId { get; set; }
        public string AggregatorName { get; set; }
        public string AggregatorUsername { get; set; }
        public string AggregatorPassword { get; set; }
        public Nullable<int> tps { get; set; }
    
        public virtual Operator Operator { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ServiceInfo> ServiceInfoes { get; set; }
    }
}
