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
        public InsuranceData GetData()
        {
            var insuranceData = new InsuranceData();
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
                    userInfo.UserRequest = Enums.UserRequest.DamageReport;
                    damageReport.IsSendedToInsuranceCompany = true;
                    entity.Entry(damageReport).State = System.Data.Entity.EntityState.Modified;
                    insuranceData.UsersInfo.Add(userInfo);
                    currentTakedSize++;
                }
                if (currentTakedSize < 100)
                {
                    var insurances = entity.InsuranceInfoes.Where(o => o.IsSendedToInsuranceCompany != true && o.DateCancelationRequested == null).Take(100 - currentTakedSize).ToList();

                    foreach (var insurance in insurances)
                    {
                        var userInfo = new UsersInfo();
                        userInfo.DateRequested = insurance.DateInsuranceRequested;
                        userInfo.MobileNumber = insurance.MobileNumber;
                        userInfo.SocialNumber = insurance.SocialNumber;
                        userInfo.ZipCode = insurance.ZipCode;
                        if (insurance.InsuranceType == "A")
                            userInfo.UserRequest = Enums.UserRequest.RegisterInsurancePlanA;
                        else if (insurance.InsuranceType == "B")
                            userInfo.UserRequest = Enums.UserRequest.RegisterInsurancePlanB;
                        else if (insurance.InsuranceType == "C")
                            userInfo.UserRequest = Enums.UserRequest.RegisterInsurancePlanC;
                        else if (insurance.InsuranceType == "D")
                            userInfo.UserRequest = Enums.UserRequest.RegisterInsurancePlanD;
                        insuranceData.UsersInfo.Add(userInfo);
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
                        userInfo.DateRequested = cancelInsurance.DateInsuranceRequested;
                        userInfo.MobileNumber = cancelInsurance.MobileNumber;
                        userInfo.SocialNumber = cancelInsurance.SocialNumber;
                        userInfo.ZipCode = cancelInsurance.ZipCode;
                        userInfo.InsuranceCode = cancelInsurance.InsuranceNo;
                        userInfo.UserRequest = Enums.UserRequest.CancelInsurnce;
                        insuranceData.UsersInfo.Add(userInfo);
                        cancelInsurance.IsCancelationSendedToInsuranceCompany = true;
                        entity.Entry(cancelInsurance).State = System.Data.Entity.EntityState.Modified;
                    }
                }
                entity.SaveChanges();
            }
            return insuranceData;
        }

        public DeliveryStatus ValidateDataDelivery(UsersInfo userInfo)
        {
            var status = new DeliveryStatus();
            status.IsSucessful = false;
            using (var entity = new BimeIranEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var insurance = entity.InsuranceInfoes.Where(o => o.SocialNumber == userInfo.SocialNumber && o.ZipCode == userInfo.ZipCode).FirstOrDefault();
                if (insurance == null)
                    status.Description = "Information for Social Number:" + userInfo.SocialNumber + " and Zipcode:" + userInfo.ZipCode + " not found";
                else
                {
                    if (userInfo.UserRequest == Enums.UserRequest.RegisterInsurancePlanA || userInfo.UserRequest == Enums.UserRequest.RegisterInsurancePlanB || userInfo.UserRequest == Enums.UserRequest.RegisterInsurancePlanC || userInfo.UserRequest == Enums.UserRequest.RegisterInsurancePlanD)
                    {
                        insurance.IsSendedDeliveryReceivedFromInsuranceCompnay = true;
                        entity.Entry(insurance).State = System.Data.Entity.EntityState.Modified;
                    }
                    else if (userInfo.UserRequest == Enums.UserRequest.CancelInsurnce)
                    {
                        insurance.IsCancelationDeliveryReceivedFromInsuranceCompany = true;
                        entity.Entry(insurance).State = System.Data.Entity.EntityState.Modified;
                    }
                    else if (userInfo.UserRequest == Enums.UserRequest.DamageReport)
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
                status.IsSucessful = true;
                status.Description = "";
            }
            return status;
        }
    }
}
