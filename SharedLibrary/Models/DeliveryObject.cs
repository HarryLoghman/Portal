using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SharedLibrary.Models
{
    public class DeliveryObject
    {
        public string PardisID { get; set; }
        public string ReferenceId { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string ErrorMessage { get; set; }
        public long AggregatorId { get; set; }
        public bool IsProcessed { get; set; }
    }
}