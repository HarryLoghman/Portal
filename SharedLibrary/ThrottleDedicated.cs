using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class ThrottleDedicated
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        Nullable<long> v_previousExecute = null;
        string v_mapFilePath;

        int v_intervalInMillisecond;
        string v_smphName = "";
        string v_mappedFileName = "";
        private object v_lockObj = new object();
        int v_waitForSemaphoreInMillisecond;
        long v_resetCounterInMillisecond;
        public event EventHandler ev_tpsOccupiedChanged;

        public ThrottleDedicated()
        {
            this.throttleConstructor(1050, Path.GetTempPath() + "MTNThrottleTpsOccupied", "MTNThrottleOccupied", "MTNThrottleOccupiedSamephore", 10000, 3 * 60 * 60 * 1000);
        }
        public ThrottleDedicated(string mapFilePath) : this(1050, mapFilePath)
        {

        }
        public ThrottleDedicated(int intervalInMillisecond, string mapFilePath) : this(intervalInMillisecond, mapFilePath, "MTNThrottleOccupied", "MTNThrottleOccupiedSamephore", 10000, 3 * 60 * 60 * 1000)
        {

        }
        public ThrottleDedicated(int intervalInMillisecond, string mapFilePath, string mappedFileName, string semaphoreName, int waitForSemaphoreInMillisecond, long resetCounterInMillisecond)
        {
            throttleConstructor(intervalInMillisecond, mapFilePath, mappedFileName, semaphoreName, waitForSemaphoreInMillisecond, resetCounterInMillisecond);
        }


        private void throttleConstructor(int intervalInMillisecond, string mapFilePath, string mappedFileName, string semaphoreName, int waitForSemaphoreInMillisecond, long resetCounterInMillisecond)
        {
            if (intervalInMillisecond < 0) throw new ArgumentException("intervalInMillisecond should be a positive value");


            if (mapFilePath == "") throw new ArgumentException("MapFilePath is not specified");
            if (!Directory.Exists(Path.GetDirectoryName(mapFilePath))) throw new ArgumentException(Path.GetDirectoryName(mapFilePath) + " does not exist");
            if (mappedFileName == "") throw new ArgumentException("mappedFileName is not specified");
            if (semaphoreName == "") throw new ArgumentException("semaphoreName is not specified");
            if (waitForSemaphoreInMillisecond <= 0) throw new ArgumentException("waitForSemaphoreInMillisecond should be a positive value");
            if (resetCounterInMillisecond <= 0) throw new ArgumentException("resetCounterInMillisecond should be a positive value");

            this.v_intervalInMillisecond = intervalInMillisecond;

            this.v_mapFilePath = mapFilePath;
            this.v_mappedFileName = mappedFileName;
            this.v_smphName = semaphoreName;
            this.v_waitForSemaphoreInMillisecond = waitForSemaphoreInMillisecond;
            this.v_resetCounterInMillisecond = resetCounterInMillisecond;
            if (!File.Exists(this.v_mapFilePath))
                File.Create(this.v_mapFilePath);
        }

        public int getOccupiedTpsCount(int operatorTps)
        {
            int occupiedTpsCount = operatorTps;
            if (!File.Exists(v_mapFilePath))
            {
                return operatorTps;
            }
            string str = "";
            using (var fs = new FileStream(this.v_mapFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    str = sr.ReadToEnd();
                    str = str.Replace("\0", "").Replace("\u0011", "");

                    long temp;
                    long ticksNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                    if (str == "" || str.Split(',').Where(o => long.TryParse(o, out temp) == false).Count() > 0)
                    {
                        return 0;
                    }
                    else
                    {
                        long ticksFile = long.Parse(str.Split(',')[0]);
                        DateTime dateFile = new DateTime(ticksFile * TimeSpan.TicksPerMillisecond);
                        if (DateTime.Now > dateFile.AddMilliseconds(this.v_resetCounterInMillisecond))
                        {
                            return 0;
                        }
                        else
                        {
                            occupiedTpsCount = int.Parse(str.Split(',')[1]);
                            return occupiedTpsCount;
                        }

                    }
                    //if (int.TryParse(str, out occupiedTpsCount)) return occupiedTpsCount;
                    //else return 0;
                }
            }
            return occupiedTpsCount;


        }

        public void occupyTps(string serviceName, int tps, int operatorTps)
        {
            if (!File.Exists(v_mapFilePath))
            {

                File.Create(this.v_mapFilePath);
            }

            Semaphore smph;

            lock (v_lockObj)
            {

                int oldOccupiedTps = this.getOccupiedTpsCount(operatorTps);
                int newOccupiedTps = oldOccupiedTps;
                if (oldOccupiedTps + tps > operatorTps) return;
                newOccupiedTps = tps + oldOccupiedTps;

                if (!Semaphore.TryOpenExisting(v_smphName, out smph))
                {
                    smph = new Semaphore(1, 1, v_smphName);
                }

                smph.WaitOne(this.v_waitForSemaphoreInMillisecond);

                string str = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString() + "," + newOccupiedTps;

                File.WriteAllText(v_mapFilePath, str);
                //using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(v_mapFilePath, FileMode.OpenOrCreate, v_mappedFileName))
                //{
                //    string str = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString() + "," + (occupiedTps + tps).ToString();
                //    while (str.Length < 25)
                //        str = str + " ";
                //    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                //    {
                //        StreamWriter writer = new StreamWriter(stream);



                //        writer.Write(str);
                //        writer.Flush();
                //        writer.Close();
                //        writer.Dispose();
                //    }

                //}

                smph.Release();
                logs.Warn(serviceName + ";" + oldOccupiedTps + ";" + newOccupiedTps + ";" + tps + ";" + operatorTps);
                this.ev_tpsOccupiedChanged?.Invoke(this, null);
            }


        }

        public void releaseTps(string serviceName, int tps, int operatorTps)
        {
            if (!File.Exists(v_mapFilePath))
            {
                File.Create(this.v_mapFilePath);
            }

            Semaphore smph;

            lock (v_lockObj)
            {
                int oldOccupiedTps = this.getOccupiedTpsCount(operatorTps);
                int newOccupiedTps = oldOccupiedTps;
                if (oldOccupiedTps >= tps) newOccupiedTps = (oldOccupiedTps - tps);
                else newOccupiedTps = 0;

                if (!Semaphore.TryOpenExisting(v_smphName, out smph))
                {
                    smph = new Semaphore(1, 1, v_smphName);
                }

                smph.WaitOne(this.v_waitForSemaphoreInMillisecond);

                string str = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString() + "," + newOccupiedTps;

                File.WriteAllText(v_mapFilePath, str);
                //using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(v_mapFilePath, FileMode.OpenOrCreate, v_mappedFileName))
                //{


                //    str = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString() + "," + str;
                //    while (str.Length < 25)
                //        str = str + " ";
                //    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                //    {
                //        StreamWriter writer = new StreamWriter(stream, false);


                //        writer.Write(str);
                //        writer.Flush();
                //        writer.Close();
                //        writer.Dispose();
                //    }

                //}
                logs.Warn(serviceName + ";" + oldOccupiedTps + ";" + newOccupiedTps + ";" + tps + ";" + operatorTps);
                smph.Release();

                this.ev_tpsOccupiedChanged?.Invoke(this, null);
            }
        }

        public int getMaxTps(int tpsOperator, int tps)
        {
            int freeTps = 0;
            int occupiedTps = this.getOccupiedTpsCount(tpsOperator);

            if (tpsOperator > tps && occupiedTps < tpsOperator) freeTps = tpsOperator - occupiedTps;

            if (occupiedTps >= tpsOperator || tpsOperator <= tps) { freeTps = 0; occupiedTps = tpsOperator; }
            int currentTps = ((freeTps * tps) / occupiedTps) + tps;
            if (currentTps > tpsOperator) return tps;
            return currentTps;
        }
        public DateTime throttleRequests(string serviceName, string mobileNumber, string guid, int tps)
        {
            long ticksNow = 0;
            if (tps <= 1) throw new ArgumentException("TPS should be greater than 1");
            if (tps >= this.v_intervalInMillisecond) throw new ArgumentException("TPS should be lower than intervalInMillisecond");


            int gap = this.v_intervalInMillisecond / tps + (this.v_intervalInMillisecond % tps > 0 ? 1 : 0);

            DateTime timeArrive = DateTime.Now;
            ticksNow = timeArrive.Ticks / TimeSpan.TicksPerMillisecond;
            object obj = new object();
            if (!this.v_previousExecute.HasValue)
            {
                this.v_previousExecute = ticksNow;
            }
            else
            {
                //long tick = ticksNow;
                if (ticksNow <= this.v_previousExecute + gap)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    long waitMil = this.v_previousExecute.Value + gap - ticksNow;
                    while (true)
                    {
                        if (sw.ElapsedMilliseconds > waitMil)
                        {
                            sw.Stop();
                            break;
                        }
                        //long tickTick = DateTime.Now.Ticks;
                        //tick = tickTick / TimeSpan.TicksPerMillisecond;
                    }
                }
                lock (obj)
                { this.v_previousExecute = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond); }
            }
            //TimeSpan ts;
            //ts = (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond)) - DateTime.Now.Date;
            //logs.Warn(";" + serviceName + ";" + mobileNumber + ";" + guid + ";" + new DateTime(this.v_previousExecute.Value * TimeSpan.TicksPerMillisecond).ToString("HH:mm:ss,fff") + ";" + new DateTime(ticksNow * TimeSpan.TicksPerMillisecond).ToString("HH:mm:ss,fff"));
            return (new DateTime(this.v_previousExecute.Value * TimeSpan.TicksPerMillisecond)).AddMilliseconds(gap);

        }
    }
}