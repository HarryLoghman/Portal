using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoltanLibrary.Models;
using SoltanLibrary;
using System.Data.Entity;

namespace DehnadSoltanService
{
    public class SinglechargeInstallment
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void ProcessInstallment()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                using (var entity = new SoltanEntities())
                {
                    var chargeCodes = entity.ImiChargeCodes.Where(o => o.Price <= 4000).ToList();
                    var installmentList = entity.SinglechargeInstallments.Where(o => o.IsFullyPaid == false && o.IsExceededDailyChargeLimit == false).ToList();
                    foreach (var installment in installmentList)
                    {
                        if (batchSaveCounter >= 500)
                            entity.SaveChanges();
                        var priceUserChargedToday = entity.Singlecharges.Where(o => o.MobileNumber == installment.MobileNumber && o.InstallmentId == installment.Id && DbFunctions.TruncateTime(o.DateCreated).Value == today).ToList().Sum(o => o.Price);
                        if (priceUserChargedToday >= 4000)
                        {
                            installment.IsExceededDailyChargeLimit = true;
                            entity.Entry(installment).State = EntityState.Modified;
                            batchSaveCounter += 1;
                            continue;
                        }
                        var message = new SharedLibrary.Models.MessageObject();
                        message.MobileNumber = installment.MobileNumber;
                        message.ShortCode = "3071171";
                        message = ChooseSinglechargePrice(message, chargeCodes, priceUserChargedToday);
                        var response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message);
                        if (response.IsSucceeded == true)
                        {
                            installment.PricePayed += message.Price.GetValueOrDefault();
                            if (installment.PricePayed >= installment.TotalPrice)
                                installment.IsFullyPaid = true;
                            entity.Entry(installment).State = EntityState.Modified;
                        }
                        batchSaveCounter += 1;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment ProcessInstallment: ", e);
            }
        }

        private static SharedLibrary.Models.MessageObject ChooseSinglechargePrice(SharedLibrary.Models.MessageObject message, List<ImiChargeCode> chargeCodes, int priceUserChargedToday)
        {
            if(priceUserChargedToday <= 200)
            {
                var chargecode = chargeCodes.FirstOrDefault(o => o.Price == 200);
                message.Price = chargecode.Price;
                message.ImiChargeKey = chargecode.ChargeKey;
            }
            return message;
        }
    }
}
