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
            Unsuccessful,
            No_Damage_Has_Been_Reported_From_This_User,
            Zip_Code_Format_Is_Incorrect,
            SocialNumber_Format_Is_Incorrect
        }
    }
}