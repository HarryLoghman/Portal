//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BimeIranLibrary.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class InsuranceInfo
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public InsuranceInfo()
        {
            this.DamageReports = new HashSet<DamageReport>();
            this.ErrorLogs = new HashSet<ErrorLog>();
        }
    
        public long Id { get; set; }
        public string MobileNumber { get; set; }
        public string PassportNo { get; set; }
        public bool IsApproved { get; set; }
        public string ConfirmationCode { get; set; }
        public System.DateTime DateInsuranceRequested { get; set; }
        public string PersianDateInsuranceRequested { get; set; }
        public Nullable<System.DateTime> DateInsuranceApproved { get; set; }
        public string PersianDateInsuranceApproved { get; set; }
        public string InsuranceNo { get; set; }
        public string SocialNumber { get; set; }
        public string ZipCode { get; set; }
        public bool IsSendedToInsuranceCompany { get; set; }
        public Nullable<int> PackageIdSendedToInsuranceCompany { get; set; }
        public string InsuranceType { get; set; }
        public Nullable<System.DateTime> DateCancelationRequested { get; set; }
        public string PersianDateCancelationRequested { get; set; }
        public Nullable<bool> IsCancelationSendedToInsuranceCompany { get; set; }
        public Nullable<bool> IsCancelationDeliveryReceivedFromInsuranceCompany { get; set; }
        public Nullable<bool> IsSendedDeliveryReceivedFromInsuranceCompnay { get; set; }
        public Nullable<System.DateTime> DateCancelationApproved { get; set; }
        public string PersianDateCancelationApproved { get; set; }
        public Nullable<bool> IsUserRequestedInsuranceCancelation { get; set; }
        public Nullable<bool> IsUserWantsToChangeZipCode { get; set; }
        public string NewZipCode { get; set; }
        public Nullable<bool> IsNewZipCodeSendedToInsuranceCompany { get; set; }
        public Nullable<bool> IsNewZipCodeDeliveredFromInsuranceCompany { get; set; }
        public string CancelationNumber { get; set; }
        public string CancelationDescription { get; set; }
        public Nullable<bool> IsInsuranceNumberSendedToUser { get; set; }
        public Nullable<System.DateTime> NextRenewalDate { get; set; }
        public string PersianNextRenewalDate { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DamageReport> DamageReports { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ErrorLog> ErrorLogs { get; set; }
    }
}
