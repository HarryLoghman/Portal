using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models.ServiceModel;

using System.Data.Entity;

namespace DehnadAvvalPod500Service
{
    public class SinglechargeQueue
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void ProcessQueue()
        {
            try
            {
                RemoveUsersFromSinglechargeWaitingList();
                //SendWarningToSinglechargeUsersInQueue();
                //ChargeUsersFromSinglechargeQueue();
                //SendRenewalWarningToSinglechargeUsersInQueue();
                //RenewSinglechargeInstallmentQueue();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeQueue ProcessQueue : ", e);
            }
        }

        private void RemoveUsersFromSinglechargeWaitingList()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities( Properties.Settings.Default.ServiceCode))
                {

                    var chargeCodes = entity.ImiChargeCodes.ToList();
                    var now = DateTime.Now;
                    var QueueList = entity.SinglechargeWaitings.Where(o => DbFunctions.AddHours(o.DateAdded, 2) <= now).ToList();
                    if (QueueList.Count == 0)
                        return;
                    var mobileNumbers = QueueList.Select(o => o.MobileNumber).ToList();
                    foreach (var item in QueueList)
                    {
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        entity.SinglechargeWaitings.Remove(item);
                        batchSaveCounter += 1;
                    }
                    entity.SaveChanges();

                    var maxChargeLimit = SinglechargeInstallmentClass.maxChargeLimit;
                    string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode);
                    var serviceCode = Properties.Settings.Default.ServiceCode;
                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                    int installmentListCount = mobileNumbers.Count;
                    var installmentListTakeSize = Properties.Settings.Default.DefaultSingleChargeTakeSize;
                    SinglechargeInstallmentClass.TelepromoMapfaInstallmentJob(maxChargeLimit, 0, 0, serviceCode, chargeCodes, mobileNumbers, installmentListCount, installmentListTakeSize, serviceAdditionalInfo);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeQueue RemoveUsersFromSinglechargeWaitingList : ", e);
            }
        }

        
    }
}
