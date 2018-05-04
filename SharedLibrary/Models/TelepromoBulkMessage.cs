using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class TelepromoBulkMessage
    {
        public string to { get; set; }
        public string message { get; set; }
        public string messageId { get; set; }
    }
}
