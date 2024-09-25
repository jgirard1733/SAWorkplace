using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Models
{
    public class SMERequestModel
    {
        [Key]
        public int RequestId { get; set; }
        public int RequestType { get; set; }
        public string Requestor { get; set; }
        public string RequestorName { get; set; }
        public string RequestorEmail { get; set; }
        public int CarrierId { get; set; }
        public string CarrierName { get; set; }
        public string RequestName { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime RequestDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime? RequestReviewDate { get; set; }

        public string ProjectorCode { get; set; }
        public string RequestDesc { get; set; }
        public string AssignedSME { get; set; }
        public string AssignedSMEName { get; set; }
        public string AssignedSMEEmail { get; set; }
        public int ProgressType { get; set; }
        public int RequestStatus { get; set; }
        public int TicketNumber { get; set; }
        public string Duration1 { get; set; }
        public string Duration2 { get; set; }
        public string DurationType1 { get; set; }
        public string DurationType2 { get; set; }
    }

    public class SMERequestDisplayModel
    {
        [Key]
        public int ID { get; set; }
        [NotMapped]
        public IEnumerable<SMERequestModel> Requests { get; set; }
        [NotMapped]
        public IEnumerable<CarrierModel> Carriers { get; set; }
        [NotMapped]
        public IEnumerable<RequestTypeModel> RequestTypes { get; set; }
        [NotMapped]
        public IEnumerable<DurationTypesModel> DurationTypes { get; set; }
    }

    public class SMERequestEditModel
    {
        [Key]
        public int ID { get; set; }
        [NotMapped]
        public SMERequestModel Requests { get; set; }
        [NotMapped]
        public IEnumerable<CarrierModel> Carriers { get; set; }
        [NotMapped]
        public IEnumerable<RequestTypeModel> RequestTypes { get; set; }
        [NotMapped]
        public IEnumerable<DurationTypesModel> DurationTypes { get; set; }
    }
}
