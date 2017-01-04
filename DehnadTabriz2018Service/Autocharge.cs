using System;
using SharedLibrary.Models;
using Tabriz2018Library.Models;
using System.Linq;
using System.Collections.Generic;

namespace DehnadTabriz2018Service
{
    class Autocharge
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Job()
        {
            try
            {
                var time = DateTime.Now.TimeOfDay;
                var insertTime = TimeSpan.Parse(Properties.Settings.Default.InsertAutochargeMessageInQueueTime);
                var insertEndTime = TimeSpan.Parse(Properties.Settings.Default.InsertAutochargeMessageInQueueEndTime);
                if (time > insertTime && time < insertEndTime)
                    InsertAutochargeMessagesToQueue();

                SendAutochargeOnTime();
            }
            catch (Exception e)
            {
                logs.Error("Error in Autocharge Job:" + e);
            }
        }

        private void SendAutochargeOnTime()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var currentPersianDate = SharedLibrary.Date.GetPersianDate(DateTime.Now).Replace("/", "-");
            var autochargeTimeTable = GetAutochargeTimeTable();
            foreach (var item in autochargeTimeTable)
            {
                var sendTime = TimeSpan.Parse(item.SendTime);
                var SendEndTime = sendTime + TimeSpan.Parse("00:00:05");
                if (currentTime > sendTime && currentTime < SendEndTime)
                {
                    using (var entity = new Tabriz2018Entities())
                    {
                        entity.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.AutoCharge, null, item.Tag, currentPersianDate, (int)SharedLibrary.MessageHandler.ProcessStatus.InQueue, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, null);
                    }
                }
            }
        }

        public void InsertAutochargeMessagesToQueue()
        {
            try
            {
                
            }
            catch (Exception e)
            {
                logs.Info(" Exception in Autocharge thread occurred ");
                logs.Error(" Exception in Autocharge thread occurred: ", e);
            }
        }

        private static List<AutochargeTimeTable> GetAutochargeTimeTable()
        {
            var autochargeTimeTable = new List<AutochargeTimeTable>();
            try
            {
                using (var entity = new Tabriz2018Entities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    autochargeTimeTable = entity.AutochargeTimeTables.ToList();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in GetAutochargeTimeTable:" + e);
            }
            return autochargeTimeTable;
        }
    }
}