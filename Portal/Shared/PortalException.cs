using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.Shared
{
    public class PortalException : System.Exception
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void Throw(string errorString)
        {
            logs.Error(errorString);
            throw new Exception(errorString);
        }
        public static void Throw(Exception exp,string errorString)
        {
            logs.Error(errorString + exp);
            throw new Exception(errorString);
        }
    }
}