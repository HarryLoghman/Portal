using SharedLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DehnadMTNChargeServices
{
    class ServiceChargeMusicYad : ServiceCharge
    {
        public override int prp_maxPrice
        {
            get
            {
                return 300;
            }
        }
        public ServiceChargeMusicYad( int serviceId, int tpsService, string aggregatorServiceId, int maxTries, int cycleNumber)
            : base(serviceId, tpsService, aggregatorServiceId, maxTries,cycleNumber)
        {

        }

      
    }
}
