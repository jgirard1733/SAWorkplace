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
using LoggerService;

namespace SAWorkplace.Controllers
{
    public class SARequestController : Controller
    {
        protected ApplicationDBContext aContext;
        protected EmailHelper eHelper;
        protected string mAssignedSA;
        private readonly IConfiguration _config;
        private static ILoggerManager _logger;

        public SARequestController(ApplicationDBContext context, IConfiguration configuration, ILoggerManager logger)
        {
            aContext = context;
            _config = configuration;
            _logger = logger;
            eHelper = new EmailHelper(context, configuration, logger);
        }

        [HttpGet]
        [Route("Request/CodeReview")]
        public async Task<IActionResult> CodeReview(int ticketNum)
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));
            requestEdit.Documents = await data.LoadDocuments(ticketNum);
            requestEdit.History = data.LoadHistory(ticketNum);
            requestEdit.Carriers = await data.LoadSingleCarrier(requestEdit.Requests.CarrierId);
            requestEdit.RequestTypes = await data.GetRequestTypes();

            //save old status to compare with after they close the ticket
            var status = requestEdit.Requests.RequestStatus;
            HttpContext.Session.SetInt32("CodeRequestStatus", status);

            mAssignedSA = requestEdit.Requests.AssignedSA;

            return PartialView("/Views/Modal/SAViews/CodeReview.cshtml", requestEdit);
        }

        [HttpPost]
        [Route("Request/CodeReview")]
        public async Task<IActionResult> CodeReview([FromServices]ApplicationDBContext context, RequestEditModel request, string newComment, string reviewStatus)
        {
            //add email to requestor when assigned
            bool emailSent = false;
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            if (request.Requests.AssignedSA != null && request.Requests.ProgressType == 1)
            {
                request.Requests.ProgressType = 2;
                request.Requests.RequestStatus = 2;

                eHelper.sendEmails(request, "assigned", userName, userEmail, newComment);
                emailSent = true;
            }

            if (reviewStatus == "Pass")
            {
                if (request.Requests.RequestStatus != 10)
                {
                    request.Requests.RequestStatus = 10;
                    eHelper.sendEmails(request, "SApass", userName, userEmail, newComment);
                    emailSent = true;
                }
                request.Requests.ProgressType = 5;
            }
            else if (reviewStatus == "Fail")
            {
                if (request.Requests.RequestStatus != 9)
                {
                    request.Requests.RequestStatus = 9;
                    eHelper.sendEmails(request, "SAfail", userName, userEmail, newComment);
                    emailSent = true;
                }
                request.Requests.ProgressType = 4;
            }
            else
            {
                if (request.Requests.AssignedSA != null)
                {
                    request.Requests.ProgressType = 2;
                    request.Requests.RequestStatus = 2;
                }
                else
                {
                    request.Requests.ProgressType = 1;
                    request.Requests.RequestStatus = 1;
                }
            }
            if (request.Requests.RequestType == 4)
            {
                //changing to Feasibility review
                request.Requests.RequestStatus = 5;
                if (request.Requests.ProgressType > 1)
                    request.Requests.ProgressType = 6;
                if (string.IsNullOrEmpty(request.Requests.Requirements))
                    request.Requests.Requirements = request.Requests.RequestDesc;
            }

            var oldStatus = HttpContext.Session.GetInt32("CodeRequestStatus");
            if (oldStatus > 0 && oldStatus != request.Requests.RequestStatus)
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

            if (emailSent == false && request.Requests.AssignedSA != null && mAssignedSA != null && request.Requests.AssignedSA != mAssignedSA)
            {
                eHelper.sendEmails(request, "assigned", userName, userEmail, newComment);
                emailSent = true;
            }

            context.tblReviewRequests.Update(request.Requests);
            context.SaveChanges();

            if (newComment != "" && newComment != null)
            {
                context.Add(new RequestHistoryModel
                {
                    TicketNumber = request.Requests.TicketNumber,
                    AddDateTime = DateTime.Now,
                    History = newComment,
                    AddedBy = userName
                });
                context.SaveChanges();
            }

            if (emailSent == false)
            {
                eHelper.sendEmails(request, "SAupdate", userName, userEmail, newComment);
            }
            return Json(new { success = true, responseType = "Request", responseText = "Request Updated" });
        }


        [HttpGet]
        [Route("Request/LoadTest")]
        public async Task<IActionResult> LoadTest(int ticketNum)
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));
            requestEdit.Documents = await data.LoadDocuments(ticketNum);
            requestEdit.History = data.LoadHistory(ticketNum);
            requestEdit.Carriers = await data.LoadSingleCarrier(requestEdit.Requests.CarrierId);
            requestEdit.RequestTypes = await data.GetRequestTypes();
            requestEdit.LoadTestResults = await data.LoadTestResults(Convert.ToInt32(ticketNum));

            //save old status to compare with after they close the ticket
            var status = requestEdit.Requests.RequestStatus;
            HttpContext.Session.SetInt32("LoadRequestStatus", status);

            mAssignedSA = requestEdit.Requests.AssignedSA;

            return PartialView("/Views/Modal/SAViews/LoadTest.cshtml", requestEdit);
        }

        [HttpPost]
        [Route("Request/LoadTest")]
        public async Task<IActionResult> LoadTest([FromServices]ApplicationDBContext context, RequestEditModel request, string newComment, string reviewStatus)
        {
            //add email to requestor when assigned
            bool emailSent = false;
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            if (request.Requests.AssignedSA != null && request.Requests.ProgressType == 1)
            {
                request.Requests.ProgressType = 2;
                request.Requests.RequestStatus = 2;
                eHelper.sendEmails(request, "assigned", userName, userEmail, newComment);
                emailSent = true;
            }

            if (reviewStatus == "Pass")
            {
                if (request.Requests.RequestStatus != 10)
                {
                    request.Requests.RequestStatus = 10;
                    eHelper.sendEmails(request, "SApass", userName, userEmail, newComment);
                    emailSent = true;
                }
                request.Requests.ProgressType = 5;
            }
            else if (reviewStatus == "Fail")
            {
                if (request.Requests.RequestStatus != 9)
                {
                    request.Requests.RequestStatus = 9;
                    eHelper.sendEmails(request, "SAfail", userName, userEmail, newComment);
                    emailSent = true;
                }
                request.Requests.ProgressType = 4;
            }
            else
            {
                if (request.Requests.AssignedSA != null)
                {
                    request.Requests.ProgressType = 2;
                    request.Requests.RequestStatus = 2;
                }
                else
                {
                    request.Requests.ProgressType = 1;
                    request.Requests.RequestStatus = 1;
                }
            }
            if (request.Requests.RequestType == 4)
            {
                //changing to Feasibility review
                request.Requests.RequestStatus = 5;
                if (request.Requests.ProgressType > 1)
                    request.Requests.ProgressType = 6;
                if (string.IsNullOrEmpty(request.Requests.Requirements))
                    request.Requests.Requirements = request.Requests.RequestDesc;
            }

            var oldStatus = HttpContext.Session.GetInt32("LoadRequestStatus");
            if (oldStatus > 0 && oldStatus != request.Requests.RequestStatus)
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

            if (emailSent == false && request.Requests.AssignedSA != null && mAssignedSA != null && request.Requests.AssignedSA != mAssignedSA)
            {
                eHelper.sendEmails(request, "assigned", userName, userEmail, newComment);
                emailSent = true;
            }

            context.tblReviewRequests.Update(request.Requests);
            context.SaveChanges();

            if (newComment != "" && newComment != null)
            {
                context.Add(new RequestHistoryModel
                {
                    TicketNumber = request.Requests.TicketNumber,
                    AddDateTime = DateTime.Now,
                    History = newComment,
                    AddedBy = userName
                });
                context.SaveChanges();
            }

            if (emailSent == false)
            {
                eHelper.sendEmails(request, "SAupdate", userName, userEmail, newComment);
            }

            return Json(new { success = true, responseType = "Request", responseText = "Request Updated" });
        }

        [HttpGet]
        [Route("Request/IssueReview")]
        public async Task<IActionResult> IssueReview(int ticketNum)
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));
            requestEdit.Documents = await data.LoadDocuments(ticketNum);
            requestEdit.History = data.LoadHistory(ticketNum);
            requestEdit.Carriers = await data.LoadSingleCarrier(requestEdit.Requests.CarrierId);
            requestEdit.Status = await data.GetStatuses();
            requestEdit.RequestTypes = await data.GetRequestTypes();

            //save old status to compare with after they close the ticket
            var status = requestEdit.Requests.RequestStatus;
            HttpContext.Session.SetInt32("IssueRequestStatus", status);

            mAssignedSA = requestEdit.Requests.AssignedSA;

            return PartialView("/Views/Modal/SAViews/IssueReview.cshtml", requestEdit);
        }
        [HttpPost]
        [Route("Request/IssueReview")]
        public async Task<IActionResult> IssueReview([FromServices]ApplicationDBContext context, RequestEditModel request, string newComment, string reviewStatus)
        {
            //add email to requestor when assigned
            bool emailSent = false;
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            if (request.Requests.AssignedSA != null && request.Requests.ProgressType == 1)
            {
                request.Requests.ProgressType = 2;
                request.Requests.RequestStatus = 2;
                eHelper.sendEmails(request, "assigned", userName, userEmail, newComment);
                emailSent = true;
            }

            if (request.Requests.RequestStatus == 1)
            {
                request.Requests.ProgressType = 1;
            }
            else if (request.Requests.RequestStatus == 2 || request.Requests.RequestStatus == 8)
            {
                request.Requests.ProgressType = 2;
            }
            else if (request.Requests.RequestStatus == 9)
            {
                request.Requests.ProgressType = 4;
            }
            else if (request.Requests.RequestStatus == 3 || request.Requests.RequestStatus == 4 || request.Requests.RequestStatus == 10 || request.Requests.RequestStatus == 15)
            {
                request.Requests.ProgressType = 5;
            }
            else
            {
                request.Requests.ProgressType = 3;
            }

            if (request.Requests.RequestType == 4)
            {
                //changing to Feasibility review
                request.Requests.RequestStatus = 5;
                if (request.Requests.ProgressType > 1)
                    request.Requests.ProgressType = 6;

                if (string.IsNullOrEmpty(request.Requests.Requirements))
                    request.Requests.Requirements = request.Requests.RequestDesc;
            }

            var oldStatus = HttpContext.Session.GetInt32("IssueRequestStatus");
            if (oldStatus > 0 && oldStatus != request.Requests.RequestStatus)
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

            if (emailSent == false && request.Requests.AssignedSA != null && mAssignedSA != null && request.Requests.AssignedSA != mAssignedSA)
            {
                eHelper.sendEmails(request, "assigned", userName, userEmail, newComment);
                emailSent = true;
            }

            context.tblReviewRequests.Update(request.Requests);
            context.SaveChanges();

            if (newComment != "" && newComment != null)
            {
                context.Add(new RequestHistoryModel
                {
                    TicketNumber = request.Requests.TicketNumber,
                    AddDateTime = DateTime.Now,
                    History = newComment,
                    AddedBy = userName
                });
                context.SaveChanges();
            }

            if (emailSent == false)
            {
                eHelper.sendEmails(request, "SAupdate", userName, userEmail, newComment);
            }
            return Json(new { success = true, responseType = "Request", responseText = "Request Updated" });
        }
        [HttpGet]
        [Route("Request/ProjectConcern")]
        public async Task<IActionResult> ProjectConcern(int ticketNum)
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));
            requestEdit.Documents = await data.LoadDocuments(ticketNum);
            requestEdit.History = data.LoadHistory(ticketNum);
            requestEdit.Carriers = await data.LoadSingleCarrier(requestEdit.Requests.CarrierId);
            requestEdit.Status = await data.GetStatuses();
            requestEdit.RequestTypes = await data.GetRequestTypes();

            //save old status to compare with after they close the ticket
            var status = requestEdit.Requests.RequestStatus;
            HttpContext.Session.SetInt32("ConcernRequestStatus", status);

            mAssignedSA = requestEdit.Requests.AssignedSA;

            return PartialView("/Views/Modal/SAViews/ProjectConcern.cshtml", requestEdit);
        }
        [HttpPost]
        [Route("Request/ProjectConcern")]
        public async Task<IActionResult> ProjectConcern([FromServices]ApplicationDBContext context, RequestEditModel request, string newComment, string reviewStatus)
        {
            //add email to requestor when assigned
            bool emailSent = false;
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            if (request.Requests.AssignedSA != null && request.Requests.ProgressType == 1)
            {
                request.Requests.ProgressType = 2;
                request.Requests.RequestStatus = 2;
                eHelper.sendEmails(request, "assigned", userName, userEmail, newComment);
                emailSent = true;
            }

            if (request.Requests.RequestStatus == 1)
            {
                request.Requests.ProgressType = 1;
            }
            else if (request.Requests.RequestStatus == 2 || request.Requests.RequestStatus == 8)
            {
                request.Requests.ProgressType = 2;
            }
            else if (request.Requests.RequestStatus == 9)
            {
                request.Requests.ProgressType = 4;
            }
            else if (request.Requests.RequestStatus == 3 || request.Requests.RequestStatus == 4 || request.Requests.RequestStatus == 10 || request.Requests.RequestStatus == 15)
            {
                request.Requests.ProgressType = 5;
            }
            else
            {
                request.Requests.ProgressType = 3;
            }

            if (request.Requests.RequestType == 4)
            {
                //changing to Feasibility review
                request.Requests.RequestStatus = 5;
                if (request.Requests.ProgressType > 1)
                    request.Requests.ProgressType = 6;
                if (string.IsNullOrEmpty(request.Requests.Requirements))
                    request.Requests.Requirements = request.Requests.RequestDesc;
            }

            var oldStatus = HttpContext.Session.GetInt32("ConcernRequestStatus");
            if (oldStatus > 0 && oldStatus != request.Requests.RequestStatus)
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

            if (emailSent == false && request.Requests.AssignedSA != null && mAssignedSA != null && request.Requests.AssignedSA != mAssignedSA)
            {
                eHelper.sendEmails(request, "assigned", userName, userEmail, newComment);
                emailSent = true;
            }

            context.tblReviewRequests.Update(request.Requests);
            context.SaveChanges();

            if (newComment != "" && newComment != null)
            {
                context.Add(new RequestHistoryModel
                {
                    TicketNumber = request.Requests.TicketNumber,
                    AddDateTime = DateTime.Now,
                    History = newComment,
                    AddedBy = userName
                });
                context.SaveChanges();
            }

            if (emailSent == false)
            {
                eHelper.sendEmails(request, "SAupdate", userName, userEmail, newComment);
            }

            return Json(new { success = true, responseType = "Request", responseText = "Request Updated" });
        }

    }
}