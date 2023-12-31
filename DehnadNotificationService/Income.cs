﻿using System;
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
                //IncomeDiffrenceNotify("TahChin", typeof(TahChinLibrary.Models.TahChinEntities));
                //IncomeDiffrenceNotify("MusicYad", typeof(MusicYadLibrary.Models.MusicYadEntities));
                //IncomeDiffrenceNotify("Dambel", typeof(DambelLibrary.Models.DambelEntities));
                //IncomeDiffrenceNotify("Phantom", typeof(PhantomLibrary.Models.PhantomEntities));
                //IncomeDiffrenceNotify("Medio", typeof(MedioLibrary.Models.MedioEntities));
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
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("TahChin"))
                    OverChargeNotify("TahChin", entity, 300);
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("MusicYad"))
                    OverChargeNotify("MusicYad", entity, 300);
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Dambel"))
                    OverChargeNotify("Dambel", entity, 300);
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Phantom"))
                    OverChargeNotify("Phantom", entity, 500);
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Medio"))
                    OverChargeNotify("Medio", entity, 400);
                //using (var entity = new DarchinLibrary.Models.DarchinEntities())
                //    OverChargeNotify("Darchin", entity, 7000);
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Medad"))
                    OverChargeNotify("Medad", entity, 300);
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("PorShetab"))
                    OverChargeNotify("PorShetab", entity, 500);
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
                if (serviceCode.ToLower() == "tahchin")
                {
                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("TahChin"))
                        result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(entity, DateTime.Now);
                }
                else if (serviceCode.ToLower() == "musicyad")
                {
                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("MusicYad"))
                        result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(entity, DateTime.Now);
                }
                else if (serviceCode.ToLower() == "dambel")
                {
                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Dambel"))
                        result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(entity, DateTime.Now);
                }
                else if (serviceCode.ToLower() == "phantom")
                {
                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Phantom"))
                        result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(entity, DateTime.Now);
                }
                else if (serviceCode.ToLower() == "medio")
                {
                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Medio"))
                        result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(entity, DateTime.Now);
                }
                else if (serviceCode.ToLower() == "darchin")
                {
                    //using (var entity = new DarchinLibrary.Models.DarchinEntities())
                    //    result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(entity, DateTime.Now);
                }
                else if (serviceCode.ToLower() == "medad")
                {
                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Medad"))
                        result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(entity, DateTime.Now);
                }
                else if (serviceCode.ToLower() == "porshetab")
                {
                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("PorShetab"))
                        result = SharedLibrary.Notify.GetSuccessfulIncomeByDate(entity, DateTime.Now);
                }
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

        public static void OverChargeNotify(string serviceCode, dynamic entity, int maxChargeLimit)
        {
            try
            {
                var message = "";
                var isOvercharge = SharedLibrary.Notify.OverChargeCheck(entity, maxChargeLimit);
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
