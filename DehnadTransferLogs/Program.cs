using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace DehnadTransferLogs
{

    class Program
    {

        public static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        static void Main(string[] args)
        {
            transferLog tl = new transferLog();
            tl.sb_transferLogFiles("E:\\Windows Services");
            tl.sb_transferLogFiles("E:\\Web\\App");
            tl.sb_transferLogFiles("E:\\Web\\Portal");
            tl.sb_transferLogFiles("E:\\Web\\Receive");
        }

        public static void Log(string msg)
        {
            Log(msg, false);
        }
        public static void Log(string msg, bool error)
        {
            Log(msg, error, null);
        }
        public static void Log(string msg, bool error, Exception ex)
        {
            if (error)
            {

                Console.WriteLine("Error:" + msg);
                if (ex == null)
                    Program.logs.Error(msg);
                else
                {
                    Program.logs.Error(msg, ex);
                    Console.WriteLine("Error:" + ex.Message);
                }
            }
            else
            {
                Console.WriteLine(msg);
                Program.logs.Info(msg);

            }
        }

    }
}
