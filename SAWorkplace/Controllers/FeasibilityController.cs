using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAWorkplace.Data;
using SAWorkplace.Models;
using SAWorkplace.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using LoggerService;
using Newtonsoft.Json;

namespace SAWorkplace.Controllers
{
    public class FeasibilityController : Controller
    {
        protected ApplicationDBContext aContext;
        private readonly IConfiguration _config;
        protected EmailHelper eHelper;
        protected OKTAHelper oHelper;
        protected string mAssignedSA;
        private IHubContext<Services.SignalR_Notifications> _hubContext;
        private static ILoggerManager _logger;

        public FeasibilityController(ApplicationDBContext context, IConfiguration configuration, ILoggerManager logger)
        {
            aContext = context;
            _config = configuration;
            _logger = logger;
            eHelper = new EmailHelper(context,configuration, logger);
            oHelper = new OKTAHelper(context, configuration, logger);
        }

        [HttpGet]
        [Route("Feasibility/AddFeasibility")]
        public async Task<IActionResult> AddFeasibilityReview()
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            //add logic if session is blank
            if (HttpContext.Session.GetString("UserID") == "" || HttpContext.Session.GetString("UserName") == "")
            {
                var member = oHelper.GetUserInfo(HttpContext.User.Claims);
                HttpContext.Session.SetString("UserID", member.Result.UserId);
                HttpContext.Session.SetString("UserName", member.Result.UserName);
                HttpContext.Session.SetString("UserEmail", member.Result.EmailAddress);
            }

            HttpContext.Session.SetString("backupUserName", HttpContext.Session.GetString("UserName"));

            int matchedId = 0;
            int newTicket = 0;
            do
            {
                Random newRandom = new Random();
                newTicket = newRandom.Next(100000, 999999);
                var matchLookup = aContext.tblReviewRequests.Where(x => x.TicketNumber == newTicket);
                matchedId = matchLookup.Select(a => a.TicketNumber).FirstOrDefault();
            }
            while (matchedId > 0);

            requestEdit.Requests = data.LoadRequest(0);
            requestEdit.Carriers = await data.LoadActiveCarriers();
            requestEdit.Status = await data.GetStatuses();
            requestEdit.Progress = await data.GetProgress();
            requestEdit.RequestTypes = await data.GetRequestTypes();
            requestEdit.TeamMembers = await oHelper.callOKTA();
            requestEdit.Director = requestEdit.TeamMembers; //should be same list

            requestEdit.Requests.TicketNumber = newTicket;

            return View("/Views/Request/AddFeasibilityReview.cshtml", requestEdit);
        }

        [HttpPost]
        [Route("Feasibility/AddFeasibility")]
        public async Task<IActionResult> AddFeasibilityReview([FromServices]ApplicationDBContext context, [FromForm]RequestEditModel request, string updatedLanguages, string updatedSelling, string updatedInsurance, string updatedProducts, string updatedTechnology, string updatedTeamMembers, string updatedDirector, [FromServices]IHubContext<Services.SignalR_Notifications> hubContext)
        {
            _hubContext = hubContext;
            //add logic if session is blank
            if (HttpContext.Session.GetString("UserID") == "" || HttpContext.Session.GetString("UserName") == "")
            {
                _logger.LogDebug("AddFeasibility: User is blank! TicketNumber = " + request.Requests.TicketNumber + ", ");

                var member = oHelper.GetUserInfo(HttpContext.User.Claims);
                HttpContext.Session.SetString("UserID", member.Result.UserId);
                HttpContext.Session.SetString("UserName", member.Result.UserName);
                HttpContext.Session.SetString("UserEmail", member.Result.EmailAddress);
            }
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            var carrier = context.tblCarriers.Where(t => t.CarrierId == request.Requests.CarrierId).FirstOrDefault();
            var myCarrierName = carrier.CarrierName;
            request.Requests.CarrierName = myCarrierName;
            request.Requests.RequestReviewDate = request.Requests.FeasProdDate;
            request.Requests.RequestType = 4;
            request.Requests.RequestStatus = 1;
            request.Requests.RequestDate = DateTime.Now;
            request.Requests.RequestorEmail = userEmail;
            request.Requests.RequestorName = userName;

            if (request.Requests.eSig == "on")
            {
                request.Requests.eSig = "Yes";
            }
            else if (request.Requests.eSig == "off" || request.Requests.eSig == "" || request.Requests.eSig is null)
            {
                request.Requests.eSig = "No";
            }

            if (request.Requests.Splunk == "on")
            {
                request.Requests.Splunk = "Yes";
            }
            else if (request.Requests.Splunk == "off" || request.Requests.Splunk == "" || request.Requests.Splunk is null)
            {
                request.Requests.Splunk = "No";
            }

            _logger.LogDebug("AddFeasibility: TicketNumber = " + request.Requests.TicketNumber + ", CarrierName = " + myCarrierName + ", Requirements = " + request.Requests.Requirements + ", RequestorName = " + userName);

            context.Add(new RequestModel
            {
                ProjectName = System.Net.WebUtility.HtmlEncode(request.Requests.ProjectName),
                ProgressType = 1,
                AssignedSA = "",
                AssignedSAEmail = "",
                AssignedSAName = "",
                CarrierId = request.Requests.CarrierId,
                CarrierName = myCarrierName,
                RequestReviewDate = request.Requests.FeasProdDate,
                FeasProdDate = request.Requests.FeasProdDate,
                WebServiceURLs = request.Requests.WebServiceURLs,
                ProjectorCode = request.Requests.ProjectorCode,
                RequestDate = DateTime.Now,
                RequestDesc = request.Requests.RequestDesc,
                Requestor = userId,
                RequestorEmail = userEmail,
                RequestorName = userName,
                RequestStatus = 1,
                RequestType = 4,
                Requirements = request.Requests.Requirements,
                TestTime = request.Requests.TestTime,
                TFSPath = request.Requests.TFSPath,
                TicketNumber = request.Requests.TicketNumber,
                OpportunityNameNumber = request.Requests.OpportunityNameNumber,
                SpecialChallenge = request.Requests.SpecialChallenge,
                Technology = updatedTechnology,
                ProjectType = request.Requests.ProjectType,
                NeedsStaffing = "No",
                TeamMembers = updatedTeamMembers,
                SteelThread = "No",
                SteelThreadDetail = "",
                eSig = request.Requests.eSig,
                Director = updatedDirector,
                Splunk = request.Requests.Splunk,
                NeedInitBuildReview = "No",
                SANotes = ""
            });

            var updatedCarrier = context.tblCarriers.FirstOrDefault(updateableCarrier => updateableCarrier.CarrierId == request.Requests.CarrierId);
            if (updatedCarrier != null)
            {
                updatedCarrier.SupportedLanguages = updatedLanguages;
                updatedCarrier.InsuranceProducts = updatedInsurance;
                updatedCarrier.iPipelineProducts = updatedProducts;
                updatedCarrier.SellingModel = updatedSelling;
                updatedCarrier.TeamMembers = updatedTeamMembers;
                updatedCarrier.Director = updatedDirector;
                context.tblCarriers.Update(updatedCarrier);
            }

            context.SaveChanges();

            eHelper.sendEmails(request, "add", userName, userEmail, "");

            await _hubContext.Clients.All.SendAsync("Review_Added", request.Requests.TicketNumber);

            return Json(new { success = true, responseType = "Request", responseText = "Request Saved" });
        }


        [HttpGet]
        [Route("Feasibility/EditFeasibility")]
        public async Task<IActionResult> EditFeasibilityReview(int ticketNum)
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));

            //save old status to compare with after they close the ticket
            var status = requestEdit.Requests.RequestStatus;
            HttpContext.Session.SetInt32("FeasRequestStatus", status);

            requestEdit.Carriers = await data.LoadActiveCarriers();
            requestEdit.Status = await data.GetStatuses();
            requestEdit.Progress = await data.GetProgress();
            requestEdit.RequestTypes = await data.GetRequestTypes();
            requestEdit.Documents = await data.LoadDocuments(ticketNum);
            requestEdit.History = data.LoadHistory(ticketNum);
            requestEdit.FeasForms = await data.LoadFeasForm(ticketNum);
            return View("/Views/Request/SAFeasibilityReview.cshtml", requestEdit);
        }

        [HttpPost]
        [Route("Feasibility/EditFeasibility")]
        public async Task<IActionResult> EditFeasibilityReview([FromServices]ApplicationDBContext context, RequestEditModel request, string newComment, string submitType)
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");
            string viewType = HttpContext.Session.GetString("ViewType");
            bool emailSent = false;
            bool requestMoved = false;

            if (viewType == "Admin")
            {
                if (submitType == "1")
                {
                    DateTime selectedDate;
                    if (request.Requests.FeasProdDate != null)
                        selectedDate = request.Requests.FeasProdDate.Value;
                    else
                        selectedDate = BusinessDays.AddBusinessDays(DateTime.Now, 2);
                    //moving request to next step
                    if (request.Requests.RequestType == 4)
                    {
                        request.Requests.RequestStatus = 6;
                        request.Requests.RequestType = 6;
                        request.Requests.ProgressType = 7;

                        requestMoved = true;
                        request.Requests.FeasibilityReviewDate = DateTime.Now;
                        request.Requests.RequestReviewDate = selectedDate;

                        context.Add(new RequestHistoryModel
                        {
                            TicketNumber = request.Requests.TicketNumber,
                            AddDateTime = DateTime.Now,
                            History = "<b><font color='blue'>Request moved to Initiation Review</font></b>",
                            AddedBy = userName
                        });
                    }
                    else if (request.Requests.RequestType == 6)
                    { //Initiation
                        if (request.Requests.RequestStatus == 12)
                        {
                            request.Requests.RequestStatus = 6;
                            request.Requests.RequestType = 6;
                            request.Requests.ProgressType = 7;
                        }
                        else
                        {
                            var review = "";
                            if (request.Requests.NeedInitBuildReview == "Yes")
                            {
                                review = "Post Initial Build";
                                request.Requests.RequestStatus = 17;
                                request.Requests.RequestType = 8;
                                request.Requests.ProgressType = 8;
                            }
                            else
                            {
                                review = "Implementation";
                                request.Requests.RequestStatus = 7;
                                request.Requests.RequestType = 7;
                                request.Requests.ProgressType = 8;
                            }

                            requestMoved = true;
                            request.Requests.InitiationReviewDate = DateTime.Now;
                            request.Requests.RequestReviewDate = selectedDate;

                            context.Add(new RequestHistoryModel
                            {
                                TicketNumber = request.Requests.TicketNumber,
                                AddDateTime = DateTime.Now,
                                History = "<b><font color='blue'>Request moved to " + review + " Review</font></b>",
                                AddedBy = userName
                            });
                        }
                    }
                    //new type - Post Initial Build
                    else if (request.Requests.RequestType == 8)
                    {
                        if (request.Requests.RequestStatus == 12)
                        {
                            request.Requests.RequestStatus = 17;
                            request.Requests.RequestType = 8;
                            request.Requests.ProgressType = 8;
                        }
                        else
                        {
                            request.Requests.RequestStatus = 7;
                            request.Requests.RequestType = 7;
                            request.Requests.ProgressType = 8;

                            requestMoved = true;
                            request.Requests.InitBuildReviewDate = DateTime.Now;
                            request.Requests.RequestReviewDate = selectedDate;

                            context.Add(new RequestHistoryModel
                            {
                                TicketNumber = request.Requests.TicketNumber,
                                AddDateTime = DateTime.Now,
                                History = "<b><font color='blue'>Request moved to Implementation Review</font></b>",
                                AddedBy = userName
                            });
                        }
                    }
                    else if (request.Requests.RequestType == 7)
                    {
                        if (request.Requests.RequestStatus == 12)
                        {
                            request.Requests.RequestStatus = 7;
                            request.Requests.RequestType = 7;
                            request.Requests.ProgressType = 8;
                        }
                        else
                        {
                            request.Requests.RequestStatus = 3;
                            request.Requests.RequestType = 7;
                            request.Requests.ProgressType = 5;

                            requestMoved = true;
                            request.Requests.ImplementationReviewDate = DateTime.Now;
                            request.Requests.RequestReviewDate = selectedDate;

                            context.Add(new RequestHistoryModel
                            {
                                TicketNumber = request.Requests.TicketNumber,
                                AddDateTime = DateTime.Now,
                                History = "<b><font color='blue'>Request moved to Production Ready</font></b>",
                                AddedBy = userName
                            });
                        }
                    }
                }

                else if (request.Requests.AssignedSA != null && request.Requests.ProgressType == 1)
                {
                    if (request.Requests.RequestType == 4)
                    {
                        request.Requests.RequestStatus = 5;
                        request.Requests.ProgressType = 6;
                    }

                    eHelper.sendEmails(request, "assigned", userName, userEmail, newComment);
                    emailSent = true;
                }

                if (request.Requests.RequestType < 4 || request.Requests.RequestType == 5)
                {
                    //changing from Feasibility review to other type
                    if (request.Requests.RequestStatus == 5)
                    {
                        if (request.Requests.AssignedSA != null)
                            request.Requests.RequestStatus = 2;
                        else
                            request.Requests.RequestStatus = 1;
                    }

                    if (request.Requests.RequestStatus == 1)
                    {
                        request.Requests.ProgressType = 1;
                    }
                    //else if closed
                    else if (request.Requests.RequestStatus == 3 || request.Requests.RequestStatus == 4 || request.Requests.RequestStatus == 10 || request.Requests.RequestStatus == 15)
                    {
                        request.Requests.ProgressType = 5;
                    }
                    else
                    {
                        request.Requests.ProgressType = 2;
                    }

                    if (string.IsNullOrEmpty(request.Requests.RequestDesc))
                        request.Requests.RequestDesc = request.Requests.Requirements;
                }

                if (request.Requests.RequestStatus == 3 || request.Requests.RequestStatus == 4)
                {
                    request.Requests.NeedsStaffing = "No";
                }
            }

            if (request.Requests.NeedInitBuildReview == "on")
            {
                request.Requests.NeedInitBuildReview = "Yes";
            }
            else if (request.Requests.NeedInitBuildReview == "off" || request.Requests.NeedInitBuildReview == "")
            {
                request.Requests.NeedInitBuildReview = "No";
            }
            if (request.Requests.NeedsStaffing == "on")
            {
                request.Requests.NeedsStaffing = "Yes";
            }
            else if (request.Requests.NeedsStaffing == "off" || request.Requests.NeedsStaffing == "")
            {
                request.Requests.NeedsStaffing = "No";
            }
            if (request.Requests.SteelThread == "on")
            {
                request.Requests.SteelThread = "Yes";
            }
            else if (request.Requests.SteelThread == "off" || request.Requests.SteelThread == "" || request.Requests.SteelThread is null)
            {
                request.Requests.SteelThread = "No";
            }

            var oldStatus = HttpContext.Session.GetInt32("FeasRequestStatus");
            if (oldStatus > 0 && oldStatus != request.Requests.RequestStatus && requestMoved == false)
            {
                try
                {
                    request.Status = await data.GetStatuses();
                    StatusModel status = request.Status.Where(x => x.ID.Equals(request.Requests.RequestStatus)).FirstOrDefault();
                    var statusName = status.Text;

                    context.Add(new RequestHistoryModel
                    {
                        TicketNumber = request.Requests.TicketNumber,
                        AddDateTime = DateTime.Now,
                        History = "<b><font color='blue'>Request Status updated to " + statusName + "</font></b>",
                        AddedBy = userName
                    });
                }
                catch { }
            }

            try
            {
                var carrier = context.tblCarriers.Where(t => t.CarrierId == request.Requests.CarrierId).FirstOrDefault();
                var myCarrierName = carrier.CarrierName;
                request.Requests.CarrierName = myCarrierName;
            }
            catch { }

            context.tblReviewRequests.Attach(request.Requests);
            context.Entry(request.Requests).Property(x => x.AssignedSA).IsModified = true;
            context.Entry(request.Requests).Property(x => x.AssignedSAEmail).IsModified = true;
            context.Entry(request.Requests).Property(x => x.AssignedSAName).IsModified = true;
            context.Entry(request.Requests).Property(x => x.RequestStatus).IsModified = true;
            context.Entry(request.Requests).Property(x => x.ProgressType).IsModified = true;                
            context.Entry(request.Requests).Property(x => x.RequestType).IsModified = true;
            context.Entry(request.Requests).Property(x => x.NeedsStaffing).IsModified = true;
            context.Entry(request.Requests).Property(x => x.SteelThread).IsModified = true;
            context.Entry(request.Requests).Property(x => x.SteelThreadDetail).IsModified = true;
            context.Entry(request.Requests).Property(x => x.FeasibilityReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.InitiationReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.InitBuildReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.ImplementationReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.RequestReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.FeasProdDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.NeedInitBuildReview).IsModified = true;
            context.Entry(request.Requests).Property(x => x.SANotes).IsModified = true;

            if (request.Requests.RequestType < 4 || request.Requests.RequestType == 5)
                context.Entry(request.Requests).Property(x => x.RequestDesc).IsModified = true;

            if (newComment != "" && newComment != null)
            {
                context.Add(new RequestHistoryModel
                {
                    TicketNumber = request.Requests.TicketNumber,
                    AddDateTime = DateTime.Now,
                    History = newComment,
                    AddedBy = userName
                });
            }
            context.SaveChanges();

            if (emailSent == false)
            {
                if (viewType == "EC")
                {
                    eHelper.sendEmails(request, "ECupdate", userName, userEmail, newComment);
                }
                else
                {
                    eHelper.sendEmails(request, "SAupdate", userName, userEmail, newComment);
                }
            }

            return Json(new { success = true, responseType = "Request", responseText = "Request Saved" });
        }
       
        [HttpGet]
        [Route("Feasibility/ViewFeasibility")]
        public async Task<IActionResult> ViewFeasibilityReview(int ticketNum)
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));
            requestEdit.Carriers = await data.LoadActiveCarriers();
            requestEdit.Status = await data.GetStatuses();
            requestEdit.Progress = await data.GetProgress();
            requestEdit.RequestTypes = await data.GetRequestTypes();
            requestEdit.Documents = await data.LoadDocuments(ticketNum);
            requestEdit.History = data.LoadHistory(ticketNum);
            requestEdit.TeamMembers = await oHelper.callOKTA();
            requestEdit.Director = requestEdit.TeamMembers;
            requestEdit.FeasForms = await data.LoadFeasForm(ticketNum);

            //get the dates to display at top for older tickets
            if (requestEdit.Requests.FeasibilityReviewDate == null || requestEdit.Requests.FeasibilityReviewDate == requestEdit.Requests.RequestReviewDate)
            {
                if (requestEdit.FeasForms != null)
                {
                    var feasDate = requestEdit.FeasForms.Feas_ReviewDate;
                    if (feasDate != null)
                    {
                        try
                        {
                            var len = feasDate.Length;
                            if (len > 10) len = 10;
                            requestEdit.Requests.FeasibilityReviewDate = DateTime.Parse(feasDate.Substring(0, len));
                        }
                        catch { }
                    }
                }
                else
                {
                    var feasDoc = requestEdit.Documents.Where(d => d.DocumentType == "Feasibility Review").FirstOrDefault();
                    if (feasDoc != null)
                    {
                        var docDate = feasDoc.DocumentModifiedDate;
                        requestEdit.Requests.FeasibilityReviewDate = docDate;
                    }
                }
            }

            if (requestEdit.Requests.InitiationReviewDate == null)
            {
                if (requestEdit.FeasForms != null)
                {
                    var initDate = requestEdit.FeasForms.Init_ReviewDate;
                    if (initDate != null)
                    {
                        try
                        {
                            var len = initDate.Length;
                            if (len > 10) len = 10;
                            requestEdit.Requests.InitiationReviewDate = DateTime.Parse(initDate.Substring(0, len));
                        }
                        catch { }
                    }
                }
                else
                {
                    var initDoc = requestEdit.Documents.Where(d => d.DocumentType == "Initiation Review").FirstOrDefault();
                    if (initDoc != null)
                    {
                        var docDate = initDoc.DocumentModifiedDate;
                        requestEdit.Requests.InitiationReviewDate = docDate;
                    }
                }
            }

            if (requestEdit.Requests.ImplementationReviewDate == null)
            {
                if (requestEdit.FeasForms != null)
                {
                    var implDate = requestEdit.FeasForms.Impl_ReviewDate;
                    if (implDate != null)
                    {
                        try
                        {
                            var len = implDate.Length;
                            if (len > 10) len = 10;
                            requestEdit.Requests.ImplementationReviewDate = DateTime.Parse(implDate.Substring(0, len));
                        }
                        catch { }
                    }
                }
                else
                {
                    var implDoc = requestEdit.Documents.Where(d => d.DocumentType == "Implementation Review").FirstOrDefault();
                    if (implDoc != null)
                    {
                        var docDate = implDoc.DocumentModifiedDate;
                        requestEdit.Requests.ImplementationReviewDate = docDate;
                    }
                }
            }
            return View("/Views/Request/RequestorFeasibilityReview.cshtml", requestEdit);
        }

        [HttpPost]

        [Route("Feasibility/ViewFeasibility")]
        public async Task<IActionResult> ViewFeasibilityReview([FromServices]ApplicationDBContext context, RequestEditModel request, string newComment, string updatedTeamMembers, string updatedDirector)
        {
            LoggerManager log = new LoggerManager();
            log.LogDebug("hey");
         
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            //request.Requests.RequestReviewDate = request.Requests.FeasibilityReviewDate;
            request.Requests.TeamMembers = updatedTeamMembers;
            request.Requests.Director = updatedDirector;
            request.Requests.RequestReviewDate = request.Requests.FeasProdDate;

            context.tblReviewRequests.Attach(request.Requests);
            context.Entry(request.Requests).Property(x => x.RequestDesc).IsModified = true;
            context.Entry(request.Requests).Property(x => x.ProjectorCode).IsModified = true;
            context.Entry(request.Requests).Property(x => x.RequestReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.FeasProdDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.FeasibilityReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.InitiationReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.InitBuildReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.ImplementationReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.OpportunityNameNumber).IsModified = true;
            context.Entry(request.Requests).Property(x => x.Requirements).IsModified = true;
            context.Entry(request.Requests).Property(x => x.SpecialChallenge).IsModified = true;
            context.Entry(request.Requests).Property(x => x.ProjectType).IsModified = true;
            context.Entry(request.Requests).Property(x => x.TeamMembers).IsModified = true;
            context.Entry(request.Requests).Property(x => x.Director).IsModified = true;
            context.Entry(request.Requests).Property(x => x.eSig).IsModified = true;
            context.Entry(request.Requests).Property(x => x.Splunk).IsModified = true;
            context.Entry(request.Requests).Property(x => x.NeedInitBuildReview).IsModified = true;

            var updatedCarrier = context.tblCarriers.FirstOrDefault(updateableCarrier => updateableCarrier.CarrierId == request.Requests.CarrierId);
            if (updatedCarrier != null)
            {
                //updatedCarrier.SupportedLanguages = updatedLanguages;
                //updatedCarrier.InsuranceProducts = updatedInsurance;
                //updatedCarrier.iPipelineProducts = updatedProducts;
                //updatedCarrier.SellingModel = updatedSelling;
                updatedCarrier.TeamMembers = updatedTeamMembers;
                updatedCarrier.Director = updatedDirector;
                context.tblCarriers.Update(updatedCarrier);
            }

            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            if (newComment != "" && newComment != null)
            {
                context.Add(new RequestHistoryModel
                {
                    TicketNumber = request.Requests.TicketNumber,
                    AddDateTime = DateTime.Now,
                    History = newComment,
                    AddedBy = userName
                });
            }
            context.SaveChanges();
           eHelper.sendEmails(request, "edit", userName, userEmail, newComment);

            return Json(new { success  = true, responseType = "Request", responseText = "Request Saved" });
        }

        [HttpGet]
        [Route("Feasibility/AddendumReview")]
        public async Task<IActionResult> AddendumReview(int ticketNum)
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));
            requestEdit.History = data.LoadHistory(ticketNum);
            requestEdit.Status = await data.GetStatuses();
            requestEdit.Documents = await data.LoadDocuments(ticketNum);

            mAssignedSA = requestEdit.Requests.AssignedSA;

            return PartialView("/Views/Modal/AddendumReview.cshtml", requestEdit);
        }

        [HttpPost]
        [Route("Feasibility/AddendumReview")]
        public IActionResult AddendumReview([FromServices]ApplicationDBContext context, RequestEditModel request, string newComment)
        {
            //add email to requestor when assigned
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            string reviewDate = "";
            if (request.Requests.RequestReviewDate != null)
                reviewDate = request.Requests.RequestReviewDate.Value.ToShortDateString();
            if (newComment == null) { newComment = ""; }

            context.Add(new RequestHistoryModel
            {
                TicketNumber = request.Requests.TicketNumber,
                AddDateTime = DateTime.Now,
                History = "<b><font color='blue'>Created Change Request Review - " + reviewDate + "</font></b> - " + newComment,
                AddedBy = userName
            });

            //request.Requests.ProgressType = (int)request.Requests.ProgressType - 1;
            request.Requests.RequestStatus = 12;

            context.tblReviewRequests.Attach(request.Requests);
            //context.Entry(request.Requests).Property(x => x.RequestReviewDate).IsModified = true;
            context.Entry(request.Requests).Property(x => x.FeasibilityReviewDate).IsModified = true;
            //context.Entry(request.Requests).Property(x => x.ProgressType).IsModified = true;
            //context.Entry(request.Requests).Property(x => x.ProjectorCode).IsModified = false;
            context.Entry(request.Requests).Property(x => x.RequestStatus).IsModified = true;
            context.SaveChanges();

            eHelper.sendEmails(request, "edit", userName, userEmail, newComment);

            return Json(new { success = true, responseType = "Request", responseText = "Request Updated", ticketNum = request.Requests.TicketNumber  });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRequest(int requestID)
        {
            var delDocument = aContext.tblReviewRequests.Find(requestID);
            aContext.tblReviewRequests.Remove(delDocument);
            aContext.SaveChanges();

            return Json(new { success = true, responseType = "Request", responseText = "Request Deleted" });
        }

        [HttpGet]
        [Route("Feasibility/FeasibilityForm")]
        public async Task<IActionResult> FeasibilityForm(int ticketNum)
        {
            string userName = HttpContext.Session.GetString("UserName");
            string viewType = HttpContext.Session.GetString("ViewType");

            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();
            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));

            mAssignedSA = requestEdit.Requests.AssignedSA;

            var feasModel = aContext.tblFeasibilityForms.ToList().Where(t => t.TicketNumber == ticketNum).FirstOrDefault();

            if (feasModel == null)
            {
                feasModel = new FeasibilityReviewModel();
                feasModel.TicketNumber = ticketNum;
                feasModel.ProjectName = requestEdit.Requests.ProjectName;
                feasModel.TeamMembers = requestEdit.Requests.TeamMembers;
                if (viewType == "Admin")
                {
                    feasModel.Feas_ReviewedBy = userName;
                    feasModel.Feas_ReviewDate = DateTime.Now.ToShortDateString();
                }
            }
            return PartialView("/Views/Modal/FeasibilityReviewForm.cshtml", feasModel);
        }

        [HttpPost]
        [Route("Feasibility/FeasibilityForm")]
        public IActionResult FeasibilityForm([FromServices]ApplicationDBContext context, FeasibilityReviewModel review)
        {
            string userName = HttpContext.Session.GetString("UserName");
            var feasModel = aContext.tblFeasibilityForms.ToList().Where(t => t.TicketNumber == review.TicketNumber).FirstOrDefault();

            if (feasModel == null)
            {
                context.Add(review);

                context.Add(new RequestHistoryModel
                {
                    TicketNumber = review.TicketNumber,
                    AddDateTime = DateTime.Now,
                    History = "<b><font color='blue'>Completed Feasibility Review Form</font></b>",
                    AddedBy = userName
                });

                var requestEdit = context.tblReviewRequests.FirstOrDefault(r => r.TicketNumber == review.TicketNumber);
                if (review.Feas_Notes != "" && requestEdit != null)
                {
                    var notes = requestEdit.SANotes;
                    requestEdit.SANotes = notes + "<p><b>Feasibility Review notes:</b>&nbsp;" + review.Feas_Notes + "</p>";
                    context.tblReviewRequests.Update(requestEdit);
                }
            }
            else
            {
                feasModel.Feas_ReviewDate = review.Feas_ReviewDate;
                feasModel.Feas_ReviewedBy = review.Feas_ReviewedBy;
                feasModel.Feas_RequirePOC = review.Feas_RequirePOC;
                feasModel.Feas_RequirePOCDetail = review.Feas_RequirePOCDetail;
                feasModel.Feas_IPProduct = review.Feas_IPProduct;
                feasModel.Feas_IPProductDetail = review.Feas_IPProductDetail;
                feasModel.Feas_Gaps = review.Feas_Gaps;
                feasModel.Feas_GapsDetail = review.Feas_GapsDetail;
                feasModel.Feas_OutsideService = review.Feas_OutsideService;
                feasModel.Feas_OutsideServiceDetail = review.Feas_OutsideServiceDetail;
                feasModel.Feas_BestUX = review.Feas_BestUX;
                feasModel.Feas_BestUXDetail = review.Feas_BestUXDetail;
                feasModel.Feas_StandardESig = review.Feas_StandardESig;
                feasModel.Feas_StandardESigDetail = review.Feas_StandardESigDetail;
                feasModel.Feas_AdditionalCosts = review.Feas_AdditionalCosts;
                feasModel.Feas_ItemsNeeded = review.Feas_ItemsNeeded;
                feasModel.Feas_JIRAs = review.Feas_JIRAs;
                feasModel.Feas_Notes = review.Feas_Notes;
                context.tblFeasibilityForms.Update(feasModel);
            }
            context.SaveChanges();

            return Json(new { success = true, responseType = "Feasibility", responseText = "FeasibilityForm Updated", ticketNum = review.TicketNumber });
        }

        [HttpGet]
        [Route("Feasibility/InitiationForm")]
        public async Task<IActionResult> InitiationForm(int ticketNum)
        {
            string userName = HttpContext.Session.GetString("UserName");
            string viewType = HttpContext.Session.GetString("ViewType");

            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();
            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));

            mAssignedSA = requestEdit.Requests.AssignedSA;

            var feasModel = aContext.tblFeasibilityForms.ToList().Where(t => t.TicketNumber == ticketNum).FirstOrDefault();

            if (feasModel == null)
            {
                feasModel = new FeasibilityReviewModel();
                feasModel.TicketNumber = ticketNum;
                feasModel.ProjectName = requestEdit.Requests.ProjectName;
                feasModel.TeamMembers = requestEdit.Requests.TeamMembers;
            }

            if (viewType == "Admin" && feasModel.Init_ReviewDate == null)
            {
                feasModel.Init_ReviewDate = DateTime.Now.ToShortDateString();

                if (feasModel.Init_ReviewedBy == null)
                {
                    feasModel.Init_ReviewedBy = userName;
                }

                if (feasModel.Init_POCResults == null)
                {
                    feasModel.Init_POCResults = feasModel.Feas_RequirePOC;
                }

                if (feasModel.Init_IPProduct == null)
                {
                    feasModel.Init_IPProduct = feasModel.Feas_IPProduct;
                }

                if (feasModel.Init_AdditionalCosts == null)
                {
                    feasModel.Init_AdditionalCosts = feasModel.Feas_AdditionalCosts;
                }

                if (feasModel.Init_JIRAs == null)
                {
                    feasModel.Init_JIRAs = feasModel.Feas_JIRAs;
                }
            }
            return PartialView("/Views/Modal/InitiationReviewForm.cshtml", feasModel);
        }

        [HttpPost]
        [Route("Feasibility/InitiationForm")]
        public IActionResult InitiationForm([FromServices]ApplicationDBContext context, FeasibilityReviewModel review)
        {
            string userName = HttpContext.Session.GetString("UserName");
            var feasModel = aContext.tblFeasibilityForms.ToList().Where(t => t.TicketNumber == review.TicketNumber).FirstOrDefault();

            if (feasModel == null)
            {
                context.Add(review);
            }
            else
            {
                if (feasModel.Init_ReviewDate == "" || feasModel.Init_ReviewDate == null)
                {
                    context.Add(new RequestHistoryModel
                    {
                        TicketNumber = review.TicketNumber,
                        AddDateTime = DateTime.Now,
                        History = "<b><font color='blue'>Completed Initiation Review Form</font></b>",
                        AddedBy = userName
                    });

                    var requestEdit = context.tblReviewRequests.FirstOrDefault(r => r.TicketNumber == review.TicketNumber);
                    if (review.Init_Notes != "" && requestEdit != null)
                    {
                        var notes = requestEdit.SANotes;
                        requestEdit.SANotes = notes + "<p><b>Initiation Review notes:</b>&nbsp;" + review.Init_Notes + "</p>";
                        context.tblReviewRequests.Update(requestEdit);
                    }
                }
                feasModel.Init_ReviewDate = review.Init_ReviewDate;
                feasModel.Init_ReviewedBy = review.Init_ReviewedBy;
                feasModel.Init_Workflow = review.Init_Workflow;
                feasModel.Init_WorkflowDetail = review.Init_WorkflowDetail;
                feasModel.Init_POCResults = review.Init_POCResults;
                feasModel.Init_POCResultsDetail = review.Init_POCResultsDetail;
                feasModel.Init_IPProduct = review.Init_IPProduct;
                feasModel.Init_IPProductDetail = review.Init_IPProductDetail;
                feasModel.Init_Gaps = review.Init_Gaps;
                feasModel.Init_GapsDetail = review.Init_GapsDetail;
                feasModel.Init_IntegrationReq = review.Init_IntegrationReq;
                feasModel.Init_IntegrationReqDetail = review.Init_IntegrationReqDetail;
                feasModel.Init_UX = review.Init_UX;
                feasModel.Init_UXDetail = review.Init_UXDetail;
                feasModel.Init_AdditionalCosts = review.Init_AdditionalCosts;
                feasModel.Init_ItemsNeeded = review.Init_ItemsNeeded;
                feasModel.Init_JIRAs = review.Init_JIRAs;
                feasModel.Init_Notes = review.Init_Notes;
                context.tblFeasibilityForms.Update(feasModel);
            }
            context.SaveChanges();

            return Json(new { success = true, responseType = "Feasibility", responseText = "InitiationForm Updated", ticketNum = review.TicketNumber });
        }

        [HttpGet]
        [Route("Feasibility/InitBuildForm")]
        public async Task<IActionResult> InitBuildForm(int ticketNum)
        {
            string userName = HttpContext.Session.GetString("UserName");
            string viewType = HttpContext.Session.GetString("ViewType");

            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();
            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));

            mAssignedSA = requestEdit.Requests.AssignedSA;

            var feasModel = aContext.tblFeasibilityForms.ToList().Where(t => t.TicketNumber == ticketNum).FirstOrDefault();

            if (feasModel == null)
            {
                feasModel = new FeasibilityReviewModel();
                feasModel.TicketNumber = ticketNum;
                feasModel.ProjectName = requestEdit.Requests.ProjectName;
                feasModel.TeamMembers = requestEdit.Requests.TeamMembers;
            }

            if (viewType == "Admin" && feasModel.InitBuild_ReviewDate == null)
            {
                feasModel.InitBuild_ReviewDate = DateTime.Now.ToShortDateString();

                if (feasModel.InitBuild_ReviewedBy == null)
                {
                    feasModel.InitBuild_ReviewedBy = userName;
                }

                if (feasModel.InitBuild_AdditionalCosts == null)
                {
                    feasModel.InitBuild_AdditionalCosts = feasModel.Init_AdditionalCosts;
                }

                if (feasModel.InitBuild_JIRAs == null)
                {
                    feasModel.InitBuild_JIRAs = feasModel.Init_JIRAs;
                }
            }
            return PartialView("/Views/Modal/InitBuildReviewForm.cshtml", feasModel);
        }

        [HttpPost]
        [Route("Feasibility/InitBuildForm")]
        public IActionResult InitBuildForm([FromServices] ApplicationDBContext context, FeasibilityReviewModel review)
        {
            string userName = HttpContext.Session.GetString("UserName");
            var feasModel = aContext.tblFeasibilityForms.ToList().Where(t => t.TicketNumber == review.TicketNumber).FirstOrDefault();

            if (feasModel == null)
            {
                context.Add(review);
            }
            else
            {
                if (feasModel.InitBuild_ReviewDate == "" || feasModel.InitBuild_ReviewDate == null)
                {
                    context.Add(new RequestHistoryModel
                    {
                        TicketNumber = review.TicketNumber,
                        AddDateTime = DateTime.Now,
                        History = "<b><font color='blue'>Completed Post Initial Build Review Form</font></b>",
                        AddedBy = userName
                    });

                    var requestEdit = context.tblReviewRequests.FirstOrDefault(r => r.TicketNumber == review.TicketNumber);
                    if (review.InitBuild_Notes != "" && requestEdit != null)
                    {
                        var notes = requestEdit.SANotes;
                        requestEdit.SANotes = notes + "<p><b>Post Initial Build Review notes:</b>&nbsp;" + review.InitBuild_Notes + "</p>";
                        context.tblReviewRequests.Update(requestEdit);
                    }
                }
                feasModel.InitBuild_ReviewDate = review.InitBuild_ReviewDate;
                feasModel.InitBuild_ReviewedBy = review.InitBuild_ReviewedBy;
                feasModel.InitBuild_Topology = review.InitBuild_Topology;
                feasModel.InitBuild_TopologyDetail = review.InitBuild_TopologyDetail;
                feasModel.InitBuild_Compliance = review.InitBuild_Compliance;
                feasModel.InitBuild_ComplianceDetail = review.InitBuild_ComplianceDetail;
                feasModel.InitBuild_Gaps = review.InitBuild_Gaps;
                feasModel.InitBuild_GapsDetail = review.InitBuild_GapsDetail;
                feasModel.InitBuild_IntegrationReq = review.InitBuild_IntegrationReq;
                feasModel.InitBuild_IntegrationReqDetail = review.InitBuild_IntegrationReqDetail;
                feasModel.InitBuild_UX = review.InitBuild_UX;
                feasModel.InitBuild_UXDetail = review.InitBuild_UXDetail;
                feasModel.InitBuild_AdditionalCosts = review.InitBuild_AdditionalCosts;
                feasModel.InitBuild_ItemsNeeded = review.InitBuild_ItemsNeeded;
                feasModel.InitBuild_JIRAs = review.InitBuild_JIRAs;
                feasModel.InitBuild_Notes = review.InitBuild_Notes;
                context.tblFeasibilityForms.Update(feasModel);
            }
            context.SaveChanges();

            return Json(new { success = true, responseType = "Feasibility", responseText = "ImplementationForm Updated", ticketNum = review.TicketNumber });
        }

        [HttpGet]
        [Route("Feasibility/ImplementationForm")]
        public async Task<IActionResult> ImplementationForm(int ticketNum)
        {
            string userName = HttpContext.Session.GetString("UserName");
            string viewType = HttpContext.Session.GetString("ViewType");

            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();
            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));

            mAssignedSA = requestEdit.Requests.AssignedSA;

            var feasModel = aContext.tblFeasibilityForms.ToList().Where(t => t.TicketNumber == ticketNum).FirstOrDefault();

            if (feasModel == null)
            {
                feasModel = new FeasibilityReviewModel();
                feasModel.TicketNumber = ticketNum;
                feasModel.ProjectName = requestEdit.Requests.ProjectName;
                feasModel.TeamMembers = requestEdit.Requests.TeamMembers;
            }

            if (viewType == "Admin" && feasModel.Impl_ReviewDate == null)
            {
                feasModel.Impl_ReviewDate = DateTime.Now.ToShortDateString();

                if (feasModel.Impl_ReviewedBy == null)
                {
                    feasModel.Impl_ReviewedBy = userName;
                }

                if (feasModel.Impl_AdditionalCosts == null)
                {
                    feasModel.Impl_AdditionalCosts = feasModel.Init_AdditionalCosts;
                }

                if (feasModel.Impl_JIRAs == null)
                {
                    feasModel.Impl_JIRAs = feasModel.Init_JIRAs;
                }
            }
            return PartialView("/Views/Modal/ImplementationReviewForm.cshtml", feasModel);
        }

        [HttpPost]
        [Route("Feasibility/ImplementationForm")]
        public IActionResult ImplementationForm([FromServices]ApplicationDBContext context, FeasibilityReviewModel review)
        {
            string userName = HttpContext.Session.GetString("UserName");
            var feasModel = aContext.tblFeasibilityForms.ToList().Where(t => t.TicketNumber == review.TicketNumber).FirstOrDefault();

            if (feasModel == null)
            {
                context.Add(review);
            }
            else
            {
                if (feasModel.Impl_ReviewDate == "" || feasModel.Impl_ReviewDate == null)
                {
                    context.Add(new RequestHistoryModel
                    {
                        TicketNumber = review.TicketNumber,
                        AddDateTime = DateTime.Now,
                        History = "<b><font color='blue'>Completed Implementation Review Form</font></b>",
                        AddedBy = userName
                    });

                    var requestEdit = context.tblReviewRequests.FirstOrDefault(r => r.TicketNumber == review.TicketNumber);
                    if (review.Impl_Notes != "" && requestEdit != null)
                    {
                        var notes = requestEdit.SANotes;
                        requestEdit.SANotes = notes + "<p><b>Implementation Review notes:</b>&nbsp;" + review.Impl_Notes + "</p>";
                        context.tblReviewRequests.Update(requestEdit);
                    }
                }
                feasModel.Impl_ReviewDate = review.Impl_ReviewDate;
                feasModel.Impl_ReviewedBy = review.Impl_ReviewedBy;
                feasModel.Impl_Topology = review.Impl_Topology;
                feasModel.Impl_TopologyDetail = review.Impl_TopologyDetail;
                feasModel.Impl_Compliance = review.Impl_Compliance;
                feasModel.Impl_ComplianceDetail = review.Impl_ComplianceDetail;
                feasModel.Impl_Gaps = review.Impl_Gaps;
                feasModel.Impl_GapsDetail = review.Impl_GapsDetail;
                feasModel.Impl_IntegrationReq = review.Impl_IntegrationReq;
                feasModel.Impl_IntegrationReqDetail = review.Impl_IntegrationReqDetail;
                feasModel.Impl_UX = review.Impl_UX;
                feasModel.Impl_UXDetail = review.Impl_UXDetail;
                feasModel.Impl_AdditionalCosts = review.Impl_AdditionalCosts;
                feasModel.Impl_ItemsNeeded = review.Impl_ItemsNeeded;
                feasModel.Impl_JIRAs = review.Impl_JIRAs;
                feasModel.Impl_Notes = review.Impl_Notes;
                context.tblFeasibilityForms.Update(feasModel);
            }
            context.SaveChanges();

            return Json(new { success = true, responseType = "Feasibility", responseText = "ImplementationForm Updated", ticketNum = review.TicketNumber });
        }
    }
}