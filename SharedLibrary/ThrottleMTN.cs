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
        public ThrottleMTN()
        {
            this.throttleConstructor(this.getOperatorTPS(), 1000, 10, Path.GetTempPath() + "MTNThrottleTPS", "MTNThrottle", "MTNThrottleSamephore");
        }
        public ThrottleMTN(string mapFilePath) : this(1000, 10, mapFilePath)
        {

        }
        public ThrottleMTN(int intervalInMillisecond, int safeMarginInMillisecond, string mapFilePath) : this(intervalInMillisecond, safeMarginInMillisecond, mapFilePath, "MTNThrottle", "MTNThrottleSamephore")
        {
        }
        public ThrottleMTN(int intervalInMillisecond, int safeMarginInMillisecond, string mapFilePath, string mappedFileName, string semaphoreName)
        {
            this.throttleConstructor(this.getOperatorTPS(), intervalInMillisecond, safeMarginInMillisecond, mapFilePath, mappedFileName, semaphoreName);
        }

        private int getOperatorTPS()
        {
            using (var entity = new PortalEntities())
            {
                Nullable<int> tps = entity.Operators.Where(o => o.OperatorName == "MTN").Select(o => o.tps).FirstOrDefault();
                return (tps.HasValue ? tps.Value : 180);
            }
        }
        private void throttleConstructor(int tps, int intervalInMillisecond, int safeMarginInMillisecond, string mapFilePath, string mappedFileName, string semaphoreName)
        {
            if (tps <= 0) throw new ArgumentException("TPS should be a positive value");
            if (tps > intervalInMillisecond) throw new ArgumentException("TPS should be lower than partLengthInMilliSecond");
            if (intervalInMillisecond < 0) throw new ArgumentException("intervalInMillisecond should be a positive value");
            if (intervalInMillisecond < safeMarginInMillisecond) throw new ArgumentException("intervalInMillisecond should be greater than safeMerginInMillisecond");
            if (intervalInMillisecond < 0) throw new ArgumentException("intervalInMillisecond should be a non negative value");
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

            if (!File.Exists(this.v_mapFilePath))
                File.Create(this.v_mapFilePath);
        }
        public void throttleRequests()
        {
            if (!File.Exists(this.v_mapFilePath))
            {
                throw new Exception(this.v_mapFilePath + " does not exists");
            }
            start:
            long temp;
            long ticksFile;
            int waitInMillisecond = 0;
            int count = 0;
            string str = "";
            object obj = new object();

            Semaphore smph;
            lock (obj)
            {
                if (!Semaphore.TryOpenExisting(this.v_smphName, out smph))
                {
                    smph = new Semaphore(1, 1, this.v_smphName);
                }

                smph.WaitOne();
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(this.v_mapFilePath, FileMode.OpenOrCreate, this.v_mappedFileName, 100))
                {

                    DateTime timeArrive = DateTime.Now;
                    long ticksNow = timeArrive.Ticks / TimeSpan.TicksPerMillisecond;

                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            str = reader.ReadString();
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
                        if (ticksFile <= ticksNow && ticksNow < ticksFile + this.v_intervalInMillisecond)
                        {
                            //1000 millisecond passed
                            count++;
                            int remain = ((count % this.v_tps) + 1);
                            int divider = (count / this.v_tps);

                            if (count >= this.v_tps)
                            {
                                waitInMillisecond = (int)((ticksFile + (this.v_intervalInMillisecond * divider) - ticksNow) + (this.v_safeMarginInMillisecond) * remain);
                            }
                            str = ticksFile + "," + count;

                        }
                        else
                        {
                            //ticksNow > ticksFile + this.v_intervalInMillisecond
                            //same second
                            count = 1;
                            str = ticksFile + (((ticksNow - ticksFile) / this.v_intervalInMillisecond) * this.v_intervalInMillisecond) + "," + count;

                        }



                    }
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        StreamWriter writer = new StreamWriter(stream);
                        writer.Write(str);
                        writer.Flush();
                    }
                    //Debug.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@" + str);


                }
                TimeSpan ts = new TimeSpan(int.Parse(str.Split(',')[0]) * 1000);
                logs.Warn(str + "," + waitInMillisecond + "," + ts.Hours.ToString() + ":" + ts.Minutes.ToString() + ":" + ts.Seconds.ToString() + "," + ts.Milliseconds.ToString());
                smph.Release();

                if (waitInMillisecond > 0)
                {
                    //Debug.WriteLine("&&&&&&&&&&Sleep" + waitInMillisecond);
                    Thread.Sleep(waitInMillisecond);

                    goto start;
                }

            }

        }
    }
}

