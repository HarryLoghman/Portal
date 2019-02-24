using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class AppEncryption
    {
        public string appName { get; set; }
        public int keySize { get; set; }
        public string IV { get; set; }
        public string keyVector { get; set; }
        public string ExtraParameter { get; set; }
    }
}
