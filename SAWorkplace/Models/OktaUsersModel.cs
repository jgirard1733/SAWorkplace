using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Models
{
    public class OktaUsersModel
    {
        [Key]
        public int ID { get; set; }
        public string OktaResults { get; set; }
    }
}
