﻿using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadDeliveryProcessorService
{
    public class DeliveryProcessor
    {
        public void Process()
        {
            try
            {
                var deliveryMessages = new List<Delivery>();
                Delivery deliveryMessage;
                long timeLng;
                string dbName;
                string shortCode;
                string mobileNumber;
                string referenceId;
                string cmd;
                using (var portal = new PortalEntities())
                {
                    var serviceInfo = portal.ServiceInfoes.ToList();

                    deliveryMessages = portal.Deliveries.Where(o => o.IsProcessed == false && o.ShortCode != null && o.MobileNumber != null).ToList();
                    for (int i = 0; i < deliveryMessages.Count; i++)
                    {
                        deliveryMessage = deliveryMessages[i];

                        if (deliveryMessage.Delivered.HasValue && !string.IsNullOrEmpty(deliveryMessage.MobileNumber))
                        {
                            if (!string.IsNullOrEmpty(deliveryMessage.Correlator)
                            && deliveryMessage.Correlator.Contains("s")
                            && long.TryParse(deliveryMessage.Correlator.Split('s')[1], out timeLng))
                            {
                                mobileNumber = deliveryMessage.MobileNumber;
                                SharedLibrary.MessageSender.sb_processCorrelator(deliveryMessage.Correlator, ref mobileNumber, out shortCode);

                                if (!mobileNumber.StartsWith("0")) mobileNumber = mobileNumber + "0";
                                dbName = serviceInfo.Where(o => o.ShortCode == shortCode).Select(o => o.databaseName).FirstOrDefault();
                                if (!string.IsNullOrEmpty(dbName))
                                {
                                    DateTime dateAddedToQueue = new DateTime(timeLng);
                                    cmd = "exec sp_messageDeliveryUpdate '" + dbName + "'"
                                        + "," + (deliveryMessages[i].Delivered.Value ? "1" : "0")
                                        + "," + (string.IsNullOrEmpty(deliveryMessages[i].Status) ? "Null" : "'" + deliveryMessages[i].Status + "'")
                                        + "," + "'" + dateAddedToQueue.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                                        + ",Null"
                                        + "," + "'" + mobileNumber + "'";
                                    Program.logs.Info(cmd);
                                    portal.Database.ExecuteSqlCommand(cmd);
                                    
                                    deliveryMessage.IsProcessed = true;
                                    portal.Entry(deliveryMessage).State = System.Data.Entity.EntityState.Modified;
                                    if (i % 500 == 0) portal.SaveChanges();
                                }
                            }
                            else if (string.IsNullOrEmpty(deliveryMessage.Correlator)
                             && !string.IsNullOrEmpty(deliveryMessage.ReferenceId))
                            {
                                //for avvalpod500
                                mobileNumber = deliveryMessage.MobileNumber;
                                referenceId = deliveryMessage.ReferenceId;
                                shortCode = deliveryMessage.ShortCode;
                                if (!mobileNumber.StartsWith("0")) mobileNumber = mobileNumber + "0";
                                dbName = serviceInfo.Where(o => o.ShortCode == shortCode).Select(o => o.databaseName).FirstOrDefault();
                                if (!string.IsNullOrEmpty(dbName))
                                {
                                    cmd = "exec sp_messageDeliveryUpdate '" + dbName + "'"
                                        + "," + (deliveryMessages[i].Delivered.Value ? "1" : "0")
                                        + "," + (string.IsNullOrEmpty(deliveryMessages[i].Status) ? "Null" : "'" + deliveryMessages[i].Status + "'")
                                        + "," + "Null"
                                        + ",'" + referenceId + "'"
                                        + "," + "'" + mobileNumber + "'";
                                    Program.logs.Info(cmd);
                                    portal.Database.ExecuteSqlCommand(cmd);
                                    
                                    deliveryMessage.IsProcessed = true;
                                    portal.Entry(deliveryMessage).State = System.Data.Entity.EntityState.Modified;
                                    if (i % 500 == 0) portal.SaveChanges();
                                }
                            }
                        }
                        portal.SaveChanges();

                    }
                }

            }
            catch (Exception e)
            {
                Program.logs.Error("Exeption in DeliveryProcessor: " + e);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Error in DeliveryProcessor:" + e.Message);
            }
        }
    }
}
