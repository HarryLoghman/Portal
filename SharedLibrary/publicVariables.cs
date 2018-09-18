﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class publicVariables
    {
        public static System.Data.SqlClient.SqlConnection GetConnectionPortal()
        {
            System.Data.SqlClient.SqlConnection cnn = new System.Data.SqlClient.SqlConnection();
            System.Data.Entity.Core.EntityClient.EntityConnectionStringBuilder builder = new System.Data.Entity.Core.EntityClient.EntityConnectionStringBuilder();
            builder.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["PortalEntities"].ConnectionString;
            cnn.ConnectionString = builder.ProviderConnectionString;
            return cnn;
        }
    }
}
