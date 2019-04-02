using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace BulkExecuter
{
    public enum enum_sendingSpeed
    {
        normal = 1,
        slow = 2,
        verySlow = 4,
        tooSlow = 8
    }
    class Program
    {
        
        internal static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        internal static string v_currentDirecoty;
        static int v_bulkId;
        static void Main(string[] args)
        {
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
            v_currentDirecoty = di.Name;

            //args = new string[] { "5" };
            string exceptionStr;

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            try
            {
                if (args == null || args.Length == 0)
                {
                    exceptionStr = "BulkExecuter:Program:Main,BulkId is not specified as a first argument";
                    logs.Error(exceptionStr);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                    Environment.Exit(0);
                    return;
                }
                int bulkId;
                if (!int.TryParse(args[0], out bulkId))
                {
                    exceptionStr = "BulkExecuter:Program:Main,BulkId is not an integer number(bulkId='" + args[0] + "')";
                    logs.Error(exceptionStr);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                    Environment.Exit(0);
                    return;
                }
                v_bulkId = bulkId;
                SharedLibrary.MessageHandler.BulkStatus bulkStatus;
                bool canStart = BulkControllerList.fnc_checkBulkStatus(bulkId, out exceptionStr, out bulkStatus);
                if (!canStart)
                {
                    exceptionStr = "BulkExecuter:Program:Main," + exceptionStr;
                    logs.Error(exceptionStr);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                    Environment.Exit(0);
                    return;
                }
                else
                {

                    if (Properties.Settings.Default.UseDataTable)
                    {
                        BulkControllerDataTable bulkController = new BulkControllerDataTable(bulkId);
                        bulkController.sb_startSending();
                    }
                    else
                    {
                        BulkControllerList bulkController = new BulkControllerList(bulkId);
                        bulkController.sb_startSending();
                    }
                    
                }

            }
            catch (Exception ex)
            {
                exceptionStr = "BulkExecuter:Program:Main,Exception has been occured." + ex.Message;
                logs.Error(exceptionStr, ex);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                Environment.Exit(1);
                return;
            }
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            if (Environment.ExitCode == 1)
            {
                //exit abnormally
                try
                {
                    SqlConnection cnn = new SqlConnection(SharedLibrary.SharedVariables.v_cnnStr);
                    SqlCommand cmd = new SqlCommand("Update portal.dbo.bulks set status =" + (int)SharedLibrary.MessageHandler.BulkStatus.Stopped + " where id = " + v_bulkId.ToString());
                    cmd.Connection = cnn;

                    cnn.Open();
                    cmd.ExecuteNonQuery();
                    cnn.Close();
                }
                catch (Exception ex)
                {
                    logs.Error("BulkExecuter:Program:OnProcessExit:", ex);
                }

            }
            //Console.WriteLine("I'm out of here");
        }
    }
}
