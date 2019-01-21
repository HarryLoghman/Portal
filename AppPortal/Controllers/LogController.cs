﻿using System.Web.Http;
using SharedLibrary.Models;
using System.Web;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Threading.Tasks;
using SharedLibrary;
using System.Data.SqlClient;

namespace Portal.Controllers
{
    public class LogController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage ChargeErrorCodes(string serviceCode, string fromDate, string toDate, bool isShamsi)
        {
            HttpContext.Current.Server.ScriptTimeout = 2000;

            dynamic result = new ExpandoObject();
            dynamic data = null;
            dynamic json = null;
            result.Success = "false";
            result.Error = null;
            result.ServiceCode = serviceCode;
            result.Data = null;
            string from = fromDate;
            string to = toDate;
            if (isShamsi)
            {
                from = SharedLibrary.Date.GetGregorianDate(fromDate).ToString("yyyy-MM-dd");
                to = SharedLibrary.Date.GetGregorianDate(toDate).ToString("yyyy-MM-dd");
            }
            try
            {
                var serviceId = SharedLibrary.ServiceHandler.GetServiceId(serviceCode);
                if (serviceId == null)
                {
                    result.Success = "false";
                    result.Error = "service code not found";
                }
                else
                {
                    if (serviceCode == "Aseman")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Aseman"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "BehAmooz500")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("BehAmooz500"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Dambel")
                        using (var entity = new DambelLibrary.Models.DambelEntities())
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    //else if (serviceCode == "Darchin")
                        //using (var entity = new DarchinLibrary.Models.DarchinEntities())
                        //{
                        //    if (isShamsi)
                        //        data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                        //    else
                        //        data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                        //    data = JsonConvert.SerializeObject(data);
                        //}
                    else if (serviceCode == "DefendIran")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("DefendIran"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Dezhban")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Dezhban"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "DonyayeAsatir")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("DonyayeAsatir"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "FitShow")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("FitShow"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "JabehAbzar")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("JabehAbzar"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Medad")
                        using (var entity = new MedadLibrary.Models.MedadEntities())
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Medio")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Medio"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "MenchBaz")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("MenchBaz"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "MusicYad")
                        using (var entity = new MusicYadLibrary.Models.MusicYadEntities())
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Nebula")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Nebula"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Phantom")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Phantom"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "ShahreKalameh")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShahreKalameh"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "ShenoYad500")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShenoYad500"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "ShenoYad")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShenoYad"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Soltan")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Soltan"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Soraty")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Soraty"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "TahChin")
                        using (var entity = new TahChinLibrary.Models.TahChinEntities())
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Takavar")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Takavar"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Tamly500")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Tamly500"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    else if (serviceCode == "Tamly")
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Tamly"))
                        {
                            if (isShamsi)
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT Portal.dbo.ShamsiDate(CONVERT(date,DateCreated), 'Saal4-Maah2-Rooz2') as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            else
                                data = RawSql.DynamicSqlQuery(entity.Database, @"SELECT CONVERT(date,DateCreated) as DateCreated, Description as Description, COUNT(*) as Count FROM " + entity.Database.Connection.Database + @".[dbo].vw_Singlecharge WHERE CONVERT(date,DateCreated) >= @from AND CONVERT(date,DateCreated) <= @to AND Price > 0 GROUP BY CONVERT(date,DateCreated), Description ORDER BY CONVERT(date,DateCreated), COUNT(*)", new SqlParameter("@from", from), new SqlParameter("@to", to));
                            data = JsonConvert.SerializeObject(data);
                        }
                    result.Success = "true";
                    result.Error = null;
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in ChargeErrorCodes:" + e);
                result.Success = "false";
                result.Error = "contact system administrator";
            }
            result.Data = JsonConvert.DeserializeObject(data);
            json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
