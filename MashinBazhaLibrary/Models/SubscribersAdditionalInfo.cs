//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MashinBazhaLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SubscribersAdditionalInfo
    {
        public long Id { get; set; }
        public long SubscriberId { get; set; }
        public Nullable<bool> IsSubscriberReceivedSubscriptionPoint { get; set; }
        public Nullable<bool> IsSubscriberReceivedOffReasonPoint { get; set; }
        public Nullable<bool> IsSubscriberSendedOffReason { get; set; }
    }
}
