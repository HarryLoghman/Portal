using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TahChinLibrary.Models;
using TahChinLibrary;
using System.Data.Entity;
using System.Threading;
using System.Collections;

namespace DehnadTahChinService
{
    public class SinglechargeInstallmentClass
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static int maxChargeLimit = 400;
        public void ProcessInstallment(int installmentCycleNumber)
        {
            try
            {
                int maxChargeLimit = 300;
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceCode = "TahChin";
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                List<SinglechargeInstallment> installmentList;
                Singlecharge singlecharge = new Singlecharge();
                using (var entity = new TahChinEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    List<ImiChargeCode> chargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                    installmentList = ((IEnumerable)SharedLibrary.InstallmentHandler.GetInstallmentList(entity)).OfType<SinglechargeInstallment>().ToList();
                    int installmentListCount = installmentList.Count;
                    var installmentListTakeSize = Properties.Settings.Default.DefaultSingleChargeTakeSize;
                    SharedLibrary.InstallmentHandler.InstallmentJob(entity, maxChargeLimit, installmentCycleNumber, serviceCode, chargeCodes, installmentList, installmentListCount, installmentListTakeSize, serviceAdditionalInfo, singlecharge);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ProcessInstallment:", e);
            }
        }
    }
}
