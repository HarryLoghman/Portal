using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace BulkLibrary
{

    class Program
    {
        internal static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        internal static string v_cnnStr = "Data source =.; initial catalog = portal; integrated security = True; max pool size=4000; multipleactiveresultsets=True;connection timeout=180 ;";
    }
}
