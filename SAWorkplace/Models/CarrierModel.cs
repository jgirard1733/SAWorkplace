using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Models
{
    public class CarrierModel
    {
        [Key]
        public int ID { get; set; }
        public int CarrierId { get; set; }
        public string CarrierName { get; set; }
        public string CarrierLogo { get; set; }
        public int Active { get; set; }
    }
}
