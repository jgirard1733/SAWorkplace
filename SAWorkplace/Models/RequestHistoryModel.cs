using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Models
{
    public class RequestHistoryModel
    {
        [Key]
        public int ID { get; set; }
        public int TicketNumber { get; set; }
        public string History { get; set; }
        public DateTime? AddDateTime { get; set; }
        public string AddedBy { get; set; }
    }
}
