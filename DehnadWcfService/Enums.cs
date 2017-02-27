using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DehnadWcfService
{
    public class Enums
    {
        public enum Request
        {
            RegisterInsurancePlanA,
            RegisterInsurancePlanB,
            RegisterInsurancePlanC,
            RegisterInsurancePlanD,
            DamageReport,
            CancelInsurnce,
            ChangeZipCode
        }

        public enum Status
        {
            Success,
            User_Does_Not_Exists,
            Zipcode_Already_Exists,
        }
    }
}