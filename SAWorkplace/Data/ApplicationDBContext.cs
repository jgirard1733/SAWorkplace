using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using SAWorkplace.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAWorkplace.Data
{
    public class ApplicationDBContext:DbContext
    {
        private readonly IConfiguration _config;

        #region Public Properties
        public DbSet<RequestModel> tblReviewRequests { get; set; }
        //public DbSet<SMERequestModel> tblReviewSMERequests { get; set; }
        public DbSet<CarrierModel> tblCarriers { get; set; }
        public DbSet<DocumentModel> tblDocuments { get; set; }
        public DbSet<RequestHistoryModel> tblRequestHistory { get; set; }
        public DbSet<LoadTestModel> tblLoadTestResults { get; set; }
        public DbSet<FeasibilityReviewModel> tblFeasibilityForms { get; set; }
		public DbSet<OktaUsersModel> tblOktaUsers { get; set; }
        public List<StatusModel> Statuses { get; set; }
        public List<ProgressModel> Progress { get; set; }
        public List<RequestTypeModel> RequestTypes { get; set; }
        public List<string> DurationTypes { get; set; }
        #endregion

        #region Constructor
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options, IConfiguration configuration) : base(options)
        {
            _config = configuration;
        }
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var path = _config.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlite(path);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }

    public class DataAccess
    {
        protected ApplicationDBContext aContext;
        protected ISession _session;

        public DataAccess(ApplicationDBContext context, ISession session)
        {
            aContext = context;
            _session = session;

        }

        public List<RequestModel> LoadRequests(string userName, int ticketNum, string PSArchitect, string searchText, string filter = "", string sort = "", string chartFilter = "", string areas = "")
        {
            var Requests = aContext.tblReviewRequests;
            string sortOrder = "RequestDate";

            if (!string.IsNullOrEmpty(sort))
            {
                sortOrder = sort.Substring(5, sort.Length - 5);
            }

            List<RequestModel> orderedRequests = new List<RequestModel>();
            List<int> myAreas = new List<int>();

            string viewType = _session.GetString("ViewType");
            if (viewType == "Admin" || viewType == "EC" || viewType == "SME")
            {
                if (viewType == "SME")
                {
                    orderedRequests = Requests.Where(a => (a.RequestType > 99) || a.Requestor == userName || a.AssignedSA.Contains(userName) || a.TeamMembers.Contains(userName)).ToList();
                    if (areas != "")
                    {
                        List<string> gatherAreas = new List<string>();
                        gatherAreas = areas.Split(",").ToList();
                        foreach (var area in gatherAreas)
                        {
                            myAreas.Add(Int32.Parse(area));
                        }
                    }
                }
                else
                {
                    orderedRequests = Requests.ToList();
                }

                //build upon filtered list
                if (ticketNum > 0)
                {
                    orderedRequests = orderedRequests.Where(a => a.TicketNumber == ticketNum).ToList();
                }
                else if (filter == "Staffing")
                {
                    orderedRequests = orderedRequests.Where(a => a.NeedsStaffing == "on" || a.NeedsStaffing == "Yes").ToList();
                }
                else if (filter == "Steel Thread")
                {
                    orderedRequests = orderedRequests.Where(a => a.SteelThread == "on" || a.SteelThread == "Yes").ToList();
                }
                else if (filter.Contains("SME"))
                {
                    if (filter == "Open SME")
                        orderedRequests = orderedRequests.Where(a => a.RequestStatus != 10 && a.RequestStatus != 3 && a.RequestStatus != 4 && a.RequestStatus != 15 && a.RequestType > 99).ToList();
                    else
                        orderedRequests = orderedRequests.Where(a => a.RequestType > 99).ToList();
                }
                else if (filter.StartsWith("My"))
                {
                    if (filter == "My Open")
                        orderedRequests = Requests.Where(a => (a.Requestor == userName || a.AssignedSA.Contains(userName) || a.TeamMembers.Contains(userName)) && a.RequestStatus != 3 && a.RequestStatus != 4 && a.RequestStatus != 10 && a.RequestStatus != 13 && a.RequestStatus != 14 && a.RequestStatus != 15).ToList();
                    else if (filter == "My Pending")
                        orderedRequests = Requests.Where(a => (a.Requestor == userName || a.AssignedSA.Contains(userName) || a.TeamMembers.Contains(userName)) && (a.RequestStatus == 13 || a.RequestStatus == 14)).ToList();
                    else if (filter == "My Closed")
                        orderedRequests = Requests.Where(a => (a.Requestor == userName || a.AssignedSA.Contains(userName) || a.TeamMembers.Contains(userName) || a.AssignedSA == userName) && (a.RequestStatus == 10 || a.RequestStatus == 3 || a.RequestStatus == 4 || a.RequestStatus == 15)).ToList();
                    else if (filter == "My Areas")
                        orderedRequests = Requests.Where(a => (a.Requestor == userName || a.AssignedSA.Contains(userName) || a.TeamMembers.Contains(userName)) && a.RequestStatus != 3 && a.RequestStatus != 4 && a.RequestStatus != 10 && a.RequestStatus != 13 && a.RequestStatus != 14 && a.RequestStatus != 15 && myAreas.Contains(a.RequestType)).ToList();
                    else
                        orderedRequests = Requests.Where(a => a.Requestor == userName || a.AssignedSA.Contains(userName) || a.TeamMembers.Contains(userName)).ToList();
                }
                else
                {
                    if (viewType != "SME" && filter != "All")
                    {
                        orderedRequests = orderedRequests.Where(a => a.RequestType < 99).ToList();
                    }

                    if (filter.Contains("Open"))
                    {
                        orderedRequests = orderedRequests.Where(a => a.RequestStatus != 10 && a.RequestStatus != 3 && a.RequestStatus != 4 && a.RequestStatus != 15).ToList();
                    }
                    
                    if (filter == "Unassigned")
                    {
                        orderedRequests = orderedRequests.Where(a => (a.AssignedSA == "" || a.AssignedSA == null)).ToList();
                    }
                    else if (filter.StartsWith("Feas"))
                    {
                        orderedRequests = orderedRequests.Where(a => a.RequestType == 4 || a.RequestType == 6 || a.RequestType == 7).ToList();
                    }
                }
            }
            else if (viewType == "Director")
            {
                var Carriers = aContext.tblCarriers.Where(t => t.Active == 1 && t.Director.Contains(userName));

                List<int> myCarriers = new List<int>();
                int id = 0;
                if (Carriers.Count() > 0)
                {
                    foreach (var carrier in Carriers)
                    {
                        id = carrier.CarrierId;
                        myCarriers.Add(id);
                    }
                }
                else
                    myCarriers.Add(0);

                orderedRequests = Requests.Where(a => (a.Requestor == userName || a.TeamMembers.Contains(userName)) || myCarriers.Contains(a.CarrierId)).ToList();
                if (ticketNum > 0)
                {
                    orderedRequests = orderedRequests.Where(a => a.TicketNumber == ticketNum).ToList();
                }
                else if (filter == "Open")
                {
                    orderedRequests = orderedRequests.Where(a => a.RequestStatus != 10 && a.RequestStatus != 3 && a.RequestStatus != 4 && a.RequestStatus != 15).ToList();
                }
            }
            else //user view
            { //adding open assigned tickets to the Arch user view
                if (PSArchitect == "True")
                {
                    orderedRequests = Requests.Where(a => (a.Requestor == userName || a.AssignedSA.Contains(userName) || a.TeamMembers.Contains(userName)) && a.RequestStatus != 3 && a.RequestStatus != 4 && a.RequestStatus != 10 && a.RequestStatus != 15).ToList();
                }
                else
                {
                    orderedRequests = Requests.Where(a => a.Requestor == userName || a.TeamMembers.Contains(userName)).ToList();
                }
                if (ticketNum > 0)
                {
                    orderedRequests = orderedRequests.Where(a => a.TicketNumber == ticketNum).ToList();
                }
            }

            //Use filtered list above and then search
            if (searchText != "")
            {
                try
                {
                    orderedRequests = orderedRequests.Where(a => 
                    (a.ProjectName != null && a.ProjectName.ToLower().Contains(searchText.ToLower()))
                    || (a.CarrierName != null && a.CarrierName.ToLower().Contains(searchText.ToLower()))
                    || (a.RequestorName != null && a.RequestorName.ToLower().Contains(searchText.ToLower())) 
                    || (a.RequestDesc != null && a.RequestDesc.ToLower().Contains(searchText.ToLower()))).ToList();
                }
                catch {}
            }

            //filter invoked from Allocation tab - pie chart click - builds upon already filtered lists
            if (chartFilter.StartsWith("rID_"))
            {
                var filterNum = 0;
                try { filterNum = Convert.ToInt32(chartFilter.Replace("rID_", "")); } catch { }
                orderedRequests = orderedRequests.Where(a => a.RequestType == filterNum).ToList();
            }
            else if (chartFilter.StartsWith("sID_"))
            {
                var filterNum = 0;
                try { filterNum = Convert.ToInt32(chartFilter.Replace("sID_", "")); } catch { }
                orderedRequests = orderedRequests.Where(a => a.RequestStatus == filterNum).ToList();
            }
            else if (chartFilter.StartsWith("carrierID_"))
            {
                var filterNum = 0;
                try
                { 
                    filterNum = Convert.ToInt32(chartFilter.Replace("carrierID_", ""));
                    orderedRequests = orderedRequests.Where(a => a.CarrierId == filterNum).ToList();
                    var carrier = orderedRequests.First();
                    _session.SetString("CarrierName", carrier.CarrierName);
                }
                catch { }
            }

            if (sortOrder.Contains("_new"))
            {
                sortOrder = sortOrder.Replace("_new", "");
                return orderedRequests.OrderByDescending(a => a.GetType().GetProperty(sortOrder).GetValue(a, null)).ToList();
            }
            else
            {
                return orderedRequests.OrderBy(a => a.GetType().GetProperty(sortOrder).GetValue(a, null)).ToList();
            }
        }

        public RequestModel LoadRequest(int ticketNum)
        {
            System.DateTime today = System.DateTime.Now;

            RequestModel model = new RequestModel();
            if (ticketNum > 0)
            {
                var requestList = aContext.tblReviewRequests.ToList().Where(t => t.TicketNumber == ticketNum).FirstOrDefault();
                if (requestList != null)
                {
	                model.RequestId = requestList.RequestId;
	                model.ProjectName = requestList.ProjectName;
	                model.ProgressType = requestList.ProgressType;
	                model.AssignedSA = requestList.AssignedSA;
	                model.AssignedSAEmail = requestList.AssignedSAEmail;
	                model.AssignedSAName = requestList.AssignedSAName;
	                model.CarrierId = requestList.CarrierId;
	                model.CarrierName = requestList.CarrierName;
	                model.RequestReviewDate = requestList.RequestReviewDate;
	                model.FeasibilityReviewDate = requestList.FeasibilityReviewDate;
                    model.InitiationReviewDate = requestList.InitiationReviewDate;
                    model.InitBuildReviewDate = requestList.InitBuildReviewDate;
                    model.ImplementationReviewDate = requestList.ImplementationReviewDate;
                    model.FeasProdDate = requestList.FeasProdDate;
                    model.WebServiceURLs = requestList.WebServiceURLs;
	                model.ProjectorCode = requestList.ProjectorCode;
	                model.RequestDate = requestList.RequestDate;
	                model.RequestDesc = requestList.RequestDesc;
	                model.RequestorEmail = requestList.RequestorEmail;
	                model.RequestorName = requestList.RequestorName;
	                model.RequestStatus = requestList.RequestStatus;
	                model.Requestor = requestList.Requestor;
	                model.RequestType = requestList.RequestType;
	                model.Requirements = requestList.Requirements;
	                model.TestTime = requestList.TestTime;
	                model.TFSPath = requestList.TFSPath;
	                model.TicketNumber = requestList.TicketNumber;
	                model.Duration1 = requestList.Duration1;
	                model.Duration2 = requestList.Duration2;
	                model.DurationType1 = requestList.DurationType1;
	                model.DurationType2 = requestList.DurationType2;
	                model.SpecialChallenge = requestList.SpecialChallenge;
	                model.ProjectType = requestList.ProjectType;
	                model.OpportunityNameNumber = requestList.OpportunityNameNumber;
	                model.Technology = requestList.Technology;
	                model.NeedsStaffing = requestList.NeedsStaffing;
	                model.TeamMembers = requestList.TeamMembers;
	                model.SteelThread = requestList.SteelThread;
	                model.SteelThreadDetail = requestList.SteelThreadDetail;
                    model.eSig = requestList.eSig;
                    model.Director = requestList.Director;
                    model.Splunk = requestList.Splunk;
                    model.NeedInitBuildReview = requestList.NeedInitBuildReview;
                    model.SANotes = requestList.SANotes;
                }
            }
            else
            {
                model.ProjectName = "";
                model.ProgressType = 1;
                model.AssignedSA = "";
                model.AssignedSAEmail = "";
                model.AssignedSAName = "";
                model.CarrierId = 0;
                model.CarrierName = "";
                model.RequestReviewDate = null;
                model.FeasibilityReviewDate = null;
                model.InitiationReviewDate = null;
                model.InitBuildReviewDate = null;
                model.ImplementationReviewDate = null;
                model.FeasProdDate = null;
                model.WebServiceURLs = "";
                model.ProjectorCode = "";
                model.RequestDesc = "";
                model.RequestorEmail = "";
                model.RequestorName = "";
                model.RequestStatus = 1;
                model.RequestType = 0;
                model.Requirements = "";
                model.TestTime = null;
                model.TFSPath = "";
                model.Duration1 = "";
                model.Duration2 = "";
                model.DurationType1 = "";
                model.DurationType2 = "";
                model.SpecialChallenge = "";
                model.ProjectType = "";
                model.OpportunityNameNumber = "";
                model.Technology = "";
                model.NeedsStaffing = "";
                model.TeamMembers = "";
                model.SteelThread = "";
                model.SteelThreadDetail = "";
                model.eSig = "";
                model.Director = "";
                model.Splunk = "";
                model.NeedInitBuildReview = "";
                model.SANotes = "";
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

        public async Task<List<CarrierModel>> LoadSingleCarrier(int carrierId)
        {
            if (aContext.tblCarriers.Local.Count() != aContext.tblCarriers.Count())
            {
                var Carriers = aContext.tblCarriers.Where(t => t.CarrierId == carrierId);
                return Carriers.ToList();
            }
            else
            {
                var Carriers = aContext.tblCarriers.Local.Where(t => t.CarrierId == carrierId);
                return Carriers.ToList();
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
            statusModel.Add(new StatusModel { ID = 9, Text = "Needs Attention!", Class = "alert-custom-closed" });
            statusModel.Add(new StatusModel { ID = 10, Text = "Complete - Passed", Class = "alert-custom-complete" });
            statusModel.Add(new StatusModel { ID = 11, Text = "In Escalation!", Class = "alert-custom-closed" });
            statusModel.Add(new StatusModel { ID = 12, Text = "Change Request Review", Class = "alert-custom-addendum" });
            statusModel.Add(new StatusModel { ID = 13, Text = "Pending Further Review", Class = "alert-custom-pending" });
            statusModel.Add(new StatusModel { ID = 14, Text = "On Hold", Class = "alert-custom-hold" });
            statusModel.Add(new StatusModel { ID = 15, Text = "Closed - Incomplete", Class = "alert-custom-closed" });
            statusModel.Add(new StatusModel { ID = 16, Text = "Insufficient Requirements", Class = "alert-custom-closed" });
            statusModel.Add(new StatusModel { ID = 17, Text = "Post Initial Build Review", Class = "alert-custom-implementation" });

            return statusModel;
        }

        public async Task<List<ProgressModel>> GetProgress()
        {
            var progressModel = new List<ProgressModel>();
            //progressModel.Add(new ProgressModel { ProgressId = 1, ProgressClass = "<div class='info-text'>Not Started</div><div class='progress' style='width:200px'><div class='progress-bar progress-bar-striped bg-primary' role='progressbar' style='width: 0%;' aria-valuenow='0' aria-valuemin='0' aria-valuemax='100'></div></div>" });
            progressModel.Add(new ProgressModel { ProgressId = 1, ProgressClass = "<div class='info-text'>Not Started</div><div class='progress' style='width:200px'><div class='ui standard active progress' role='progressbar' style='width: 100%;margin:0px 0px 0px 0px !important' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100'><div class='bar progress-bar-striped' style='width:0%; height:18px'><div class='progress' style='color:#FFFFFF !important'>0%</div></div></div></div>" });
           
            //progressModel.Add(new ProgressModel { ProgressId = 2, ProgressClass = "<div class='info-text'>In Progress</div><div class='progress' style='width:200px'><div class='progress-bar progress-bar-striped bg-primary' role='progressbar' style='width: 25%;' aria-valuenow='25' aria-valuemin='0' aria-valuemax='100'>25%</div></div>" });
            progressModel.Add(new ProgressModel { ProgressId = 2, ProgressClass = "<div class='info-text'>In Progress</div><div class='progress' style='width:200px'><div class='ui blue standard active progress' role='progressbar' style='width: 100%;margin:0px 0px 0px 0px !important' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100'><div class='bar progress-bar-striped' style='width:25%; height:18px'><div class='progress' style='color:#FFFFFF !important'>25%</div></div></div></div>" });

            //progressModel.Add(new ProgressModel { ProgressId = 3, ProgressClass = "<div class='info-text'>In Progress</div><div class='progress' style='width:200px'><div class='progress-bar progress-bar-striped bg-primary' role='progressbar' style='width: 50%;' aria-valuenow='50' aria-valuemin='0' aria-valuemax='100'>50%</div></div>" });
            progressModel.Add(new ProgressModel { ProgressId = 3, ProgressClass = "<div class='info-text'>In Progress</div><div class='progress' style='width:200px'><div class='ui blue standard active progress' role='progressbar' style='width: 100%;margin:0px 0px 0px 0px !important' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100'><div class='bar progress-bar-striped' style='width:50%; height:18px'><div class='progress' style='color:#FFFFFF !important'>50%</div></div></div></div>" });

            //progressModel.Add(new ProgressModel { ProgressId = 4, ProgressClass = "<div class='info-text'>In Progress</div><div class='progress' style='width:200px'><div class='progress-bar progress-bar-striped bg-primary' role='progressbar' style='width: 75%;' aria-valuenow='75' aria-valuemin='0' aria-valuemax='100'>75%</div></div>" });
            progressModel.Add(new ProgressModel { ProgressId = 4, ProgressClass = "<div class='info-text'>In Progress</div><div class='progress' style='width:200px'><div class='ui blue standard active progress' role='progressbar' style='width: 100%;margin:0px 0px 0px 0px !important' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100'><div class='bar progress-bar-striped' style='width:75%; height:18px'><div class='progress' style='color:#FFFFFF !important'>75%</div></div></div></div>" });

            //progressModel.Add(new ProgressModel { ProgressId = 5, ProgressClass = "<div class='info-text'>Complete!</div><div class='progress' style='width:200px'><div class='progress-bar progress-bar-striped bg-success' role='progressbar' style='width: 100%;' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100'>100%</div></div>" });
            progressModel.Add(new ProgressModel { ProgressId = 5, ProgressClass = "<div class='info-text'>Complete!</div><div class='progress' style='width:200px'><div class='ui green standard active progress' role='progressbar' style='width: 100%;margin:0px 0px 0px 0px !important' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100'><div class='bar progress-bar-striped' style='width:100%; height:18px'><div class='progress' style='color:#FFFFFF !important'>100%</div></div></div></div>" });
           
            
            
            //SA Review Process
            progressModel.Add(new ProgressModel { ProgressId = 6, ProgressClass = "<div class='info-text'>Feasibility Review In Progress</div><div class='progress' style='width:200px'><div class='ui blue standard active progress' role='progressbar' style='width: 100%;margin:0px 0px 0px 0px !important' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100'><div class='bar progress-bar-striped' style='width:25%; height:18px'><div class='progress' style='color:#FFFFFF !important'>25%</div></div></div></div>" });
            progressModel.Add(new ProgressModel { ProgressId = 7, ProgressClass = "<div class='info-text'>Initiation Review In Progress</div><div class='progress' style='width:200px'><div class='ui blue standard active progress' role='progressbar' style='width: 100%;margin:0px 0px 0px 0px !important' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100'><div class='bar progress-bar-striped' style='width:50%; height:18px'><div class='progress' style='color:#FFFFFF !important'>50%</div></div></div></div>" });
            progressModel.Add(new ProgressModel { ProgressId = 8, ProgressClass = "<div class='info-text'>Implementation Review In Progress</div><div class='progress' style='width:200px'><div class='ui blue standard active progress' role='progressbar' style='width: 100%;margin:0px 0px 0px 0px !important' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100'><div class='bar progress-bar-striped' style='width:75%; height:18px'><div class='progress' style='color:#FFFFFF !important'>75%</div></div></div></div>" });

            return progressModel;
        }

        public async Task<List<RequestTypeModel>> GetRequestTypes()
        {
            var requestTypeModel = new List<RequestTypeModel>();
            requestTypeModel.Add(new RequestTypeModel { RequestType = 1, RequestName = "Web Service Code Review" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 2, RequestName = "Web Service Load Test" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 3, RequestName = "Project Issue Support" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 4, RequestName = "Feasibility Review" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 5, RequestName = "Project Concern/Alert" });

            //6 = Initiation
            //7 = Implementation
            //8 = Post Initial Build - new optional step before Implementation

            requestTypeModel.Add(new RequestTypeModel { RequestType = 100, RequestName = "ACORD/Data Dictionary" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 200, RequestName = "AWS" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 300, RequestName = "Base Issue/Question" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 400, RequestName = "Dev Ops" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 500, RequestName = "DTC" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 600, RequestName = "eSignature" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 700, RequestName = "Foundations/Standards" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 800, RequestName = "Go/No Go" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 900, RequestName = "JavaScript" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 1000, RequestName = "Medical Vendor (MRAS, SwissRe, etc)" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 1100, RequestName = "NGSD" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 1200, RequestName = "Security" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 1300, RequestName = "UI/UX" });
            requestTypeModel.Add(new RequestTypeModel { RequestType = 1400, RequestName = "Web Services" });


            return requestTypeModel;
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

        public async Task<List<LoadTestModel>> LoadTestResults(int ticket)
        {
            var Results = aContext.tblLoadTestResults.Where(l => l.TicketNumber == ticket).OrderByDescending(n => n.InterfaceName);
            return Results.ToList();
        }

        public async Task<List<DurationTypesModel>> GetDurationTypes()
        {
            var durTypeModel = new List<DurationTypesModel>();
            durTypeModel.Add(new DurationTypesModel { Name = "Meeting(s)", Value = "Meeting(s)" });
            durTypeModel.Add(new DurationTypesModel { Name = "Day(s)", Value = "Day(s)" });
            durTypeModel.Add(new DurationTypesModel { Name = "Week(s)", Value = "Week(s)" });
            durTypeModel.Add(new DurationTypesModel { Name = "Sprint(s)", Value = "Sprint(s)" });
            durTypeModel.Add(new DurationTypesModel { Name = "Month(s)", Value = "Month(s)" });

            return durTypeModel;
        }

        public async Task<FeasibilityReviewModel> LoadFeasForm(int ticket)
        {

            var feasModel = aContext.tblFeasibilityForms.Where(d => d.TicketNumber == ticket).FirstOrDefault();
            return feasModel;
        }
    }
}

