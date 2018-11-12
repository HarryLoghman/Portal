using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadMCISingleChargeFTPService
{
    class FtpFile
    {
        public FtpFile(string windowsFilePath, string ftpFilePath, string identifier)
        {
            this.winFilePath = windowsFilePath;
            this.ftpFilePath = ftpFilePath;
            this.identifier = identifier;
        }

        public string winFilePath { get; set; }
        public string ftpFilePath { get; set; }

        public string identifier { get; set; }
    }
}
