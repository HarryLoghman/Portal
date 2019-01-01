using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ChargingLibrary
{
    public class ServiceCharge
    {

        #region properties and variables
        public vw_servicesServicesInfo prp_service { get; set; }
        //public int prp_serviceId { get; set; }
        //public string prp_serviceName { get; }
        //public string prp_serviceCode { get; }
        //public string prp_shortCode { get; }
        //public string prp_databaseName { get; }
        /// <summary>
        /// initial tps value
        /// </summary>
        public int prp_tpsServiceInitial { get; }
        /// <summary>
        /// tps during assign
        /// </summary>
        public int prp_tpsServiceCurrent { get; set; }
        public virtual int prp_maxPrice
        {
            get
            {
                if (this.prp_service == null) return 0;
                else return (this.prp_service.chargePrice.HasValue ? this.prp_service.chargePrice.Value : 0);
            }
        }
        public virtual int prp_maxTries { get; }
        //public string prp_aggregatorServiceId { get; }
        public int prp_rowCount { get; set; }
        protected List<SharedLibrary.ServiceHandler.SubscribersAndCharges> v_subscribersAndCharges;
        public virtual string[] prp_wipeDescription { get { return new string[] { "" }; ; } }
        public int prp_rowIndex { get; set; }
        public int prp_rowsProcessedInSecond { get; set; }
        public int prp_remainRowCount
        {
            get
            {
                return this.prp_rowCount - this.prp_rowIndex;
            }
        }
        public SharedLibrary.ServiceHandler.SubscribersAndCharges prp_subscriberCurrent
        {
            get
            {
                if (this.prp_rowIndex >= 0 && this.prp_rowIndex < this.v_subscribersAndCharges.Count)
                    return this.v_subscribersAndCharges[this.prp_rowIndex];
                else return null;
            }
        }
        internal int prp_cycleNumber { get; set; }
        internal int prp_rowsSavedToDB { get; set; }

        /// <summary>
        /// gets and sets the price for this cycle
        /// </summary>
        public virtual int prp_cyclePrice
        { get; set; }

        public bool prp_wipe { get; set; }
        #endregion

        public ServiceCharge(int serviceId, int tpsService, int maxTries, int cycleNumber, int cyclePrice)
        {
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var vw = portal.vw_servicesServicesInfo.Where(o => o.Id == serviceId).FirstOrDefault();
                if (vw == null)
                {
                    throw new Exception("There is no service with serviceId=" + serviceId.ToString());
                }
                else
                {
                    this.prp_service = vw;
                }
                //SharedLibrary.Models.Service service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceId);
            }
            if (cyclePrice < 0)
            {
                throw new Exception("ServiceCharge " + this.prp_service.ServiceCode + ": CyclePrice is negative Value");
            }
            else if (cyclePrice > this.prp_maxPrice)
            {
                throw new Exception("ServiceCharge " + this.prp_service.ServiceCode + ": CyclePrice(" + cyclePrice + ") is greater than MaxChargePrice(" + this.prp_service.chargePrice + ")Value");
            }
            //if (service == null)
            //{
            //    throw new Exception("There is no service with serviceId=" + serviceId.ToString());
            //}

            //SharedLibrary.Models.ServiceInfo serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId);

            //if (serviceInfo == null)
            //{
            //    throw new Exception("There is no serviceInfo with serviceId=" + serviceId.ToString());
            //}

            //if (string.IsNullOrWhiteSpace(serviceInfo.databaseName))
            //{
            //    throw new Exception("The database name is not specified for serviceId=" + serviceId.ToString());
            //}
            if (maxTries <= 0)
            {
                throw new Exception("Max tries should be a positive value");
            }
            //this.prp_service.Id = serviceId;
            //this.prp_service.ServiceCode = service.ServiceCode;
            //this.prp_serviceName = service.Name;
            //this.prp_shortCode = serviceInfo.ShortCode;
            //this.prp_service.databaseName = serviceInfo.databaseName;
            //this.prp_maxPrice = (this.prp_service.chargePrice.HasValue ? this.prp_service.chargePrice.Value : 0);
            this.prp_maxTries = maxTries;

            this.prp_tpsServiceInitial = tpsService;
            this.prp_tpsServiceCurrent = tpsService;

            //this.prp_aggregatorServiceId = aggregatorServiceId;
            this.prp_cycleNumber = cycleNumber;
            this.prp_rowCount = 0;
            this.prp_rowIndex = 0;
            this.prp_rowsProcessedInSecond = 0;

            this.prp_cyclePrice = cyclePrice;
        }

        #region subs and functions
        public virtual bool fnc_canStartCharging(int cycleNumber, out string reason)
        {
            return this.fnc_canStartCharging(cycleNumber, DateTime.Now, out reason);
        }
        public virtual bool fnc_canStartCharging(int cycleNumber, DateTime date, out string reason)
        {

            reason = "";
            bool canStart = false;
            if ((DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00")))
            {
                reason = "23:45:00 to 00:01:00";
                return canStart;
            }
            SqlCommand cmd = new SqlCommand();
            try
            {

                cmd.CommandText = "Select top 1 value from " + this.prp_service.databaseName + ".dbo.settings where Name='IsInMaintenanceTime'";
                cmd.Connection = new SqlConnection(Program.v_cnnStr);
                cmd.Connection.Open();

                object isInMaintenace = cmd.ExecuteScalar();
                bool bl;
                if (isInMaintenace == null || isInMaintenace == DBNull.Value || isInMaintenace.ToString() == "" || (bool.TryParse(isInMaintenace.ToString(), out bl) && !bl))
                {
                    cmd.CommandText = "Select top 1 value from " + this.prp_service.databaseName + ".dbo.settings where Name='LastSingleCharge'";
                    object lastSingleCharge = cmd.ExecuteScalar();
                    if (lastSingleCharge == null || lastSingleCharge == DBNull.Value || lastSingleCharge.ToString() == "" || (!lastSingleCharge.ToString().StartsWith(date.ToString("yyyy-MM-dd")) || !lastSingleCharge.ToString().EndsWith(";" + cycleNumber.ToString() + ";wipe")))
                    {
                        canStart = true;
                    }
                    else
                    {
                        reason = "Cycle " + cycleNumber.ToString() + " for date " + date.ToString("yyyy-MM-dd") + " has been completed recently";
                        canStart = false;
                    }
                }
                else
                {
                    reason = "Service " + this.prp_service.Id.ToString() + " is in maintenance mode";
                    canStart = false;
                }
                cmd.Connection.Close();
            }
            catch (Exception e)
            {
                Program.logs.Error(this.prp_service.ServiceCode + ": cannot start charging", e);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, this.prp_service.ServiceCode + " : cannot start charging" + e.Message);
                try
                {
                    cmd.Connection.Close();
                    reason = e.Message + "\r\n" + e.StackTrace;
                }
                catch
                {

                }
            }
            return canStart;
        }
        public virtual void sb_fill(out bool error)
        {
            error = false;
            try
            {
                this.v_subscribersAndCharges = null;
                this.prp_rowCount = 0;
                this.prp_rowIndex = 0;

                this.v_subscribersAndCharges = SharedLibrary.ServiceHandler.getActiveSubscribersForCharge(this.prp_service.databaseName, this.prp_service.Id, DateTime.Now, this.prp_maxPrice, this.prp_maxTries, this.prp_cycleNumber
                , true, false, null, null, null);
                this.prp_rowCount = this.v_subscribersAndCharges.Count;

            }
            catch (Exception e)
            {
                error = true;
                Program.logs.Error(this.prp_service.ServiceCode + " : fill", e);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, this.prp_service.ServiceCode + " : fill " + e.Message);
            }
        }

        public virtual void sb_fillWipe(out bool error)
        {
            error = false;
            try
            {
                this.v_subscribersAndCharges = null;
                this.prp_rowCount = 0;
                this.prp_rowIndex = 0;

                this.v_subscribersAndCharges = SharedLibrary.ServiceHandler.getActiveSubscribersForCharge(this.prp_service.databaseName, this.prp_service.Id, DateTime.Now, this.prp_maxPrice, this.prp_maxTries, this.prp_cycleNumber
                    , true, true, string.Join(";", this.prp_wipeDescription), null, null);
                this.prp_rowCount = this.v_subscribersAndCharges.Count;

            }
            catch (Exception e)
            {
                error = true;
                Program.logs.Error(this.prp_service.ServiceCode + " : fill wipe", e);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, this.prp_service.ServiceCode + " : fill wipe" + e.Message);
            }

        }

        public virtual void sb_charge(
       SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber
       , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, long installmentId = 0)
        {

        }

        internal void saveResponseToDB(singleChargeRequest singleChargeReq)
        {
            //DateTime timeResponse = DateTime.Now;
            //Nullable<DateTime> timeAfterXML = null;
            //Nullable<DateTime> timeBeforeHTTPClient = null;
            //Nullable<DateTime> timeBeforeSendMTNClient = null;
            //Nullable<DateTime> timeAfterSendMTNClient = null;
            //Nullable<DateTime> timeBeforeReadStringClient = null;
            //Nullable<DateTime> timeAfterReadStringClient = null;
            System.Threading.Interlocked.Decrement(ref ChargingController.v_taskCount);
            //object obj = new object();
            //lock (obj) { int t = chargeServices.v_taskCount; chargeServices.v_taskCount = t - 1; }
            singleChargeReq.timeAfterSendMTNClient = DateTime.Now;
            Nullable<TimeSpan> duration = null;

            try
            {
                //timeAfterSendMTNClient = DateTime.Now;
                if (singleChargeReq.webStatus == WebExceptionStatus.Success || singleChargeReq.internalServerError)
                {

                    singleChargeReq.timeBeforeReadStringClient = DateTime.Now;
                    singleChargeReq.timeAfterReadStringClient = DateTime.Now;

                }
                else
                {
                    singleChargeReq.isSucceeded = false;
                    singleChargeReq.resultDescription = singleChargeReq.webStatus.ToString();
                }
                singleChargeReq.timeAfterXML = DateTime.Now;

                //dateCreated = DateTime.Now;
                var endTime = DateTime.Now;
                duration = endTime - singleChargeReq.timeBeforeSendMTNClient;


            }
            catch (Exception e)
            {
                //Program.logs.Info(this.prp_service.ServiceCode + " :  " + singleChargeReq.payload);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in Save to DB1: ", e);
            }
            try
            {

                this.insertSingleCharge(singleChargeReq.mobileNumber, singleChargeReq.referenceCode, singleChargeReq.dateCreated, singleChargeReq.Price.GetValueOrDefault()
                    , singleChargeReq.isSucceeded
                   , singleChargeReq.resultDescription, false, singleChargeReq.installmentCycleNumber, false, (duration.HasValue ? (int)duration.Value.TotalMilliseconds : (int?)null), singleChargeReq.installmentCycleNumber, singleChargeReq.threadNumber);

                this.insertSingleChargeTiming(singleChargeReq.installmentCycleNumber, singleChargeReq.loopNo, singleChargeReq.threadNumber, singleChargeReq.mobileNumber, singleChargeReq.guidStr
                    , singleChargeReq.timeLoop, singleChargeReq.timeStartProcessMtnInstallment, singleChargeReq.timeAfterEntity, singleChargeReq.timeAfterWhere
                    , singleChargeReq.timeStartChargeMtnSubscriber, singleChargeReq.timeBeforeHTTPClient
                    , singleChargeReq.timeBeforeSendMTNClient, singleChargeReq.timeAfterSendMTNClient, singleChargeReq.timeBeforeReadStringClient
                    , singleChargeReq.timeAfterReadStringClient, singleChargeReq.timeAfterXML, singleChargeReq.dateCreated);

            }
            catch (Exception e)
            {
                //Program.logs.Info(this.prp_service.ServiceCode + " : " + singleChargeReq.payload);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in Save to DB2: ", e);

            }
        }

        protected void insertSingleCharge(string MobileNumber, string ReferenceId, DateTime DateCreated, int Price, bool IsSucceeded
            , string Description, bool IsApplicationInformed, long InstallmentId, bool IsCalledFromInAppPurchase, Nullable<int> ProcessTimeInMilliSecond, int CycleNumber,
                         int ThreadNumber)
        {

            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = new SqlConnection(Program.v_cnnStr);

                cmd.CommandText = "insert into " + this.prp_service.databaseName + ".dbo.singleCharge "
                             + "(MobileNumber, ReferenceId, DateCreated, PersianDateCreated, Price, IsSucceeded, Description, IsApplicationInformed, InstallmentId"
                             + ", IsCalledFromInAppPurchase, ProcessTimeInMilliSecond, CycleNumber, ThreadNumber , wipe) "
                             + " values"
                             + "('" + MobileNumber + "'" + ","
                             + (string.IsNullOrEmpty(ReferenceId) ? "Null" : "'" + ReferenceId + "'") + ","
                             + "'" + DateCreated.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" + ","
                             + "'" + SharedLibrary.Date.GetPersianDate(DateCreated) + " " + DateCreated.ToString("HH:mm:ss.fff") + "'" + ","
                             + Price + ","
                             + (IsSucceeded ? "1" : "0") + ","
                             + (string.IsNullOrEmpty(Description) ? "Null" : "'" + Description + "'") + ","
                             + (IsApplicationInformed ? "1" : "0") + ","
                             + InstallmentId.ToString() + ","
                             + (IsCalledFromInAppPurchase ? "1" : "0") + ","
                             + (ProcessTimeInMilliSecond.HasValue ? ProcessTimeInMilliSecond.Value.ToString() : "null") + ","
                             + CycleNumber.ToString() + ","
                             + ThreadNumber.ToString() + ","
                             + (this.prp_wipe ? "1" : "0") + ")";
                Program.logs.Info(cmd.CommandText);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }
            catch (Exception ex1)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in insertSingleCharge: ", ex1);
                Program.logs.Error("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                Program.logs.Warn("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                Program.logs.Info("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");

                //*ServiceMTN.StopService("Service " + this.prp_service.ServiceCode + " stops because of :" + ex1.Message);

            }
        }

        protected void insertSingleChargeTiming(int cycleNumber, int loopNo, int threadNumber, string mobileNumber, string guid, Nullable<DateTime> timeCreate
            , Nullable<DateTime> timeStartProcessMtnInstallment, Nullable<DateTime> timeAfterEntity, Nullable<DateTime> timeAfterWhere, Nullable<DateTime> timeStartChargeMtnSubscriber
            , Nullable<DateTime> timeBeforeHTTPClient, Nullable<DateTime> timeBeforeSendMTNClient, Nullable<DateTime> timeAfterSendMTNClient, Nullable<DateTime> timeBeforeReadStringClient
            , Nullable<DateTime> timeAfterReadStringClient, Nullable<DateTime> timeAfterXML, Nullable<DateTime> timeFinish)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = new SqlConnection(Program.v_cnnStr);

                cmd.CommandText = "insert into " + this.prp_service.databaseName + ".dbo.singleChargeTiming "
                             + "(cycleNumber, loopNo, threadNumber, mobileNumber, guid, timeCreate, timeStartProcessMtnInstallment, timeAfterEntity, timeAfterWhere "
                             + ", timeStartChargeMtnSubscriber, timeBeforeHTTPClient, timeBeforeSendMTNClient, timeAfterSendMTNClient, timeBeforeReadStringClient"
                             + ", timeAfterReadStringClient, timeAfterXML, timeFinish) "
                             + " values"
                             + "("
                             + cycleNumber.ToString() + ","
                             + loopNo.ToString() + ","
                             + threadNumber.ToString() + ","
                             + "'" + mobileNumber + "'" + ","
                             + "'" + guid + "'" + ","
                             + (timeCreate.HasValue ? "'" + timeCreate.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeStartProcessMtnInstallment.HasValue ? "'" + timeStartProcessMtnInstallment.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeAfterEntity.HasValue ? "'" + timeAfterEntity.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeAfterWhere.HasValue ? "'" + timeAfterWhere.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeStartChargeMtnSubscriber.HasValue ? "'" + timeStartChargeMtnSubscriber.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeBeforeHTTPClient.HasValue ? "'" + timeBeforeHTTPClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeBeforeSendMTNClient.HasValue ? "'" + timeBeforeSendMTNClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeAfterSendMTNClient.HasValue ? "'" + timeAfterSendMTNClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeBeforeReadStringClient.HasValue ? "'" + timeBeforeReadStringClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeAfterReadStringClient.HasValue ? "'" + timeAfterReadStringClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeAfterXML.HasValue ? "'" + timeAfterXML.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                             + (timeFinish.HasValue ? "'" + timeFinish.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ")";
                //Program.logs.Info(cmd.CommandText);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }
            catch (Exception e)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in insertSingleChargeTiming: ", e);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, this.prp_service.ServiceCode + " : Exception in insertSingleChargeTiming: " + e.Message);
            }
        }

        protected virtual void afterSend(singleChargeRequest chargeRequest)
        {

        }
        public SharedLibrary.Models.MessageObject ChooseSinglechargePrice(SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber)
        {
            var message = new SharedLibrary.Models.MessageObject();
            Nullable<int> pricePaidToday = subscriber.pricePaidToday;
            //message.MobileNumber = "100" + subscriber.mobileNumber;
            message.MobileNumber = subscriber.mobileNumber;
            message.ShortCode = this.prp_service.ShortCode;

            int maxCyclePrice = Math.Min(this.prp_maxPrice, this.prp_cyclePrice);
            int remain = this.prp_maxPrice - (subscriber.pricePaidToday.HasValue ? subscriber.pricePaidToday.Value : 0);
            message.Price = Math.Min(maxCyclePrice, remain);

            //message = ChooseMtnSinglechargePrice(message, pricePaidToday.Value);

            //if (installmentCycleNumber < 2 && message.Price != this.prp_maxPrice)
            //    return;
            //else if (installmentCycleNumber > 2)
            //    message.Price = this.prp_maxPrice / 2;



            //if (installmentCycleNumber == 4 && this.prp_service.ServiceCode.ToLower() == "tahchin")
            //{
            //    if(wipe)
            //        message.Price = 150;
            //    else message.Price = 65;
            //}



            if (pricePaidToday + message.Price > this.prp_maxPrice)
                return null;

            return message;
            //if (!pricePaidToday.HasValue || pricePaidToday == 0)
            //{
            //    message.Price = this.prp_maxPrice;
            //}
            //else if (pricePaidToday <= this.prp_maxPrice / 2)
            //{
            //    message.Price = this.prp_maxPrice / 2;
            //}
            //else
            //    message.Price = 0;
            //return message;
        }

        public virtual void sb_finishCharge(int duration, bool wipe)
        {
            try
            {
                string yesterday = (DateTime.Now.AddDays(-1)).ToString("yyyy-MM-dd");
                string str = "";

                str = "exec sp_finishCharging "
                    + " '" + this.prp_service.databaseName + "'"
                    + "," + "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                    + "," + this.prp_cycleNumber.ToString()
                    + "," + (wipe ? "'wipe'" : "Null")
                    + "," + duration.ToString();
                using (PortalEntities portal = new PortalEntities())
                {
                    portal.Database.CommandTimeout = 180;
                    portal.Database.ExecuteSqlCommand(str);
                }
            }
            catch (Exception e)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in sb_finishCharge: ", e);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, this.prp_service.ServiceCode + " : Exception in sb_finishCharge: " + e.Message);
            }

        }

        #endregion
    }
}
