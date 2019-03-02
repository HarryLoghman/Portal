using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadFtpSyncAndChargingService.MCI
{
    class FtpFile
    {
        public FtpFile(string ftpFileName , string windowsFilePath, string ftpFilePath, string identifier)
        {
            this.fileName = ftpFileName;
            this.winFilePath = windowsFilePath;
            this.ftpFilePath = ftpFilePath;
            this.identifier = identifier;
        }

        public string fileName { get; set; }
        public string winFilePath { get; set; }
        public string ftpFilePath { get; set; }

        public string identifier { get; set; }
    }
}
