using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DambelLibrary.Models;
using SharedLibrary;
using SharedLibrary.Models;

namespace DehnadMTNChargeServices
{
    class ServiceChargeDambel : ServiceCharge
    {
        public override int prp_maxPrice
        {
            get
            {
                return 300;
            }
        }

        public ServiceChargeDambel( int serviceId, int tpsService, string aggregatorServiceId, int maxTries, int cycleNumber)
            : base(serviceId, tpsService, aggregatorServiceId, maxTries, cycleNumber)
        {

        }
       
    }
}
