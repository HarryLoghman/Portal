using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using SharedLibrary.Models;

namespace DehnadJhoobinService
{
    public class GetData
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void GetJoobinData()
        {
            try
            {
                
            }
            catch (Exception e)
            {
                logs.Error("Error in GetJoobinData: ", e);
            }
        }
    }
}
