using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedioLibrary.Models;
using MedioLibrary;
using System.Data.Entity;
using System.Threading;
using System.Collections;

namespace DehnadMedioService
{
    public class SinglechargeInstallmentClass
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int maxChargeLimit = 400;
        public int ProcessInstallment(int installmentCycleNumber)
        {
            int income = 0;
            try
            {
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceCode = Properties.Settings.Default.ServiceCode;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                List<string> installmentList;
                Type entityType = typeof(MedioEntities);
                Type singleChargeType = typeof(Singlecharge);

                using (var entity = new MedioEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    entity.Database.CommandTimeout = 120;
                    List<ImiChargeCode> chargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                    for (int installmentInnerCycleNumber = 1; installmentInnerCycleNumber <= 1; installmentInnerCycleNumber++)
                    {
                        logs.Info("start of installmentInnerCycleNumber " + installmentInnerCycleNumber);
                        //installmentList = ((IEnumerable)SharedLibrary.InstallmentHandler.GetInstallmentList(entity)).OfType<SinglechargeInstallment>().ToList();
                        
                        installmentList = SharedLibrary.ServiceHandler.GetServiceActiveMobileNumbersFromServiceCode(serviceCode);
                        var today = DateTime.Now;
                        var delayDateBetweenCharges = today.AddDays(0);
                        var chargeCompleted = entity.vw_Singlecharge.AsNoTracking()
                            .Where(o => DbFunctions.TruncateTime(o.DateCreated) >= DbFunctions.TruncateTime(delayDateBetweenCharges) && DbFunctions.TruncateTime(o.DateCreated) <= DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                            .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) >= maxChargeLimit).Select(o => o.Key).ToList();
                        var waitingList = entity.SinglechargeWaitings.AsNoTracking().Select(o => o.MobileNumber).ToList();
                        installmentList.RemoveAll(o => chargeCompleted.Contains(o));
                        installmentList.RemoveAll(o => waitingList.Contains(o));
                        int installmentListCount = installmentList.Count;
                        var installmentListTakeSize = Properties.Settings.Default.DefaultSingleChargeTakeSize;
                        income += SharedLibrary.InstallmentHandler.MapfaInstallmentJob(entityType, maxChargeLimit, installmentCycleNumber, installmentInnerCycleNumber, serviceCode, chargeCodes, installmentList, installmentListCount, installmentListTakeSize, serviceAdditionalInfo, singleChargeType);
                        logs.Info("end of installmentInnerCycleNumber " + installmentInnerCycleNumber);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ProcessInstallment:", e);
            }
            return income;
        }
    }
}
