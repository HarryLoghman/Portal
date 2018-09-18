using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DehnadMTNChargeServices
{
    class chargeServices
    {
        int v_turn;
        int v_tps;
        int v_tpsTotal;
        long v_ticksStart;
        Nullable<long> v_ticksPrevious;
        //System.Timers.Timer v_timer;
        List<ServiceCharge> v_chargeServices;
        
        public static int v_taskCount;
        object v_lockObj;
        SqlConnection v_cnn;

        double v_intervalInMillisecond;
        public bool prp_finished { get; set; }

        public chargeServices()
        {
            Uri uri = new Uri("http://92.42.55.180:8310/");
            ServicePoint sp = ServicePointManager.FindServicePoint(uri);
            sp.ConnectionLimit = 3000;
        }


        public void sb_chargeAll(int tpsTotal, List<ServiceCharge> chargeServices, long ticksStart)
        {

            v_taskCount = 0;
            this.v_lockObj = new object();
            this.prp_finished = false;
            this.v_tpsTotal = tpsTotal;
            
            this.v_turn = 0;

            this.v_chargeServices = new List<ServiceCharge>();

            int i;
            for (i = 0; i <= chargeServices.Count - 1; i++)
            {
                chargeServices[i].sb_fill();
                if (chargeServices[i].prp_rowCount > 0)
                {
                    Program.logs.Info(chargeServices[i].prp_serviceCode + ":start of installmentCycleNumber " + chargeServices[i].prp_cycleNumber);
                    Program.logs.Info(chargeServices[i].prp_serviceCode + ":installmentList final list count:" + chargeServices[i].prp_rowCount);
                    this.v_chargeServices.Add(chargeServices[i]);
                }
                else
                {
                    chargeServices[i].sb_finishCharge(0);
                }
            }

            if (this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Count() == 0)
            {
                this.prp_finished = true;
                return;
            }

            this.sb_resetTPSs();
            this.sb_assignServicesTPS(this.v_tpsTotal);

            this.v_cnn = SharedLibrary.publicVariables.GetConnectionPortal();
            this.v_cnn.Open();



            DateTime timeStart = new DateTime(ticksStart);
            Program.logs.Info("startTime:" + timeStart.ToString("HH:mm:ss,fff"));

            this.v_intervalInMillisecond = 1000.00 / tpsTotal;
            Program.logs.Info("interval:" + this.v_intervalInMillisecond + " totalRowCount " + this.v_chargeServices.Sum(o => o.prp_rowCount));

            Program.logs.Info("------------------------------------------------------");
            Program.logs.Info("------------------------------------------------------");

            this.v_ticksPrevious = null;
            this.v_ticksStart = DateTime.Now.Ticks;
            this.v_turn = 0;

            this.sb_chargeLoop();

            for (i = 0; i <= chargeServices.Count - 1; i++)
            {
                chargeServices[i].sb_fillWipe();
                if (chargeServices[i].prp_rowCount > 0)
                {
                    Program.logs.Info(chargeServices[i].prp_serviceCode + ":start of wipe " + chargeServices[i].prp_cycleNumber);
                    Program.logs.Info(chargeServices[i].prp_serviceCode + ":installmentList wipe list count:" + chargeServices[i].prp_rowCount);
                    this.v_chargeServices.Add(chargeServices[i]);
                }
                else
                {
                    chargeServices[i].sb_finishCharge(0);
                }
                
            }

            this.sb_resetTPSs();
            this.sb_assignServicesTPS(this.v_tpsTotal);

            this.sb_chargeLoop();
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

        private void sb_assignServicesTPS(int operatorTPS)
        {
            int i;
            int occupiedTPS = 0;
            int serviceTPS;
            occupiedTPS = this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Sum(o => o.prp_tpsServiceInitial);
            List<ServiceCharge> chargeService = this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).ToList();
            if (chargeService.Count == 1)
            {
                serviceTPS = chargeService[0].prp_tpsServiceInitial;
                chargeService[0].prp_tpsServiceCurrent = operatorTPS;
                Program.logs.Info("TPS ASSIGN:" + chargeService[0].prp_serviceCode + ";old tps:" + serviceTPS + ";newtps:" + chargeService[0].prp_tpsServiceCurrent.ToString());
                return;
            }
            int freeTps = this.v_chargeServices.Where(o => o.prp_remainRowCount == 0).Sum(o => o.prp_tpsServiceInitial);
            if (freeTps <= 0) return;
            //Program.logs.Info("ASSIGN:FREE" + freeTps);

            for (i = 0; i <= this.v_chargeServices.Count - 1; i++)
            {
                if (this.v_chargeServices[i].prp_remainRowCount > 0)
                {
                    serviceTPS = this.v_chargeServices[i].prp_tpsServiceInitial;
                    this.v_chargeServices[i].prp_tpsServiceCurrent = serviceTPS + ((serviceTPS * freeTps) / occupiedTPS);
                    Program.logs.Info("TPS ASSIGN:" + this.v_chargeServices[i].prp_serviceCode + ";old tps:" + serviceTPS + ";newtps:" + this.v_chargeServices[i].prp_tpsServiceCurrent.ToString());

                }
            }
        }

        private void sb_finish()
        {
            if (this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Count() == 0/*no remain rows*/
                && v_taskCount == 0/*wait till all task complete*/)
            {
                //   this.v_timer.Enabled = false;
                this.prp_finished = true;
                this.v_cnn.Close();
                return;
            }
            else
            {
                // this.v_timer.Start();
            }
        }

        private int getNewTurn(int currentTurn)
        {
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
                this.v_chargeServices[turn].sb_finishCharge((int)span.TotalMilliseconds / 1000);
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

        private void sb_chargeLoop()
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

                this.v_turn = this.getNewTurn(this.v_turn);

                if (this.v_turn >= 0 && this.v_turn <= this.v_chargeServices.Count - 1)
                {
                    this.sb_charge(this.v_turn);
                }

                #region no rows remained
                if (this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Count() == 0)
                {
                    if (this.v_turn >= 0 && this.v_turn <= this.v_chargeServices.Count - 1)
                    {
                        DateTime time = new DateTime(this.v_ticksStart);
                        TimeSpan span = DateTime.Now - time;
                        this.v_chargeServices[this.v_turn].sb_finishCharge((int)span.TotalMilliseconds / 1000);
                    }
                    Console.WriteLine("-no turn remain1:" + v_taskCount);
                    break;
                }
                #endregion

                DateTime timeEnd = DateTime.Now;
                TimeSpan span2 = timeEnd - (this.v_ticksPrevious.HasValue ? (new DateTime(this.v_ticksPrevious.Value)) : timeEnd);
                this.v_ticksPrevious = DateTime.Now.Ticks;
                Program.logs.Info(" diff:" + span2.ToString("c"));
            }

            Program.logs.Info("----------------------------------------");
            Program.logs.Info("----------------------------------------");

            Program.logs.Info("total:" + ((new DateTime(this.v_ticksPrevious.Value)) - (new DateTime(this.v_ticksStart))).TotalMilliseconds);
            Console.WriteLine("taskRemain:" + v_taskCount);
            Program.logs.Info("taskRemain:" + v_taskCount);

            int taskCount = v_taskCount;
            while (v_taskCount > 0)
            {
                if (taskCount != v_taskCount)
                {
                    Console.WriteLine("taskRemain:" + v_taskCount);
                    Program.logs.Info("taskRemain:" + v_taskCount);
                    taskCount = v_taskCount;
                }
            }

            this.sb_finish();
        }


        private void v_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //DateTime previousTime;
            //if (!this.v_ticksPrevious.HasValue)
            //    this.v_ticksPrevious = DateTime.Now.Ticks;

            //previousTime = new DateTime(this.v_ticksPrevious.Value);

            //TimeSpan span = DateTime.Now - previousTime;
            //int loop = 1;
            //if (span.TotalMilliseconds > this.v_timer.Interval)
            //{
            //    loop = (int)(span.TotalMilliseconds / this.v_timer.Interval);
            //}
            //else
            //{
            //    while (span.TotalMilliseconds < this.v_timer.Interval)
            //    {
            //        span = DateTime.Now - previousTime;
            //    }
            //}
            //if (loop > this.v_tpsTotal / 10)
            //{
            //    Program.logs.Info("loop is exceeded" + loop + ";" + span.TotalMilliseconds + ";" + this.v_timer.Interval + ";" + previousTime.ToString("HH:mm:ss,fff") + ";" + DateTime.Now.ToString("HH:mm:ss,fff"));
            //    this.v_timer.Enabled = false;
            //    this.prp_finished = true;
            //    this.v_cnn.Close();
            //    return;
            //}
            //int k1, k2;
            //System.Threading.ThreadPool.GetAvailableThreads(out k1, out k2);
            //if (k1 < 100 || k2 < 100)
            //{
            //    Program.logs.Info("workder thread " + k1 + " completion port thread " + k2);
            //}

            //int i;
            //for (i = 0; i <= loop - 1; i++)
            //{
            //    #region no rows remained
            //    if (this.v_chargeServices.Where(o => o.prp_remainRowCount > 0).Count() == 0
            //        && this.v_taskCount == 0)
            //    {
            //        Console.WriteLine("-no turn remain1:" + this.v_taskCount);
            //        this.sb_finish();

            //        return;
            //    }
            //    #endregion
            //    this.v_turn = this.getNewTurn(this.v_turn);

            //    if (this.v_turn < 0 || this.v_turn > this.v_chargeServices.Count - 1)
            //    {
            //        Console.WriteLine("-no turn remain2:" + this.v_taskCount);
            //        this.sb_finish();
            //        return;
            //    }
            //    this.sb_charge(this.v_turn);

            //    this.v_ticksPrevious = DateTime.Now.Ticks;
            //    this.v_timer.Start();
            //}
        }

        private void sb_charge(int turn)
        {

            if (turn < 0 || turn > this.v_chargeServices.Count - 1) return;

            bool isSucceeded;


            Program.logs.Info(";" + this.v_chargeServices[turn].prp_serviceCode + ";"
                + this.v_chargeServices[turn].prp_rowsProcessedInSecond + ";"
                + this.v_chargeServices[turn].prp_rowIndex + ";"
                + this.v_chargeServices[turn].prp_remainRowCount + ";"
                + this.v_chargeServices[turn].prp_subscriberCurrent.mobileNumber + ";");

            ServiceCharge service = this.v_chargeServices[turn];
            SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber = service.prp_subscriberCurrent;

            service.sb_chargeMtnSubscriber(this.v_cnn, subscriber,this.v_chargeServices[turn].prp_cycleNumber, 0, 0, DateTime.Now, out isSucceeded);
            this.v_tps = this.v_tps + 1;
            this.v_chargeServices[turn].prp_rowsProcessedInSecond = this.v_chargeServices[turn].prp_rowsProcessedInSecond + 1;
            this.v_chargeServices[turn].prp_rowIndex = this.v_chargeServices[turn].prp_rowIndex + 1;

            Console.WriteLine(this.v_tps + "-" + this.v_chargeServices[turn].prp_serviceCode + "-" + subscriber.mobileNumber);

        }
    }
}
