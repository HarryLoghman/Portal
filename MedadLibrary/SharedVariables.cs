using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedadLibrary
{
    public class SharedVariables
    {
        public static string v_serivceCode = "Medad";
        static SharedLibrary.Models.vw_servicesServicesInfo v_serviceEntity;
        public static SharedLibrary.Models.vw_servicesServicesInfo prp_serviceEntity
        {
            get
            {
                if (v_serviceEntity == null)
                {
                    using (var portal = new SharedLibrary.Models.PortalEntities())
                    {
                        var service = portal.vw_servicesServicesInfo.Where(o => o.ServiceCode == v_serivceCode).FirstOrDefault();
                        if (service == null)
                        {
                            throw new Exception("There is no service specified with service code = " + v_serivceCode);
                        }
                        else v_serviceEntity = service;
                    }

                }
                return v_serviceEntity;
            }
        }

        public static string connectionStringInAppConfig
        {
            get
            {
                return "Shared" + prp_serviceEntity.databaseName + "Entities";
            }
        }
    }
}
