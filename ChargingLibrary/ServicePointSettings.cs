
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net;

namespace ChargingLibrary
{
    class ServicePointSettings : ConfigurationSection
    {

        private static ServicePointSettings settings = ConfigurationManager.GetSection("ServicePointSettings") as ServicePointSettings;

        public void Assign()
        {
            Uri uri = new Uri(settings.Uri);
            ServicePoint sp = ServicePointManager.FindServicePoint(uri);
            sp.ConnectionLimit = settings.ConnectionLimit;
            sp.Expect100Continue = settings.Expect100Continue;
            sp.UseNagleAlgorithm = settings.UseNagleAlgorithm;

        }

        public ServicePoint GetServicePoint()
        {
            try
            {
                Uri uri = new Uri(settings.Uri);
                ServicePoint sp = ServicePointManager.FindServicePoint(uri);
                return sp;
            }
            catch (Exception ex)
            {
                Program.logs.Error("Error in fnc_getServicePoint", ex);
                Program.sb_sendNotification(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Error in fnc_getServicePoint:" + ex.Message);
            }
            return null;
        }

        public static ServicePointSettings Settings
        {
            get
            {
                return settings;
            }
        }

        [ConfigurationProperty("Uri")]
        public string Uri
        {
            get
            {
                object Uri = this["Uri"];
                if (Uri == null || Uri == DBNull.Value || string.IsNullOrEmpty(Uri.ToString()))
                    return "http://localhost/";
                else return Uri.ToString();
            }


            set { this["Uri"] = value; }
        }

        [ConfigurationProperty("ConnectionLimit")]
        public int ConnectionLimit
        {
            get
            {
                object temp = this["ConnectionLimit"];
                int value;
                if (temp == null || temp == DBNull.Value || string.IsNullOrEmpty(temp.ToString()) || !int.TryParse(temp.ToString(), out value))
                    return 4000;
                else return value;
            }


            set { this["ConnectionLimit"] = value; }
        }

        [ConfigurationProperty("Expect100Continue")]
        public bool Expect100Continue
        {
            get
            {
                object temp = this["Expect100Continue"];
                bool value;
                if (temp == null || temp == DBNull.Value || string.IsNullOrEmpty(temp.ToString()) || !bool.TryParse(temp.ToString(), out value))
                    return false;
                else return value;
            }


            set { this["Expect100Continue"] = value; }
        }

        [ConfigurationProperty("UseNagleAlgorithm")]
        public bool UseNagleAlgorithm
        {
            get
            {
                object temp = this["UseNagleAlgorithm"];
                bool value;
                if (temp == null || temp == DBNull.Value || string.IsNullOrEmpty(temp.ToString()) || !bool.TryParse(temp.ToString(), out value))
                    return false;
                else return value;
            }


            set { this["UseNagleAlgorithm"] = value; }
        }
    }
}
