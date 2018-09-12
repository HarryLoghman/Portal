using SharedLibrary.Models;
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
    public class ThrottleMTN
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        int v_tps;
        int v_intervalInMillisecond;
        int v_safeMarginInMillisecond;
        string v_mapFilePath = "";
        string v_smphName = "";
        string v_mappedFileName = "";
        int v_waitForSemaphoreInMillisecond;
        int v_gap;
        private object v_lockObj = new object();
        public ThrottleMTN()
        {
            this.throttleConstructor(getOperatorTPS(), 1050, 10, Path.GetTempPath() + "MTNThrottleTPS", "MTNThrottle", "MTNThrottleSamephore", 10000);
        }
        public ThrottleMTN(string mapFilePath) : this(1050, 10, mapFilePath)
        {

        }
        public ThrottleMTN(int intervalInMillisecond, int safeMarginInMillisecond, string mapFilePath) : this(intervalInMillisecond, safeMarginInMillisecond, mapFilePath, "MTNThrottle", "MTNThrottleSamephore", 10000)
        {

        }
        public ThrottleMTN(int intervalInMillisecond, int safeMarginInMillisecond, string mapFilePath, string mappedFileName, string semaphoreName, int waitForSemaphoreInMillisecond)
        {
            throttleConstructor(getOperatorTPS(), intervalInMillisecond, safeMarginInMillisecond, mapFilePath, mappedFileName, semaphoreName, waitForSemaphoreInMillisecond);
        }

        public static int getOperatorTPS()
        {
            using (var entity = new PortalEntities())
            {
                Nullable<int> tps = entity.Operators.Where(o => o.OperatorName == "MTN").Select(o => o.tps).FirstOrDefault();
                return (tps.HasValue ? tps.Value : 180);
            }
        }
        private void throttleConstructor(int tps, int intervalInMillisecond, int safeMarginInMillisecond, string mapFilePath, string mappedFileName, string semaphoreName, int waitForSemaphoreInMillisecond)
        {
            if (tps <= 1) throw new ArgumentException("TPS should be greater than 1");
            if (tps >= intervalInMillisecond) throw new ArgumentException("TPS should be lower than intervalInMillisecond");
            if (intervalInMillisecond < 0) throw new ArgumentException("intervalInMillisecond should be a positive value");
            if (safeMarginInMillisecond < 0) throw new ArgumentException("safeMarginInMillisecond should be a non negative value");
            if (intervalInMillisecond < safeMarginInMillisecond) throw new ArgumentException("intervalInMillisecond should be greater than safeMerginInMillisecond");
            if (waitForSemaphoreInMillisecond <= 0) throw new ArgumentException("waitForSemaphoreInMillisecond should be a positive value");

            if (mapFilePath == "") throw new ArgumentException("MapFilePath is not specified");
            if (!Directory.Exists(Path.GetDirectoryName(mapFilePath))) throw new ArgumentException(Path.GetDirectoryName(mapFilePath) + " does not exist");
            if (mappedFileName == "") throw new ArgumentException("mappedFileName is not specified");
            if (semaphoreName == "") throw new ArgumentException("semaphoreName is not specified");

            this.v_tps = tps;
            this.v_intervalInMillisecond = intervalInMillisecond;
            this.v_safeMarginInMillisecond = safeMarginInMillisecond;
            this.v_mapFilePath = mapFilePath;
            this.v_mappedFileName = mappedFileName;
            this.v_smphName = semaphoreName;
            this.v_waitForSemaphoreInMillisecond = waitForSemaphoreInMillisecond;
            this.v_gap = (this.v_intervalInMillisecond / this.v_tps) + (this.v_intervalInMillisecond % this.v_tps > 0 ? 1 : 0); ;
            if (!File.Exists(this.v_mapFilePath))
                File.Create(this.v_mapFilePath);
        }
        //public int fnc_requestsCount()
        //{
        //    //logs.Warn(";start;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //    if (!File.Exists(v_mapFilePath))
        //    {
        //        throw new Exception(v_mapFilePath + " does not exists");
        //    }
        //    //logs.Warn(";before;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //    int count = 0;
        //    Semaphore smph;
        //    //logs.Warn(";after;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //    lock (v_lockObj)
        //    {
        //        //logs.Warn(";lockstart;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //        long temp;
        //        long ticksFile;
        //        //int waitInMillisecond = 0;

        //        string str = "";
        //        long ticksNow = 0;

        //        temp = ticksFile = ticksNow = count = 0;
        //        str = "";

        //        if (!Semaphore.TryOpenExisting(v_smphName, out smph))
        //        {
        //            smph = new Semaphore(1, 1, v_smphName);
        //        }

        //        //logs.Warn(";beforeSema;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //        smph.WaitOne();
        //        //logs.Warn(";startsema;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //        using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(v_mapFilePath, FileMode.OpenOrCreate, v_mappedFileName, 25))
        //        {

        //            DateTime timeArrive = DateTime.Now;
        //            ticksNow = timeArrive.Ticks / TimeSpan.TicksPerMillisecond;

        //            using (MemoryMappedViewStream stream = mmf.CreateViewStream())
        //            {
        //                using (StreamReader reader = new StreamReader(stream))
        //                {

        //                    str = reader.ReadToEnd();
        //                    //logs.Warn("str-:" + str);
        //                    str = str.Replace("\0", "").Replace("\u0011", "");
        //                    //logs.Warn("str+:" + str);
        //                    reader.Close();
        //                    reader.Dispose();
        //                }
        //            }

        //            if (str == "" || str.Split(',').Where(o => long.TryParse(o, out temp) == false).Count() > 0)
        //            {
        //                count = 0;
        //            }
        //            else
        //            {
        //                string[] strParts = str.Split(',');
        //                ticksFile = long.Parse(strParts[0]);

        //                count = int.Parse(strParts[1]);


        //                //Debug.WriteLine("ticksFile" + ticksFile + "///ticksNow" + ticksNow + "///count:" + count);
        //                //logs.Warn(ticksFile + "," + ticksNow + "," + (ticksFile + this.v_intervalInMillisecond).ToString());
        //                if (ticksFile <= ticksNow && ticksNow < ticksFile + v_intervalInMillisecond)
        //                {
        //                    //1000 millisecond passed

        //                    //count = count + 1;

        //                    //int remain = ((count % v_tps) + 1);
        //                    //int divider = (count / v_tps);

        //                    //if (count >= v_tps)
        //                    //{
        //                    //    waitInMillisecond = (int)((ticksFile + (v_intervalInMillisecond * divider) - ticksNow) + (v_safeMarginInMillisecond) * remain);
        //                    //}
        //                    //str = ticksFile + "," + count;

        //                }
        //                else
        //                {
        //                    //ticksNow > ticksFile + this.v_intervalInMillisecond
        //                    //same second
        //                    count = 1;
        //                    //str = ticksFile + (((ticksNow - ticksFile) / v_intervalInMillisecond) * v_intervalInMillisecond) + "," + count;

        //                }



        //            }
        //            //while (str.Length < 25)
        //            //    str = str + " ";
        //            //using (MemoryMappedViewStream stream = mmf.CreateViewStream())
        //            //{
        //            //    StreamWriter writer = new StreamWriter(stream);

        //            //    writer.Write(str);
        //            //    writer.Flush();
        //            //    writer.Close();
        //            //    writer.Dispose();
        //            //}

        //            //Debug.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@" + str);


        //        }



        //        smph.Release();
        //        //TimeSpan ts;
        //        //if (waitInMillisecond > 0)
        //        //{
        //        //    DateTime time = (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond));
        //        //    ts = time - DateTime.Now.Date;
        //        //    DateTime nextTime = time.AddMilliseconds(waitInMillisecond);
        //        //    logs.Warn(";" + serviceName + ";" + mobileNumber + ";" + guid + ";" + str.Split(',')[1] + ";" + waitInMillisecond + ";" + ts.ToString("c") + ";" + nextTime.ToString("hh:mm:ss,fff"));
        //        //    //Debug.WriteLine("&&&&&&&&&&Sleep" + waitInMillisecond);
        //        //    Thread.Sleep(waitInMillisecond);
        //        //    temp = ticksFile = ticksNow = waitInMillisecond = count = 0;
        //        //    str = "";
        //        //    goto start;
        //        //}
        //        //ts = (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond)) - DateTime.Now.Date;
        //        //logs.Warn(";" + serviceName + ";" + mobileNumber + ";" + guid + ";" + str.Split(',')[1] + ";" + waitInMillisecond + ";" + ts.ToString("c"));

        //    }
        //    return count;
        //}

        //public bool fnc_canContinue()
        //{
        //    if (this.fnc_requestsCount() >= this.v_tps)
        //        return false;
        //    return true;
        //}

        //public void sb_addCounter()
        //{
        //    //logs.Warn(";start;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //    if (!File.Exists(v_mapFilePath))
        //    {

        //        throw new Exception(v_mapFilePath + " does not exists");
        //    }
        //    //logs.Warn(";before;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //    //start:
        //    Semaphore smph;
        //    //logs.Warn(";after;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //    lock (v_lockObj)
        //    {
        //        //logs.Warn(";lockstart;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //        long temp;
        //        long ticksFile;
        //        int waitInMillisecond = 0;
        //        int count = 0;
        //        string str = "";
        //        long ticksNow = 0;

        //        temp = ticksFile = ticksNow = waitInMillisecond = count = 0;
        //        str = "";

        //        if (!Semaphore.TryOpenExisting(v_smphName, out smph))
        //        {
        //            smph = new Semaphore(1, 1, v_smphName);
        //        }

        //        //logs.Warn(";beforeSema;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //        smph.WaitOne();
        //        //logs.Warn(";startsema;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
        //        using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(v_mapFilePath, FileMode.OpenOrCreate, v_mappedFileName, 25))
        //        {

        //            DateTime timeArrive = DateTime.Now;
        //            ticksNow = timeArrive.Ticks / TimeSpan.TicksPerMillisecond;

        //            using (MemoryMappedViewStream stream = mmf.CreateViewStream())
        //            {
        //                using (StreamReader reader = new StreamReader(stream))
        //                {

        //                    str = reader.ReadToEnd();
        //                    //logs.Warn("str-:" + str);
        //                    str = str.Replace("\0", "").Replace("\u0011", "");
        //                    //logs.Warn("str+:" + str);
        //                    reader.Close();
        //                    reader.Dispose();
        //                }
        //            }

        //            if (str == "" || str.Split(',').Where(o => long.TryParse(o, out temp) == false).Count() > 0)
        //            {
        //                str = ticksNow + "," + 1;
        //            }
        //            else
        //            {
        //                string[] strParts = str.Split(',');
        //                ticksFile = long.Parse(strParts[0]);

        //                count = int.Parse(strParts[1]);


        //                //Debug.WriteLine("ticksFile" + ticksFile + "///ticksNow" + ticksNow + "///count:" + count);
        //                //logs.Warn(ticksFile + "," + ticksNow + "," + (ticksFile + this.v_intervalInMillisecond).ToString());
        //                if (ticksFile <= ticksNow && ticksNow < ticksFile + v_intervalInMillisecond)
        //                {
        //                    //1000 millisecond passed

        //                    count = count + 1;

        //                    //int remain = ((count % v_tps) + 1);
        //                    //int divider = (count / v_tps);

        //                    //if (count >= v_tps)
        //                    //{
        //                    //    waitInMillisecond = (int)((ticksFile + (v_intervalInMillisecond * divider) - ticksNow) + (v_safeMarginInMillisecond) * remain);
        //                    //}
        //                    str = ticksFile + "," + count;

        //                }
        //                else
        //                {
        //                    //ticksNow > ticksFile + this.v_intervalInMillisecond
        //                    //same second
        //                    count = 1;
        //                    str = ticksFile + (((ticksNow - ticksFile) / v_intervalInMillisecond) * v_intervalInMillisecond) + "," + count;

        //                }



        //            }
        //            while (str.Length < 25)
        //                str = str + " ";
        //            using (MemoryMappedViewStream stream = mmf.CreateViewStream())
        //            {
        //                StreamWriter writer = new StreamWriter(stream);

        //                writer.Write(str);
        //                writer.Flush();
        //                writer.Close();
        //                writer.Dispose();
        //            }

        //            //Debug.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@" + str);


        //        }



        //        smph.Release();
        //        //TimeSpan ts;
        //        //if (waitInMillisecond > 0)
        //        //{
        //        //    DateTime time = (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond));
        //        //    ts = time - DateTime.Now.Date;
        //        //    DateTime nextTime = time.AddMilliseconds(waitInMillisecond);
        //        //    logs.Warn(";" + serviceName + ";" + mobileNumber + ";" + guid + ";" + str.Split(',')[1] + ";" + waitInMillisecond + ";" + ts.ToString("c") + ";" + nextTime.ToString("hh:mm:ss,fff"));
        //        //    //Debug.WriteLine("&&&&&&&&&&Sleep" + waitInMillisecond);
        //        //    Thread.Sleep(waitInMillisecond);
        //        //    temp = ticksFile = ticksNow = waitInMillisecond = count = 0;
        //        //    str = "";
        //        //    goto start;
        //        //}
        //        //ts = (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond)) - DateTime.Now.Date;
        //        //logs.Warn(";" + serviceName + ";" + mobileNumber + ";" + guid + ";" + str.Split(',')[1] + ";" + waitInMillisecond + ";" + ts.ToString("c"));
        //    }

        //}

        public DateTime throttleRequests(string serviceName, string mobileNumber, string guid)
        {
            if (!File.Exists(v_mapFilePath))
            {

                throw new Exception(v_mapFilePath + " does not exists");
            }

            Semaphore smph;

            lock (v_lockObj)
            {
                long temp;
                long ticksFile;
                string str = "";
                long ticksNow = 0;

                temp = ticksFile = ticksNow = 0;
                str = "";

                if (!Semaphore.TryOpenExisting(v_smphName, out smph))
                {
                    smph = new Semaphore(1, 1, v_smphName);
                }
                //logs.Warn("beforeWait");
                smph.WaitOne(this.v_waitForSemaphoreInMillisecond);
                //logs.Warn("afterWait");
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(v_mapFilePath, FileMode.OpenOrCreate, v_mappedFileName, 25))
                {

                    DateTime timeArrive = DateTime.Now;
                    ticksNow = timeArrive.Ticks / TimeSpan.TicksPerMillisecond;

                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {

                            str = reader.ReadToEnd();
                            str = str.Replace("\0", "").Replace("\u0011", "");
                            reader.Close();
                            reader.Dispose();
                        }
                    }
                    //logs.Warn("after read:" + str);

                    if (str == "" || str.Split(',').Where(o => long.TryParse(o, out temp) == false).Count() > 0)
                    {
                        str = ticksNow + "," + 1;
                    }
                    else
                    {
                        string[] strParts = str.Split(',');
                        ticksFile = long.Parse(strParts[0]);


                        //long tick = ticksNow;
                        if (ticksNow <= ticksFile + this.v_gap)
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            long waitMil = ticksFile + this.v_gap - ticksNow;
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

                        str = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString() + "," + "1";


                    }

                    while (str.Length < 25)
                        str = str + " ";
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        StreamWriter writer = new StreamWriter(stream);
                        //logs.Warn("before write:" + str);
                        writer.Write(str);
                        //logs.Warn("afterWrite:" + str);
                        writer.Flush();
                        writer.Close();
                        writer.Dispose();
                    }

                }

                smph.Release();
                TimeSpan ts;
                ts = (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond)) - DateTime.Now.Date;
                //if (serviceName == "porshetab")
                //    logs.Warn(";" + serviceName + ";" + mobileNumber + ";" + guid + ";" + new DateTime((long.Parse(str.Split(',')[0])) * TimeSpan.TicksPerMillisecond).ToString("HH:mm:ss,fff") + ";" + str.Split(',')[1] + ";" + ts.ToString("c"));
                return (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond)).AddMilliseconds(this.v_gap);
            }

        }


        public DateTime throttleRequestsOld(string serviceName, string mobileNumber, string guid)
        {
            //logs.Warn(";start;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
            if (!File.Exists(v_mapFilePath))
            {

                throw new Exception(v_mapFilePath + " does not exists");
            }
            //logs.Warn(";before;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
            start:
            Semaphore smph;
            //logs.Warn(";after;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
            lock (v_lockObj)
            {
                //logs.Warn(";lockstart;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
                long temp;
                long ticksFile;
                int waitInMillisecond = 0;
                int count = 0;
                string str = "";
                long ticksNow = 0;

                temp = ticksFile = ticksNow = waitInMillisecond = count = 0;
                str = "";

                if (!Semaphore.TryOpenExisting(v_smphName, out smph))
                {
                    smph = new Semaphore(1, 1, v_smphName);
                }

                //logs.Warn(";beforeSema;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
                smph.WaitOne(this.v_waitForSemaphoreInMillisecond);
                //logs.Warn(";startsema;" + serviceName + ";" + mobileNumber + ";" + guid + ";");
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(v_mapFilePath, FileMode.OpenOrCreate, v_mappedFileName, 25))
                {

                    DateTime timeArrive = DateTime.Now;
                    ticksNow = timeArrive.Ticks / TimeSpan.TicksPerMillisecond;

                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {

                            str = reader.ReadToEnd();
                            //logs.Warn("str-:" + str);
                            str = str.Replace("\0", "").Replace("\u0011", "");
                            //logs.Warn("str+:" + str);
                            reader.Close();
                            reader.Dispose();
                        }
                    }

                    if (str == "" || str.Split(',').Where(o => long.TryParse(o, out temp) == false).Count() > 0)
                    {
                        str = ticksNow + "," + 1;
                    }
                    else
                    {
                        string[] strParts = str.Split(',');
                        ticksFile = long.Parse(strParts[0]);

                        count = int.Parse(strParts[1]);


                        //Debug.WriteLine("ticksFile" + ticksFile + "///ticksNow" + ticksNow + "///count:" + count);
                        //logs.Warn(ticksFile + "," + ticksNow + "," + (ticksFile + this.v_intervalInMillisecond).ToString());
                        if (ticksFile <= ticksNow && ticksNow < ticksFile + v_intervalInMillisecond)
                        {
                            //1000 millisecond passed

                            count = count + 1;

                            int remain = ((count % v_tps) + 1);
                            int divider = (count / v_tps);

                            if (count >= v_tps)
                            {
                                waitInMillisecond = (int)((ticksFile + (v_intervalInMillisecond * divider) - ticksNow) + (v_safeMarginInMillisecond) * remain);
                            }
                            str = ticksFile + "," + count;

                        }
                        else
                        {
                            //ticksNow > ticksFile + this.v_intervalInMillisecond
                            //same second
                            count = 1;
                            //str = ticksFile + (((ticksNow - ticksFile) / v_intervalInMillisecond) * v_intervalInMillisecond) + "," + count;

                            str = ticksNow + "," + count;

                        }



                    }

                    while (str.Length < 25)
                        str = str + " ";
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        StreamWriter writer = new StreamWriter(stream);

                        writer.Write(str);
                        writer.Flush();
                        writer.Close();
                        writer.Dispose();
                    }

                    //Debug.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@" + str);


                }



                smph.Release();
                TimeSpan ts;
                if (waitInMillisecond > 0)
                {
                    DateTime time = (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond));
                    ts = time - DateTime.Now.Date;
                    DateTime nextTime = time.AddMilliseconds(waitInMillisecond);
                    //logs.Warn(";" + serviceName + ";" + mobileNumber + ";" + guid + ";" + str.Split(',')[1] + ";" + waitInMillisecond + ";" + ts.ToString("c") + ";" + nextTime.ToString("hh:mm:ss,fff"));
                    //Debug.WriteLine("&&&&&&&&&&Sleep" + waitInMillisecond);
                    Thread.Sleep(waitInMillisecond);
                    temp = ticksFile = ticksNow = waitInMillisecond = count = 0;
                    str = "";
                    goto start;
                }
                ts = (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond)) - DateTime.Now.Date;
                logs.Warn(";" + serviceName + ";" + mobileNumber + ";" + guid + ";" + new DateTime((long.Parse(str.Split(',')[0])) * TimeSpan.TicksPerMillisecond).ToString("HH:mm:ss,fff") + ";" + str.Split(',')[1] + ";" + waitInMillisecond + ";" + ts.ToString("c"));
                return (new DateTime(long.Parse(str.Split(',')[0]) * TimeSpan.TicksPerMillisecond)).AddMilliseconds(this.v_intervalInMillisecond);
            }

        }
    }
}

