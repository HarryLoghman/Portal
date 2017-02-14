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
        public InsuranceData GetNewInsuranceData()
        {
            var insuranceData = new InsuranceData();
            using (var entity = new BimeIranEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var lastPackageId = entity.InsuranceInfoes.Where(o => o.PackageIdSendedToInsuranceCompany != null).OrderByDescending(o => o.PackageIdSendedToInsuranceCompany).Select(o => o.PackageIdSendedToInsuranceCompany).FirstOrDefault();
                if (lastPackageId == null)
                    insuranceData.PackageId = 1;
                else
                    insuranceData.PackageId = lastPackageId.Value + 1;
                var insurances = entity.InsuranceInfoes.Where(o => o.IsSendedToInsuranceCompany != true && o.DateCanceled == null).ToList();
                if (insurances.Count == 0)
                    insuranceData.PackageId = 0;
                else
                {
                    foreach (var insurance in insurances)
                    {
                        var userInfo = new UsersInfo();
                        userInfo.DateRequested = insurance.DateCreated;
                        userInfo.MobileNumber = insurance.MobileNumber;
                        userInfo.SocialNumber = insurance.SocialNumber;
                        userInfo.InsuranceType = insurance.InsuranceType;
                        userInfo.ZipCode = insurance.ZipCode;
                        insuranceData.UsersInfo.Add(userInfo);
                        insurance.PackageIdSendedToInsuranceCompany = insuranceData.PackageId;
                        entity.Entry(insurance).State = System.Data.Entity.EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
            return insuranceData;
        }

        public DeliveryStatus ValidateNewInsuranceDataDelivery(int packageId)
        {
            var status = new DeliveryStatus();
            status.IsSucessful = false;
            using (var entity = new BimeIranEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                
                var insurances = entity.InsuranceInfoes.Where(o => o.PackageIdSendedToInsuranceCompany == packageId).ToList();
                if (insurances.Count == 0)
                    status.Description = "No Item found in package id of" + packageId;
                else
                {
                    foreach (var insurance in insurances)
                    {
                        insurance.IsSendedToInsuranceCompany = true;
                        entity.Entry(insurance).State = System.Data.Entity.EntityState.Modified;
                    }
                    entity.SaveChanges();
                    status.IsSucessful = true;
                    status.Description = "Sucessful";
                }
            }
            return status;
        }
    }
}
