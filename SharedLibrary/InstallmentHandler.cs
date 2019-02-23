using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class InstallmentHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static dynamic GetInstallmentList(dynamic entity)
        {
            return ((IEnumerable<dynamic>)entity.SinglechargeInstallments).Where(o => o.IsFullyPaid == false && o.IsExceededDailyChargeLimit == false && o.IsUserCanceledTheInstallment == false && o.IsRenewd != true).ToList();
        }

       
        public static SharedLibrary.Models.MessageObject ChooseMtnSinglechargePrice(SharedLibrary.Models.MessageObject message, dynamic chargeCodes, int priceUserChargedToday, int maxChargeLimit)
        {
            if (priceUserChargedToday == 0)
            {
                message.Price = maxChargeLimit;
            }
            else if (priceUserChargedToday <= 200)
            {
                message.Price = 100;
            }
            else
                message.Price = 0;
            return message;
        }

        public static SharedLibrary.Models.MessageObject ChooseMapfaSinglechargePrice(SharedLibrary.Models.MessageObject message, dynamic chargeCodes, int priceUserChargedToday, int maxChargeLimit)
        {
            if (priceUserChargedToday == 0)
            {
                message.Price = maxChargeLimit;
            }
            else
                message.Price = 0;
            return message;
        }

        public static SharedLibrary.Models.MessageObject SetMessagePrice(SharedLibrary.Models.MessageObject message, dynamic chargeCodes, int price)
        {
            var chargecode = ((IEnumerable<dynamic>)chargeCodes).FirstOrDefault(o => o.Price == price);
            message.Price = chargecode.Price;
            message.ImiChargeKey = chargecode.ChargeKey;
            return message;
        }

        public static void InstallmentCycleToDb(string serviceCode, Type entityType, Type installmentCycleType, int cycleNumber, long duration, int income)
        {
            try
            {
                var today = DateTime.Now;
                using (dynamic entity = Activator.CreateInstance(entityType, serviceCode))
                {
                    var cycle = ((IEnumerable<dynamic>)entity.InstallmentCycles).FirstOrDefault(o => o.DateCreated.Date == today.Date && o.CycleNumber == cycleNumber);
                    if (cycle != null)
                    {
                        cycle.Income += income;
                        entity.Entry(cycle).State = EntityState.Modified;
                    }
                    else
                    {
                        dynamic installmentCycle = Activator.CreateInstance(installmentCycleType);
                        installmentCycle.CycleNumber = cycleNumber;
                        installmentCycle.DateCreated = DateTime.Now;
                        installmentCycle.Duration = duration;
                        installmentCycle.Income = income;
                        entity.InstallmentCycles.Add(installmentCycle);
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in InstallmentCycleToDb: ", e);
            }
        }

    }
}
