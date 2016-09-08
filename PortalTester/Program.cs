using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalTester
{
    class Program
    {
        static void Main(string[] args)
        {
            RunTest();
        }

        public static void RunTest()
        {
            var a = new SharedLibrary.Models.ReceievedMessage();
            a.MobileNumber = "09125612694";
            a.ShortCode = "301714";
            a.Content = "222";
            a.IsReceivedFromIntegratedPanel = false;
            a.IsProcessed = false;
            DehnadReceiveProcessorService.MessageProcesser.HandleReceivedMessage(a);
        }
    }
}
