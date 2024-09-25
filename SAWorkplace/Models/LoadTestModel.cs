using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Models
{
    public class LoadTestModel
    {
        [Key]
        public int LoadTestID { get; set; }
        public int TicketNumber { get; set; }
        public DateTime? DateTested { get; set; }
        public string InterfaceName { get; set; }
        public double Burst_Max { get; set; }
        public double Burst_Min { get; set; }
        public double Burst_Avg { get; set; }
        public double Variance_Max { get; set; }
        public double Variance_Min { get; set; }
        public double Variance_Avg { get; set; }
        public string TestResult { get; set; }
    }

    public class LoadTestDisplayModel
    {
        [Key]
        public int LoadTestID { get; set; }
        public int TicketNumber { get; set; }
        public int CarrierID { get; set; }
        public IEnumerable<LoadTestModel> LoadTests { get; set; }
    }
}
