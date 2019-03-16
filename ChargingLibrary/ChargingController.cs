using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChargingLibrary
{
    public class ChargingController
    {
        int v_turn;
        int v_tps;
        int v_tpsTotal;
        long v_ticksStart;
        Nullable<long> v_ticksPrevious;
        //System.Timers.Timer v_timer;
        List<ServiceCharge> v_chargeServices;

        public static int v_taskCount;
        private long v_notifTime;

        double v_intervalInMillisecond;

        public ChargingController()
        {
            //this.sb_setServicePoint();
        }
        ServicePointSettings v_spSettings;

        public enum enum_chargingSpeed
        {
            normal = 1,
            slow = 2,
            verySlow = 4,
            tooSlow = 8
        }
        public bool prp_resetVerySlowCharging { get; set; }
        public bool prp_resetTooSlowCharging { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tpsTotal"></param>
        /// <param name="chargeServices"></param>
        /// <param name="ticksStart"></param>
        /// <param name="chargingServiceName">for notify the user e.g. MTN Services or Phantom</param>
        public void sb_chargeAll(int tpsTotal, List<ServiceCharge> chargeServices, long ticksStart, string chargingServiceName
            , string notifIcon, bool resetVerySlowCharging, bool resetTooSlowCharging)
        {

            if (chargeServices == null || chargeServices.Count == 0)
                return;
            this.prp_resetTooSlowCharging = resetTooSlowCharging;
            this.prp_resetVerySlowCharging = resetVerySlowCharging;

            object lockObj = new object();
            this.v_spSettings = new ServicePointSettings();
            ServicePoint sp;

            #region threadPoolSetting
            ThreadPoolSettings thread = new ThreadPoolSettings();
            thread.Assign();
            #endregion

            this.v_tpsTotal = tpsTotal;
            this.v_intervalInMillisecond = 1000.00 / tpsTotal;


            this.v_turn = 0;
            this.v_ticksPrevious = null;
            lock (lockObj) { v_taskCount = 0; }
            //System.Threading.Interlocked.Exchange(ref v_taskCount ,0);
            this.sb_fillChargeList(chargeServices, false, notifIcon);

            this.sb_resetTPSs();
            this.sb_assignServicesTPS(this.v_tpsTotal);

            this.v_ticksStart = DateTime.Now.Ticks;

            if (this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Count() > 0)
            {
                #region checkServicePoint
                sp = this.v_spSettings.GetServicePoint();

                if (sp != null)
                {
                    //Program.logs.Info("Connection Limit to:" + sp.ConnectionLimit.ToString());
                    Console.WriteLine("Connection Limit to:" + sp.ConnectionLimit.ToString());
                }
                else
                {
                    //Program.logs.Error("Connection Limit is Default");
                    Console.WriteLine("Connection Limit to:" + sp.ConnectionLimit.ToString());
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, (string.IsNullOrEmpty(notifIcon) ? "" : notifIcon) + "Connection Limit is Default for " + chargingServiceName);

                }
                #endregion

                Program.logs.Info("startTime:" + DateTime.Now.ToString("HH:mm:ss,fff"));
                Program.logs.Info("interval:" + this.v_intervalInMillisecond + " totalRowCount " + this.v_chargeServices.Sum(o => o.prp_rowCount));

                Program.logs.Info("------------------------------------------------------");
                Program.logs.Info("------------------------------------------------------");

                this.v_notifTime = 0;
                this.v_spSettings.Assign();
                this.sb_chargeLoop(false, chargingServiceName);

                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, (string.IsNullOrEmpty(notifIcon) ? "" : notifIcon) + "End of Main cycle of " + chargingServiceName);
                return;
            }


            sp = this.v_spSettings.GetServicePoint();
            if (sp != null)
            {
                //Program.logs.Info("Connection Limit to:" + sp.ConnectionLimit.ToString());
                Console.WriteLine("Connection Limit to:" + sp.ConnectionLimit.ToString());
            }
            else
            {
                //Program.logs.Error("Connection Limit is Default");
                Console.WriteLine("Connection Limit to:" + sp.ConnectionLimit.ToString());
                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, (string.IsNullOrEmpty(notifIcon) ? "" : notifIcon) + "Connection Limit is Default for " + chargingServiceName);
            }

            this.v_turn = 0;
            this.v_ticksPrevious = null;
            lock (lockObj) { v_taskCount = 0; }
            //System.Threading.Interlocked.Exchange(ref v_taskCount ,0);
            this.sb_fillChargeList(chargeServices, true, notifIcon);
            this.sb_resetTPSs();
            this.sb_assignServicesTPS(this.v_tpsTotal);


            this.v_notifTime = 0;
            this.v_spSettings.Assign();
            this.sb_chargeLoop(true, chargingServiceName);

            SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, (string.IsNullOrEmpty(notifIcon) ? "" : notifIcon) + "End of Wipe of " + chargingServiceName);
            Console.WriteLine("Charging is Finished");
            SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, (string.IsNullOrEmpty(notifIcon) ? "" : notifIcon) + "Charging " + chargingServiceName + " is finished");
        }

        private void sb_fillChargeList(List<ServiceCharge> chargeServices, bool wipe, string notifIcon)
        {
            int i;
            bool error;

            this.v_chargeServices = new List<ServiceCharge>();

            if (!wipe)
            {
                for (i = 0; i <= chargeServices.Count - 1; i++)
                {
                    chargeServices[i].sb_fill(out error);
                    if (chargeServices[i].prp_rowCount > 0)
                    {
                        Program.logs.Info(chargeServices[i].prp_service.ServiceCode + ":start of installmentCycleNumber " + chargeServices[i].prp_cycleNumber);
                        Program.logs.Info(chargeServices[i].prp_service.ServiceCode + ":installmentList final list count:" + chargeServices[i].prp_rowCount);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, (string.IsNullOrEmpty(notifIcon) ? "" : notifIcon) + chargeServices[i].prp_service.ServiceCode + ":start of installmentCycleNumber " + chargeServices[i].prp_cycleNumber + " with " + chargeServices[i].prp_rowCount + " rows");
                        this.v_chargeServices.Add(chargeServices[i]);
                    }
                    else
                    {
                        if (!error)
                            chargeServices[i].sb_finishCharge(0, false);
                    }
                }
            }
            else
            {
                for (i = 0; i <= chargeServices.Count - 1; i++)
                {
                    chargeServices[i].sb_fillWipe(out error);
                    if (chargeServices[i].prp_rowCount > 0)
                    {
                        Program.logs.Info(chargeServices[i].prp_service.ServiceCode + ":start of wipe " + chargeServices[i].prp_cycleNumber);
                        Program.logs.Info(chargeServices[i].prp_service.ServiceCode + ":installmentList wipe list count:" + chargeServices[i].prp_rowCount);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, (string.IsNullOrEmpty(notifIcon) ? "" : notifIcon) + chargeServices[i].prp_service.ServiceCode + ":start of wipe " + chargeServices[i].prp_cycleNumber + " with " + chargeServices[i].prp_rowCount + " rows");
                        this.v_chargeServices.Add(chargeServices[i]);
                    }
                    else
                    {
                        if (!error)
                            chargeServices[i].sb_finishCharge(0, true);
                    }

                }
            }
        }

        private void sb_resetTPSs()
        {
            int i;
            this.v_tps = 0;
            for (i = 0; i <= this.v_chargeServices.Count - 1; i++)
            {
                this.v_chargeServices[i].prp_rowsProcessedInSecond = 0;
            }
        }

        private void sb_assignServicesTPS(int totalTPS)
        {
            int i;
            int occupiedTPS = 0;
            int serviceTPS;
            occupiedTPS = this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Sum(o => o.prp_tpsServiceInitial);
            List<ServiceCharge> chargeService = this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).ToList();
            if (chargeService.Count == 1)
            {
                serviceTPS = chargeService[0].prp_tpsServiceInitial;
                chargeService[0].prp_tpsServiceCurrent = totalTPS;
                Program.logs.Info("TPS ASSIGN:" + chargeService[0].prp_service.ServiceCode + ";old tps:" + serviceTPS + ";newtps:" + chargeService[0].prp_tpsServiceCurrent.ToString());
                return;
            }
            int freeTps = totalTPS - this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Sum(o => o.prp_tpsServiceInitial);
            if (freeTps <= 0) return;
            //Program.logs.Info("ASSIGN:FREE" + freeTps);

            for (i = 0; i <= this.v_chargeServices.Count - 1; i++)
            {
                if (this.v_chargeServices[i].prp_remainRowCount > 0)
                {
                    serviceTPS = this.v_chargeServices[i].prp_tpsServiceInitial;
                    this.v_chargeServices[i].prp_tpsServiceCurrent = (serviceTPS * totalTPS) / occupiedTPS;
                    Program.logs.Info("TPS ASSIGN:" + this.v_chargeServices[i].prp_service.ServiceCode + ";old tps:" + serviceTPS + ";newtps:" + this.v_chargeServices[i].prp_tpsServiceCurrent.ToString());

                }
            }
        }

        //private void sb_finish()
        //{
        //    if (this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Count() == 0/*no remain rows*/
        //        && v_taskCount <= 0/*wait till all task complete*/)
        //    {
        //        //   this.v_timer.Enabled = false;
        //        this.prp_finished = true;
        //        return;
        //    }
        //    else
        //    {
        //        // this.v_timer.Start();
        //    }
        //}

        private int getNewTurn(int currentTurn, bool wipe)
        {
            if (this.v_chargeServices == null || this.v_chargeServices.Count == 0)
                return -1;
            int turn = (currentTurn < 0 || currentTurn > this.v_chargeServices.Count - 1) ? 0 : currentTurn;
            if (this.v_chargeServices[turn].prp_remainRowCount > 0)
            {
                if (this.v_chargeServices[turn].prp_rowsProcessedInSecond < this.v_chargeServices[turn].prp_tpsServiceCurrent)
                {
                    return turn;
                }
                else
                {
                    this.v_chargeServices[turn].prp_rowsProcessedInSecond = 0;
                }
            }
            else
            {
                DateTime time = new DateTime(this.v_ticksStart);
                TimeSpan span = DateTime.Now - time;
                this.v_chargeServices[turn].sb_finishCharge((int)span.TotalMilliseconds / 1000, wipe);
                this.sb_assignServicesTPS(this.v_tpsTotal);
            }

            turn = currentTurn + 1;
            while (turn <= this.v_chargeServices.Count - 1)
            {
                if (this.v_chargeServices[turn].prp_remainRowCount > 0)
                {
                    if (this.v_chargeServices[turn].prp_rowsProcessedInSecond < this.v_chargeServices[turn].prp_tpsServiceCurrent)
                        return turn;
                    else this.v_chargeServices[turn].prp_rowsProcessedInSecond = 0;
                }
                else
                {
                    //this.sb_assignServicesTPS(this.v_tpsTotal);
                }
                turn++;
            }

            turn = 0;
            while (turn < currentTurn && turn <= this.v_chargeServices.Count - 1)
            {
                if (this.v_chargeServices[turn].prp_remainRowCount > 0)
                {
                    if (this.v_chargeServices[turn].prp_rowsProcessedInSecond < this.v_chargeServices[turn].prp_tpsServiceCurrent)
                        return turn;
                    else this.v_chargeServices[turn].prp_rowsProcessedInSecond = 0;
                }
                else
                {
                    //this.sb_assignServicesTPS(this.v_tpsTotal);
                }
                turn++;
            }

            return -1;
        }

        private void sb_chargeLoop(bool wipe, string chargingServiceName)
        {

            while (true)
            {
                #region throttle request
                if (this.v_ticksPrevious.HasValue)
                {
                    DateTime previousTime = new DateTime(this.v_ticksPrevious.Value);
                    //this.v_ticksPrevious = DateTime.Now.Ticks;
                    //previousTime 
                    TimeSpan span = DateTime.Now - previousTime;
                    if (span.TotalMilliseconds < this.v_intervalInMillisecond)
                    {
                        //Program.logs.Info("fast loop" + span.TotalMilliseconds);
                        while (span.TotalMilliseconds < this.v_intervalInMillisecond)
                        {
                            span = DateTime.Now - previousTime;
                        }
                    }
                }
                #endregion

                DateTime timeStart = DateTime.Now;

                this.v_turn = this.getNewTurn(this.v_turn, wipe);

                if (this.v_turn >= 0 && this.v_turn <= this.v_chargeServices.Count - 1)
                {
                    this.sb_charge(wipe, this.v_turn);
                }

                #region no rows remained
                if (this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Count() == 0)
                {
                    if (this.v_turn >= 0 && this.v_turn <= this.v_chargeServices.Count - 1)
                    {
                        DateTime time = new DateTime(this.v_ticksStart);
                        TimeSpan span = DateTime.Now - time;
                        this.v_chargeServices[this.v_turn].sb_finishCharge((int)span.TotalMilliseconds / 1000, wipe);
                    }
                    Console.WriteLine("-no turn remain1:" + v_taskCount);
                    break;
                }
                #endregion

                DateTime timeEnd = DateTime.Now;
                TimeSpan span2 = timeEnd - (this.v_ticksPrevious.HasValue ? (new DateTime(this.v_ticksPrevious.Value)) : timeEnd);
                this.v_ticksPrevious = DateTime.Now.Ticks;
                //Program.logs.Info(" diff:" + span2.ToString("c"));

                this.sb_notifyLongCharging(chargingServiceName);
            }

            Program.logs.Info("----------------------------------------");
            Program.logs.Info("----------------------------------------");

            //Program.logs.Info("total:" + ((new DateTime(this.v_ticksPrevious.Value)) - (new DateTime(this.v_ticksStart))).TotalMilliseconds);
            Console.WriteLine("taskRemain:" + v_taskCount);
            Program.logs.Info("taskRemain:" + v_taskCount);


            int taskCount = 0;
            while (v_taskCount > 0)
            {
                if (taskCount != v_taskCount)
                {
                    object obj = new object();
                    lock (obj) { taskCount = v_taskCount; }
                    Console.WriteLine("taskRemain:" + v_taskCount);
                    Program.logs.Warn("taskRemain:" + v_taskCount);
                }
                this.sb_notifyLongCharging(chargingServiceName);
            }
            Thread.Sleep(10000);//wait for saving to db is finished
            //this.sb_finish();
        }

        private void sb_notifyLongCharging(string chargingServiceName)
        {
            if (this.v_chargeServices == null || this.v_chargeServices.Count == 0) return;

            enum_chargingSpeed chargingSpeed = enum_chargingSpeed.normal;
            int totalRowCount = this.v_chargeServices.Sum(o => o.prp_rowCount);
            double bestSpeed;
            if (totalRowCount < this.v_tpsTotal)
                bestSpeed = 1;
            else bestSpeed = ((double)totalRowCount / this.v_tpsTotal);
            if (bestSpeed == 0) return;
            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - this.v_ticksStart);

            if (ts.TotalSeconds <= bestSpeed)
                return;
            else if (bestSpeed < ts.TotalSeconds &&
                ts.TotalSeconds <= bestSpeed * (int)enum_chargingSpeed.slow * 1.2)
            {
                chargingSpeed = enum_chargingSpeed.slow;
                return;
            }
            else if (bestSpeed * (int)enum_chargingSpeed.slow * 1.2 < ts.TotalSeconds &&
                ts.TotalSeconds <= bestSpeed * (int)enum_chargingSpeed.verySlow * 1.2)
            {
                chargingSpeed = enum_chargingSpeed.verySlow;
            }
            else
            {
                chargingSpeed = enum_chargingSpeed.tooSlow;
            }

            Program.logs.Info(chargingSpeed.ToString() + " starttime:" + (new DateTime(this.v_ticksStart)).ToString("HH:mm:ss.fff")
                + " datetimeNow" + DateTime.Now.ToString("HH:mm:ss.fff")
                + " rowCount = " + totalRowCount.ToString()
                + " difference = " + ts.ToString("c")
                + " bestSpeed=" + bestSpeed.ToString());
            int? connectionLimit = null;
            ServicePoint sp = this.v_spSettings.GetServicePoint();
            if (sp != null)
            {
                connectionLimit = sp.ConnectionLimit;
            }
            if ((new TimeSpan(DateTime.Now.Ticks - this.v_notifTime)).Minutes > 1)
            {
                //notif every one minute
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, chargingServiceName + " " + chargingSpeed.ToString() + ":" + ts.ToString("c") + " (Task Remain:" + v_taskCount.ToString() + ")" + "(Connection Limit:" + (connectionLimit.HasValue ? connectionLimit.Value.ToString() : "Null") + ")");
                this.v_notifTime = DateTime.Now.Ticks;
                if ((chargingSpeed == ChargingController.enum_chargingSpeed.verySlow && this.prp_resetVerySlowCharging)
                || (chargingSpeed == ChargingController.enum_chargingSpeed.tooSlow && this.prp_resetTooSlowCharging))
                {
                    if (v_taskCount == 0) return;
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Service is going to be restarted due to " + chargingSpeed.ToString() + " charging. The service should restart in a minute :"
                        + ts.ToString("c") + " (Task Remain:" + v_taskCount.ToString() + ")"
                        + "(Connection Limit:" + (connectionLimit.HasValue ? connectionLimit.Value.ToString() : "Null") + ")");
                    Environment.Exit(1);
                }
            }



            //TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - this.v_ticksStart);
            //if (ts.TotalMinutes / 120 > 1)
            //{
            //    if ((new TimeSpan(DateTime.Now.Ticks - this.v_notifTime)).Minutes > 1)
            //    {
            //        string connectionLimitStr = "Null";
            //        ServicePoint sp = this.v_spSettings.GetServicePoint();
            //        if (sp != null)
            //        {
            //            connectionLimitStr = sp.ConnectionLimit.ToString();
            //        }
            //        else
            //        {
            //            connectionLimitStr = "Null";

            //        }

            //        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, chargingServiceName + " Long Charging:" + ts.ToString("c") + " (Task Remain:" + v_taskCount.ToString() + ")" + "(Connection Limit:" + connectionLimitStr + ")");


            //        this.v_notifTime = DateTime.Now.Ticks;
            //    }

            //}
        }

        private void sb_charge(bool wipe, int turn)
        {

            if (turn < 0 || turn > this.v_chargeServices.Count - 1) return;



            Program.logs.Warn(";" + this.v_chargeServices[turn].prp_service.ServiceCode + ";"
                + this.v_chargeServices[turn].prp_rowsProcessedInSecond + ";"
                + this.v_chargeServices[turn].prp_rowIndex + ";"
                + this.v_chargeServices[turn].prp_remainRowCount + ";"
                + this.v_chargeServices[turn].prp_subscriberCurrent.mobileNumber + ";");

            ServiceCharge service = this.v_chargeServices[turn];
            SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber = service.prp_subscriberCurrent;
            service.prp_wipe = wipe;
            service.sb_charge(subscriber, this.v_chargeServices[turn].prp_cycleNumber, 0, 0, DateTime.Now);
            this.v_tps = this.v_tps + 1;
            this.v_chargeServices[turn].prp_rowsProcessedInSecond = this.v_chargeServices[turn].prp_rowsProcessedInSecond + 1;
            this.v_chargeServices[turn].prp_rowIndex = this.v_chargeServices[turn].prp_rowIndex + 1;

            Console.WriteLine(this.v_tps + "-" + this.v_chargeServices[turn].prp_service.ServiceCode + "-" + subscriber.mobileNumber);

        }
    }
}