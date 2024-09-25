using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Models
{
    public class FeasibilityReviewModel
    {
        [Key]
        public int ID { get; set; }
        public int TicketNumber { get; set; }
        public string ProjectName { get; set; }
        public string TeamMembers { get; set; }

        public string Feas_ReviewedBy { get; set; }
        public string Feas_ReviewDate { get; set; }
        public string Feas_RequirePOC { get; set; }
        public string Feas_RequirePOCDetail { get; set; }
        public string Feas_IPProduct { get; set; }
        public string Feas_IPProductDetail { get; set; }
        public string Feas_Gaps { get; set; }
        public string Feas_GapsDetail { get; set; }
        public string Feas_OutsideService { get; set; }
        public string Feas_OutsideServiceDetail { get; set; }
        public string Feas_BestUX { get; set; }
        public string Feas_BestUXDetail { get; set; }
        public string Feas_StandardESig { get; set; }
        public string Feas_StandardESigDetail { get; set; }
        public string Feas_AdditionalCosts { get; set; }
        public string Feas_ItemsNeeded { get; set; }
        public string Feas_JIRAs { get; set; }
        public string Feas_Notes { get; set; }

        //Initiation Review
        public string Init_ReviewedBy { get; set; }
        public string Init_ReviewDate { get; set; }
        public string Init_Workflow { get; set; }
        public string Init_WorkflowDetail { get; set; }
        public string Init_POCResults { get; set; }
        public string Init_POCResultsDetail { get; set; }
        public string Init_IPProduct { get; set; }
        public string Init_IPProductDetail { get; set; }
        public string Init_Gaps { get; set; }
        public string Init_GapsDetail { get; set; }
        public string Init_IntegrationReq { get; set; }
        public string Init_IntegrationReqDetail { get; set; }
        public string Init_UX { get; set; }
        public string Init_UXDetail { get; set; }
        public string Init_AdditionalCosts { get; set; }
        public string Init_ItemsNeeded { get; set; }
        public string Init_JIRAs { get; set; }
        public string Init_Notes { get; set; }

        //Post Initial Build Review
        public string InitBuild_ReviewedBy { get; set; }
        public string InitBuild_ReviewDate { get; set; }
        public string InitBuild_Topology { get; set; }
        public string InitBuild_TopologyDetail { get; set; }
        public string InitBuild_Compliance { get; set; }
        public string InitBuild_ComplianceDetail { get; set; }
        public string InitBuild_Gaps { get; set; }
        public string InitBuild_GapsDetail { get; set; }
        public string InitBuild_IntegrationReq { get; set; }
        public string InitBuild_IntegrationReqDetail { get; set; }
        public string InitBuild_UX { get; set; }
        public string InitBuild_UXDetail { get; set; }
        public string InitBuild_AdditionalCosts { get; set; }
        public string InitBuild_ItemsNeeded { get; set; }
        public string InitBuild_JIRAs { get; set; }
        public string InitBuild_Notes { get; set; }

        //Implementation Review
        public string Impl_ReviewedBy { get; set; }
        public string Impl_ReviewDate { get; set; }
        public string Impl_Topology { get; set; }
        public string Impl_TopologyDetail { get; set; }
        public string Impl_Compliance { get; set; }
        public string Impl_ComplianceDetail { get; set; }
        public string Impl_Gaps { get; set; }
        public string Impl_GapsDetail { get; set; }
        public string Impl_IntegrationReq { get; set; }
        public string Impl_IntegrationReqDetail { get; set; }
        public string Impl_UX { get; set; }
        public string Impl_UXDetail { get; set; }
        public string Impl_AdditionalCosts { get; set; }
        public string Impl_ItemsNeeded { get; set; }
        public string Impl_JIRAs { get; set; }
        public string Impl_Notes { get; set; }

    }
}
