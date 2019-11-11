﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Models
{
    public class RequestModel
    {
        [Key]
        public int RequestId { get; set; }
        public int RequestType { get; set; }
        public string Requestor { get; set; }
        public string RequestorName { get; set; }
        public string RequestorEmail { get; set; }
        public int CarrierId { get; set; }
        public string ProjectName { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime RequestDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime? FeasibilityReviewDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime? InitiationReviewDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime? ImplementationReviewDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime? RequestReviewDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime? IssueReviewDate { get; set; }
        public string ProjectorCode { get; set; }
        public string RequestDesc { get; set; }
        public string AssignedSA { get; set; }
        public string AssignedSAName { get; set; }
        public string AssignedSAEmail { get; set; }
        public int RequestStatus { get; set; }
        public string Requirements { get; set; }
        public string SAInitialReview { get; set; }
        public string SAFinalReview { get; set; }
        public string TFSPath { get; set; }
        public DateTime? TestTime { get; set; }
        public int AlertType { get; set; }
        public int TicketNumber { get; set; }
        public string WebServiceURLs { get; set; }
    }

    public class RequestDisplayModel
    {
        [Key]
        public int ID { get; set; }
        [NotMapped]
        public IEnumerable<RequestModel> Requests { get; set; }
        [NotMapped]
        public IEnumerable<CarrierModel> Carriers { get; set; }
        [NotMapped]
        public IEnumerable<StatusModel> Status { get; set; }
        [NotMapped]
        public IEnumerable<AlertModel> Alerts { get; set; }
        [NotMapped]
        public IEnumerable<RequestTypeModel> RequestTypes { get; set; }
    }

    public class RequestEditModel
    {
        [Key]
        public int ID { get; set; }
        [NotMapped]
        public RequestModel Requests { get; set; }
        [NotMapped]
        public IEnumerable<CarrierModel> Carriers { get; set; }
        [NotMapped]
        public IEnumerable<StatusModel> Status { get; set; }
        [NotMapped]
        public IEnumerable<AlertModel> Alerts { get; set; }
        [NotMapped]
        public IEnumerable<RequestTypeModel> RequestTypes { get; set; }
        [NotMapped]
        public IEnumerable<DocumentModel> Documents { get; set; }
        [NotMapped]
        public IEnumerable<RequestHistoryModel> History { get; set; }
    }
}
