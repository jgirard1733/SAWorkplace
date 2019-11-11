using Microsoft.EntityFrameworkCore;
using SAWorkplace.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Data
{
    public class ApplicationDBContext:DbContext
    {

        #region Public Properties
        public DbSet<RequestModel> tblReviewRequests { get; set; }
        public DbSet<CarrierModel> tblCarriers { get; set; }
        public DbSet<DocumentModel> tblDocuments { get; set; }
        public DbSet<RequestHistoryModel> tblRequestHistory { get; set; }
        public List<StatusModel> Statuses { get; set; }
        public List<AlertModel> Alerts { get; set; }
        public List<RequestTypeModel> RequestTypes { get; set; }
        #endregion

        #region Constructor
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {

        }
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source=C:\SQLite\SAWorkplace.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }

    public class DataAccess
    {
        protected ApplicationDBContext aContext;

        public DataAccess(ApplicationDBContext context)
        {
            aContext = context;
        }

        public List<RequestModel> LoadRequests(string userName, int ticketNum, string PSArchitect)
        {
            var Requests = aContext.tblReviewRequests;

            if (PSArchitect == "True")
            {
                if (ticketNum > 0)
                {
                    var orderedRequests = Requests.Where(a => a.TicketNumber.Equals(ticketNum)).OrderBy(a => a.RequestDate);
                    return orderedRequests.ToList();
                }
                else if (userName == "All")
                {
                    var orderedRequests = Requests.OrderBy(a => a.RequestDate);
                    return orderedRequests.ToList();
                }
                else if (userName == "Unassigned")
                {
                    var orderedRequests = Requests.Where(a => a.AssignedSA == "" || a.AssignedSA == null).OrderBy(a => a.RequestDate);
                    return orderedRequests.ToList();
                }
                else
                {
                    var orderedRequests = Requests.Where(a => a.Requestor == userName || a.AssignedSA == userName).OrderBy(a => a.RequestDate);
                    return orderedRequests.ToList();
                }
            }
            else
            {
                var orderedRequests = Requests.Where(a=> a.Requestor == userName).OrderBy(a => a.RequestDate);
                return orderedRequests.ToList();
            }
        }

        public RequestModel LoadRequest(int ticketNum)
        {
            System.DateTime today = System.DateTime.Now;

            RequestModel model = new RequestModel();
            if (ticketNum > 0)
            {
                var requestList = aContext.tblReviewRequests.ToList().Where(t => t.TicketNumber == ticketNum).FirstOrDefault();
                model.RequestId = requestList.RequestId;
                model.ProjectName = requestList.ProjectName;
                model.AlertType = requestList.AlertType;
                model.AssignedSA = requestList.AssignedSA;
                model.AssignedSAEmail = requestList.AssignedSAEmail;
                model.AssignedSAName = requestList.AssignedSAName;
                model.CarrierId = requestList.CarrierId;
                model.RequestReviewDate = requestList.RequestReviewDate;
                model.FeasibilityReviewDate = requestList.FeasibilityReviewDate;
                model.InitiationReviewDate = requestList.InitiationReviewDate;
                model.ImplementationReviewDate = requestList.ImplementationReviewDate;
                model.IssueReviewDate = requestList.IssueReviewDate;
                model.WebServiceURLs = requestList.WebServiceURLs;
                model.ProjectorCode = requestList.ProjectorCode;
                model.RequestDate = requestList.RequestDate;
                model.RequestDesc = requestList.RequestDesc;
                model.RequestorEmail = requestList.RequestorEmail;
                model.RequestorName = requestList.RequestorName;
                model.RequestStatus = requestList.RequestStatus;
                model.RequestType = requestList.RequestType;
                model.Requirements = requestList.Requirements;
                model.SAFinalReview = requestList.SAFinalReview;
                model.SAInitialReview = requestList.SAInitialReview;
                model.TestTime = requestList.TestTime;
                model.TFSPath = requestList.TFSPath;
                model.TicketNumber = requestList.TicketNumber;
            }
            else
            {
                model.ProjectName = "";
                model.AlertType = 1;
                model.AssignedSA = "";
                model.AssignedSAEmail = "";
                model.AssignedSAName = "";
                model.CarrierId = 0;
                model.RequestReviewDate = null;
                model.FeasibilityReviewDate = null;
                model.ImplementationReviewDate = null;
                model.InitiationReviewDate = null;
                model.IssueReviewDate = null;
                model.WebServiceURLs = "";
                model.ProjectorCode = "";
                model.RequestDesc = "";
                model.RequestorEmail = "";
                model.RequestorName = "";
                model.RequestStatus = 1;
                model.RequestType = 0;
                model.Requirements = "";
                model.SAFinalReview = "";
                model.SAInitialReview = "";
                model.TestTime = null;
                model.TFSPath = "";
            }
            return model;
        }

        public async Task<List<CarrierModel>> LoadActiveCarriers()
        {
            if (aContext.tblCarriers.Local.Count() != aContext.tblCarriers.Count())
            {
                var Carriers = aContext.tblCarriers.Where(t => t.Active == 1);
                var orderedCarriers = Carriers.OrderBy(a => a.CarrierName);
                return orderedCarriers.ToList();
            }
            else
            {
                var Carriers = aContext.tblCarriers.Local.Where(t => t.Active == 1);
                var orderedCarriers = Carriers.OrderBy(a => a.CarrierName);
                return orderedCarriers.ToList();
            }
        }

        public async Task<List<StatusModel>> GetStatuses()
        {
            var statusModel = new List<StatusModel>();
            statusModel.Add(new StatusModel { ID = 1, Text = "New", Class = "alert-custom-new" });
            statusModel.Add(new StatusModel { ID = 2, Text = "Assigned", Class = "alert-custom-open" });
            statusModel.Add(new StatusModel { ID = 3, Text = "Complete", Class = "alert-custom-complete" });
            statusModel.Add(new StatusModel { ID = 4, Text = "Closed", Class = "alert-custom-closed" });
            statusModel.Add(new StatusModel { ID = 5, Text = "Feasibility Review", Class = "alert-custom-feasibility" });
            statusModel.Add(new StatusModel { ID = 6, Text = "Initiation Review", Class = "alert-custom-initiation" });
            statusModel.Add(new StatusModel { ID = 7, Text = "Implementation Review", Class = "alert-custom-implementation" });
            statusModel.Add(new StatusModel { ID = 8, Text = "New - Immediate", Class = "alert-custom-closed" });

            return statusModel;
        }

        public async Task<List<AlertModel>> GetAlerts()
        {
            var alertModel = new List<AlertModel>();
            //alertModel.Add(new AlertModel { AlertId = 1, AlertClass = "<figure class='ball ball-default'></figure>" });
            //alertModel.Add(new AlertModel { AlertId = 2, AlertClass = "<figure class='ball ball-success'></figure>" });
            //alertModel.Add(new AlertModel { AlertId = 3, AlertClass = "<figure class='ball ball-danger'></figure>" });
            alertModel.Add(new AlertModel { AlertId = 1, AlertClass = "<progress max='100' value='0'>Started</progress>" });
            alertModel.Add(new AlertModel { AlertId = 2, AlertClass = "<progress max='100' value='25'>25%</progress>" });
            alertModel.Add(new AlertModel { AlertId = 3, AlertClass = "<progress max='100' value='50'>50%</progress>" });
            alertModel.Add(new AlertModel { AlertId = 4, AlertClass = "<progress max='100' value='75'>75%</progress>" });
            alertModel.Add(new AlertModel { AlertId = 5, AlertClass = "<progress max='100' value='100'>Complete!</progress>" });

            return alertModel;
        }

        public async Task<List<RequestTypeModel>> GetRequestTypes()
        {
            var requesTypeModel = new List<RequestTypeModel>();
            requesTypeModel.Add(new RequestTypeModel { RequestType = 1, RequestName = "Web Service Code Review" });
            requesTypeModel.Add(new RequestTypeModel { RequestType = 2, RequestName = "Web Service Load Test" });
            requesTypeModel.Add(new RequestTypeModel { RequestType = 3, RequestName = "Project Issue Support" });
            requesTypeModel.Add(new RequestTypeModel { RequestType = 4, RequestName = "Project/CR Review Process" });
            requesTypeModel.Add(new RequestTypeModel { RequestType = 5, RequestName = "Project Concern/Alert" });

            return requesTypeModel;
        }

        public async Task<List<DocumentModel>> LoadDocuments(int ticket)
        {
            var Documents = aContext.tblDocuments.Where(d => d.TicketNumber == ticket);
            var orderedDocuments = Documents.OrderBy(a => a.DocumentName);
            return orderedDocuments.ToList();
        }

        public List<RequestHistoryModel> LoadHistory(int ticket)
        {
                var History = aContext.tblRequestHistory.Where(d => d.TicketNumber == ticket);
                var orderedHistory = History.OrderBy(a => a.AddDateTime);
                return orderedHistory.ToList();
        }

    }
}

