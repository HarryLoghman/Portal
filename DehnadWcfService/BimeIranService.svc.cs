using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using BimeIranLibrary.Models;
namespace DehnadWcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class BimeIranService : IBimeIranService
    {
        public List<UsersInfo> GetData()
        {
            var insuranceData = new List<UsersInfo>();
            using (var entity = new BimeIranEntities())
            {
                var currentTakedSize = 0;
                entity.Configuration.AutoDetectChangesEnabled = false;

                var damageReportList = entity.DamageReports.Where(o => o.IsSendedToInsuranceCompany == false).Take(100).ToList();

                foreach (var damageReport in damageReportList)
                {
                    if (currentTakedSize > 100)
                        break;
                    var userInfo = new UsersInfo();
                    userInfo.DateRequested = damageReport.DateDamageReported;
                    userInfo.MobileNumber = damageReport.InsuranceInfo.MobileNumber;
                    userInfo.SocialNumber = damageReport.InsuranceInfo.SocialNumber;
                    userInfo.ZipCode = damageReport.InsuranceInfo.ZipCode;
                    userInfo.InsuranceCode = damageReport.InsuranceInfo.InsuranceNo;
                    userInfo.Request = Enums.Request.DamageReport;
                    damageReport.IsSendedToInsuranceCompany = true;
                    entity.Entry(damageReport).State = System.Data.Entity.EntityState.Modified;
                    insuranceData.Add(userInfo);
                    currentTakedSize++;
                }
                if (currentTakedSize < 100)
                {
                    var zipcodes = entity.InsuranceInfoes.Where(o => o.IsUserWantsToChangeZipCode == true && o.IsNewZipCodeSendedToInsuranceCompany == false).Take(100 - currentTakedSize).ToList();

                    foreach (var zipcode in zipcodes)
                    {
                        var userInfo = new UsersInfo();
                        userInfo.DateRequested = DateTime.Now;
                        userInfo.MobileNumber = zipcode.MobileNumber;
                        userInfo.SocialNumber = zipcode.SocialNumber;
                        userInfo.InsuranceCode = zipcode.InsuranceNo;
                        userInfo.ZipCode = zipcode.NewZipCode;
                        userInfo.Request = Enums.Request.ChangeZipCode;
                        insuranceData.Add(userInfo);
                        zipcode.IsNewZipCodeSendedToInsuranceCompany = true;
                        zipcode.IsUserWantsToChangeZipCode = false;
                        entity.Entry(zipcode).State = System.Data.Entity.EntityState.Modified;
                    }
                }
                if (currentTakedSize < 100)
                {
                    var insurances = entity.InsuranceInfoes.Where(o => o.IsSendedToInsuranceCompany == false && o.DateCancelationRequested == null).Take(100 - currentTakedSize).ToList();

                    foreach (var insurance in insurances)
                    {
                        var userInfo = new UsersInfo();
                        userInfo.DateRequested = insurance.DateInsuranceRequested;
                        userInfo.MobileNumber = insurance.MobileNumber;
                        userInfo.SocialNumber = insurance.SocialNumber;
                        userInfo.ZipCode = insurance.ZipCode;
                        if (insurance.InsuranceType == "A")
                            userInfo.Request = Enums.Request.RegisterInsurancePlanA;
                        else if (insurance.InsuranceType == "B")
                            userInfo.Request = Enums.Request.RegisterInsurancePlanB;
                        else if (insurance.InsuranceType == "C")
                            userInfo.Request = Enums.Request.RegisterInsurancePlanC;
                        else if (insurance.InsuranceType == "D")
                            userInfo.Request = Enums.Request.RegisterInsurancePlanD;
                        insuranceData.Add(userInfo);
                        insurance.IsSendedToInsuranceCompany = true;
                        entity.Entry(insurance).State = System.Data.Entity.EntityState.Modified;
                    }
                }
                if (currentTakedSize < 100)
                {
                    var cancelInsurancesList = entity.InsuranceInfoes.Where(o => o.IsSendedToInsuranceCompany != true && o.IsUserRequestedInsuranceCancelation == true && o.IsCancelationSendedToInsuranceCompany == false).Take(100 - currentTakedSize).ToList();

                    foreach (var cancelInsurance in cancelInsurancesList)
                    {
                        var userInfo = new UsersInfo();
                        userInfo.DateRequested = cancelInsurance.DateCancelationRequested.GetValueOrDefault();
                        userInfo.MobileNumber = cancelInsurance.MobileNumber;
                        userInfo.SocialNumber = cancelInsurance.SocialNumber;
                        userInfo.ZipCode = cancelInsurance.ZipCode;
                        userInfo.InsuranceCode = cancelInsurance.InsuranceNo;
                        userInfo.Request = Enums.Request.CancelInsurnce;
                        insuranceData.Add(userInfo);
                        cancelInsurance.IsCancelationSendedToInsuranceCompany = true;
                        entity.Entry(cancelInsurance).State = System.Data.Entity.EntityState.Modified;
                    }
                }
                entity.SaveChanges();
            }
            return insuranceData;
        }

        public ResultStatus SetData(UsersInfo userInfo)
        {
            var result = new ResultStatus();
            using (var entity = new BimeIranEntities())
            {
                if (userInfo.Request == Enums.Request.RegisterInsurancePlanA || userInfo.Request == Enums.Request.RegisterInsurancePlanB || userInfo.Request == Enums.Request.RegisterInsurancePlanC || userInfo.Request == Enums.Request.RegisterInsurancePlanD)
                {
                    var insurance = entity.InsuranceInfoes.FirstOrDefault(o => o.SocialNumber == userInfo.SocialNumber && o.ZipCode == userInfo.ZipCode);
                    if (insurance == null)
                    {
                        result.Status = Enums.Status.User_Does_Not_Exists;
                        return result;
                    }
                    insurance.DateInsuranceApproved = DateTime.Now;
                    insurance.PersianDateInsuranceApproved = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    insurance.InsuranceNo = userInfo.InsuranceCode;
                    entity.Entry(insurance).State = System.Data.Entity.EntityState.Modified;
                    entity.SaveChanges();
                }
                if(userInfo.Request == Enums.Request.CancelInsurnce)
                {
                    var insurance = entity.InsuranceInfoes.FirstOrDefault(o => o.SocialNumber == userInfo.SocialNumber && o.ZipCode == userInfo.ZipCode);
                    if (insurance == null)
                    {
                        result.Status = Enums.Status.User_Does_Not_Exists;
                        return result;
                    }
                    insurance.DateCancelationApproved = DateTime.Now;
                    insurance.PersianDateCancelationApproved = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                }
                result.Status = Enums.Status.Success;
                return result;
            }
        }

        public ResultStatus ValidateDataDelivery(UsersInfo userInfo)
        {
            var status = new ResultStatus();

            using (var entity = new BimeIranEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var insurance = entity.InsuranceInfoes.Where(o => o.SocialNumber == userInfo.SocialNumber && o.ZipCode == userInfo.ZipCode).FirstOrDefault();
                if (insurance == null)
                {
                    status.Status = Enums.Status.User_Does_Not_Exists;
                    status.Description = "Information for Social Number:" + userInfo.SocialNumber + " and Zipcode:" + userInfo.ZipCode + " not found";
                }
                else
                {
                    if (userInfo.Request == Enums.Request.RegisterInsurancePlanA || userInfo.Request == Enums.Request.RegisterInsurancePlanB || userInfo.Request == Enums.Request.RegisterInsurancePlanC || userInfo.Request == Enums.Request.RegisterInsurancePlanD)
                    {
                        insurance.IsSendedDeliveryReceivedFromInsuranceCompnay = true;
                        entity.Entry(insurance).State = System.Data.Entity.EntityState.Modified;
                    }
                    else if (userInfo.Request == Enums.Request.CancelInsurnce)
                    {
                        insurance.IsCancelationDeliveryReceivedFromInsuranceCompany = true;
                        entity.Entry(insurance).State = System.Data.Entity.EntityState.Modified;
                    }
                    else if (userInfo.Request == Enums.Request.ChangeZipCode)
                    {
                        insurance.IsNewZipCodeSendedToInsuranceCompany = false;
                        insurance.IsNewZipCodeDeliveredFromInsuranceCompany = true;
                        insurance.ZipCode = insurance.NewZipCode;
                        insurance.NewZipCode = "";
                        entity.Entry(insurance).State = System.Data.Entity.EntityState.Modified;
                    }
                    else if (userInfo.Request == Enums.Request.DamageReport)
                    {
                        var damageReport = entity.DamageReports.FirstOrDefault(o => o.InsuranceId == insurance.Id);
                        if (damageReport != null)
                        {
                            damageReport.IsDeliveryReceivedFromInsuranceCompany = true;
                            entity.Entry(damageReport).State = System.Data.Entity.EntityState.Modified;
                        }
                    }
                }
                entity.SaveChanges();
                status.Status = Enums.Status.Success;
                status.Description = "";
            }
            return status;
        }

        public Enums.Status ChangeZipCode(UsersInfo userInfo)
        {
            using (var entity = new BimeIranEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var insuranceInfo = entity.InsuranceInfoes.FirstOrDefault(o => o.MobileNumber == userInfo.MobileNumber && o.SocialNumber == userInfo.SocialNumber);
                if (insuranceInfo == null)
                    return Enums.Status.User_Does_Not_Exists;
                var isZipcodeExists = entity.InsuranceInfoes.FirstOrDefault(o => o.ZipCode == userInfo.ZipCode && o.DateCancelationApproved != null);
                if (isZipcodeExists != null)
                    return Enums.Status.Zipcode_Already_Exists;
                insuranceInfo.NewZipCode = userInfo.ZipCode;
                insuranceInfo.IsUserWantsToChangeZipCode = true;
                entity.Entry(insuranceInfo).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
                return Enums.Status.Success;
            }
        }

        public UsersInfo GetUserInfo(UsersInfo userInfo)
        {
            using (var entity = new BimeIranEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var user = new UsersInfo();
                var insuranceInfo = entity.InsuranceInfoes.FirstOrDefault(o => o.MobileNumber == userInfo.MobileNumber && o.SocialNumber == userInfo.SocialNumber);
                if (insuranceInfo == null)
                    user.Description = "User does not exists";

                user.MobileNumber = userInfo.MobileNumber;
                user.SocialNumber = userInfo.SocialNumber;
                user.ZipCode = userInfo.ZipCode;
                return user;
            }
        }
    }
}
