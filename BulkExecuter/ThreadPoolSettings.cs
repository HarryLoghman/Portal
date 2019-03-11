using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;

namespace BulkExecuter
{
    class ThreadPoolSettings : ConfigurationSection
    {
        private static ThreadPoolSettings settings = ConfigurationManager.GetSection("ThreadPoolSettings") as ThreadPoolSettings;

        public void Assign()
        {
            int maxW, maxIO, minW, minIO;
            maxW = settings.MaxWorkderThreads;
            maxIO = settings.MaxCompletionPortThreads;
            minW = settings.MinWorkderThreads;
            minIO = settings.MinCompletionPortThreads;
            if (maxW < minW)
            {
                throw new Exception("MaxWorkderThreads is smaller than MinWorkderThreads");
            }
            if (maxIO < minIO)
            {
                throw new Exception("MaxCompletionPortThreads is smaller than MaxCompletionPortThreads");
            }
            if (ThreadPool.SetMaxThreads(maxW, maxIO))
                ThreadPool.SetMinThreads(minW, minIO);
            else if (ThreadPool.SetMinThreads(minW, minIO))
                ThreadPool.SetMaxThreads(maxW, maxIO);
        }

        public static ThreadPoolSettings Settings
        {
            get
            {
                return settings;
            }
        }

        [ConfigurationProperty("MaxWorkderThreads")]
        public int MaxWorkderThreads
        {
            get
            {
                object temp = this["MaxWorkderThreads"];
                int value = 4000;
                if (temp == null || temp == DBNull.Value || string.IsNullOrEmpty(temp.ToString()) || !int.TryParse(temp.ToString(), out value))
                    value = 4000;
                else value = int.Parse(temp.ToString());

                int maxW, maxIo;
                ThreadPool.GetMaxThreads(out maxW, out maxIo);
                if (maxW > value) value = maxW;
                return value;
            }


            set { this["MaxWorkderThreads"] = value; }
        }

        [ConfigurationProperty("MaxCompletionPortThreads")]
        public int MaxCompletionPortThreads
        {
            get
            {
                object temp = this["MaxCompletionPortThreads"];
                int value = 4000;
                if (temp == null || temp == DBNull.Value || string.IsNullOrEmpty(temp.ToString()) || !int.TryParse(temp.ToString(), out value))
                    value = 4000;
                else value = int.Parse(temp.ToString());

                int maxW, maxIo;
                ThreadPool.GetMaxThreads(out maxW, out maxIo);
                if (maxIo > value) value = maxIo;
                return value;
            }


            set { this["MaxCompletionPortThreads"] = value; }
        }

        [ConfigurationProperty("MinWorkderThreads")]
        public int MinWorkderThreads
        {
            get
            {
                object temp = this["MinWorkderThreads"];
                int value = 200;
                if (temp == null || temp == DBNull.Value || string.IsNullOrEmpty(temp.ToString()) || !int.TryParse(temp.ToString(), out value))
                    value = 200;
                else value = int.Parse(temp.ToString());

                int minW, minIo;
                ThreadPool.GetMinThreads(out minW, out minIo);
                if (minW > value) value = minW;
                return value;
            }


            set { this["MinWorkderThreads"] = value; }
        }

        [ConfigurationProperty("MinCompletionPortThreads")]
        public int MinCompletionPortThreads
        {
            get
            {
                object temp = this["MinCompletionPortThreads"];
                int value = 200;
                if (temp == null || temp == DBNull.Value || string.IsNullOrEmpty(temp.ToString()) || !int.TryParse(temp.ToString(), out value))
                    value = 200;
                else value = int.Parse(temp.ToString());

                int minW, minIo;
                ThreadPool.GetMinThreads(out minW, out minIo);
                if (minIo > value) value = minIo;
                return value;
            }


            set { this["MinCompletionPortThreads"] = value; }
        }
    }
}


