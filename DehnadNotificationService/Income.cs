using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadNotificationService
{
    public class Income
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int defaultIncomePercentageDecrease = 10;

        public static void IncomeDiffrenceByHour()
        {
            try
            {
                IncomeDiffrenceNotify("TahChin", typeof(TahChinLibrary.Models.TahChinEntities));
                IncomeDiffrenceNotify("MusicYad", typeof(MusicYadLibrary.Models.MusicYadEntities));
                IncomeDiffrenceNotify("Dambel", typeof(DambelLibrary.Models.DambelEntities));
                IncomeDiffrenceNotify("Phantom", typeof(PhantomLibrary.Models.PhantomEntities));
                IncomeDiffrenceNotify("Medio", typeof(MedioLibrary.Models.MedioEntities));
            }
            catch (Exception e)
            {
                logs.Error("Exception in IncomeDiffrenceByHour: ", e);
                DehnadNotificationService.Service.SaveMessageToSendQueue("Exception in IncomeDiffrenceByHour", UserType.AdminOnly);
            }
        }

        public static void OverChargeChecker()
        {
            try
            {
                OverChargeNotify("TahChin", typeof(TahChinLibrary.Models.TahChinEntities), 300);
                OverChargeNotify("MusicYad", typeof(MusicYadLibrary.Models.MusicYadEntities), 300);
                OverChargeNotify("Dambel", typeof(DambelLibrary.Models.DambelEntities), 300);
                OverChargeNotify("Phantom", typeof(PhantomLibrary.Models.PhantomEntities), 500);
                OverChargeNotify("Medio", typeof(MedioLibrary.Models.MedioEntities), 400);
                OverChargeNotify("Darchin", typeof(DarchinLibrary.Models.DarchinEntities), 7000);
            }
            catch (Exception e)
            {
                logs.Error("Exception in OverChargeChecker: " + e);
                DehnadNotificationService.Service.SaveMessageToSendQueue("Exception in OverChargeChecker", UserType.AdminOnly);
            }

        }

        public static int GetServiceIncome(string serviceCode)
        {
            var result = -1;
            try
            {
                if(serviceCode.ToLower() == "tahchin")
                result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(typeof(TahChinLibrary.Models.TahChinEntities), DateTime.Now);
                else if (serviceCode.ToLower() == "musicyad")
                    result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(typeof(MusicYadLibrary.Models.MusicYadEntities), DateTime.Now);
                else if (serviceCode.ToLower() == "dambel")
                    result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(typeof(DambelLibrary.Models.DambelEntities), DateTime.Now);
                else if (serviceCode.ToLower() == "phantom")
                    result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(typeof(PhantomLibrary.Models.PhantomEntities), DateTime.Now);
                else if (serviceCode.ToLower() == "medio")
                    result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(typeof(MedioLibrary.Models.MedioEntities), DateTime.Now);
                else if (serviceCode.ToLower() == "darchin")
                    result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(typeof(DarchinLibrary.Models.DarchinEntities), DateTime.Now);
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetServiceIncome: ", e);
                DehnadNotificationService.Service.SaveMessageToSendQueue("Exception in GetServiceIncome", UserType.AdminOnly);
            }
            return result;
        }

        public static void IncomeDiffrenceNotify(string serviceCode, Type entityTpe)
        {
            try
            {
                int? incomeDiffrence = null;
                var message = "";
                incomeDiffrence = SharedLibrary.Notify.GetIncomeDifferenceByHourPercent(entityTpe);
                if (incomeDiffrence == null)
                {
                    message = "<b>Exception in IncomeDiffrenceNotify for " + serviceCode + " service</b>";
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                }
                else
                {
                    if (incomeDiffrence <= -10 || incomeDiffrence >= 20)
                    {
                        if (incomeDiffrence < 0)
                            message = serviceCode + " income dropped by %" + incomeDiffrence;
                        else
                            message = serviceCode + " income increased by %" + incomeDiffrence;
                        DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in IncomeDiffrenceNotify: ", e);
                var message = "<b>Exception in IncomeDiffrenceNotify</b>";
                DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
            }
        }

        public static void OverChargeNotify(string serviceCode, Type entityTpe, int maxChargeLimit)
        {
            try
            {
                var message = "";
                var isOvercharge = SharedLibrary.Notify.OverChargeCheck(entityTpe, maxChargeLimit);
                if (isOvercharge == null)
                {
                    message = "<b>Exception in OverChargeNotify for " + serviceCode + " service</b>";
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                }
                else
                {
                    if (isOvercharge == true)
                    {
                        message = "<b>Overcharge in " + serviceCode + " Service</b>";
                        DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in OverChargeNotify: ", e);
                var message = "<b>Exception in OverChargeNotify</b>";
                DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
            }
        }
    }
}
