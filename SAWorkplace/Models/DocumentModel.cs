using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Models
{
    public class DocumentModel
    {
        [Key]
        public int ID { get; set; }
        public int TicketNumber { get; set; }
        public string DocumentName { get; set; }
        public string DocumentExt { get; set; }
        public string DocumentType { get; set; }
        public string DocumentPath { get; set; }
        public DateTime? DocumentModifiedDate { get; set; }
        public string DocumentModifiedByName { get; set; }
    }

    public class DocumentDisplayModel
    {
        [Key]
        public int ID { get; set; }
        public int TicketNumber { get; set; }
        public int RequestType { get; set; }
        [NotMapped]
        public IEnumerable<DocumentModel> Documents { get; set; }
    }
}
