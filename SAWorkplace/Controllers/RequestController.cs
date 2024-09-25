using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SAWorkplace.Data;
using SAWorkplace.Models;
using SAWorkplace.Helpers;
using SAWorkplace.Constants;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using LoggerService;
using Newtonsoft.Json.Linq;

namespace SAWorkplace.Controllers
{
    public class RequestController : Controller
    {
        protected ApplicationDBContext aContext;
        private readonly IConfiguration _config;
        protected EmailHelper eHelper;
        protected OKTAHelper oHelper;
        private IHubContext<Services.SignalR_Notifications> _hubContext;
        private static ILoggerManager _logger;
        public RequestController(ApplicationDBContext context, IConfiguration configuration, ILoggerManager logger)
        {
            aContext = context;
            _config = configuration;
            _logger = logger;
            eHelper = new EmailHelper(context, configuration, logger);
            oHelper = new OKTAHelper(context, configuration, logger);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string viewType, string filter = "")
        {
            _logger.LogInfo("Starting Get Request Index.");
  
            HttpContext.Session.SetString("PSArchitect", "False");
            
            var userClaims = HttpContext.User.Claims;
            string oktaGroups, oktaUserName, oktaUserEmail;
            oktaGroups = oktaUserName = oktaUserEmail = string.Empty;
            
            foreach (var claim in userClaims)
            {
                if (claim.Type == "groups" && claim.Issuer == "OpenIdConnect")
                {
                    oktaGroups = claim.Value.ToUpper();
                }
                if (claim.Type == "name")
                {
                    oktaUserName = claim.Value;
                }
                if (claim.Type == "email")
                {
                    oktaUserEmail = claim.Value;
                }
            }
            HttpContext.Session.SetString("UserID", oktaUserEmail.Replace("@ipipeline.com", ""));
            HttpContext.Session.SetString("UserName", oktaUserName);
            HttpContext.Session.SetString("UserEmail", oktaUserEmail);

            string userId = HttpContext.Session.GetString("UserID");

            List<string> groupsArray = JsonConvert.DeserializeObject<List<string>>(oktaGroups);

            foreach (string group in groupsArray)
            {
                if (group == OKTAConst.architect)
                {
                    HttpContext.Session.SetString("PSArchitect", "True");
                    HttpContext.Session.SetString("AdminUser", "True");
                    break;
                }
                else if (group == OKTAConst.engageCoordinator)
                {
                    HttpContext.Session.SetString("ECUser", "True");
                    break;
                }
                else if (group == OKTAConst.sme)
                {
                    HttpContext.Session.SetString("SMEUser", "True");
                    break;
                }
                else
                {
                    HttpContext.Session.SetString("ECUser", _config["Admins:EC"].Contains(userId).ToString());
                    HttpContext.Session.SetString("AdminUser", _config["Admins:Mgr"].Contains(userId).ToString());
                    HttpContext.Session.SetString("DirUser", _config["Admins:Director"].Contains(userId).ToString());
                    HttpContext.Session.SetString("SMEUser", _config["SMEs:Ids"].Contains(userId).ToString());
                    //No break here because other OKTA groups could be included and don't want to exit the for loop too early.
                }
            }

            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string ECUser = HttpContext.Session.GetString("ECUser");
            string AdminUser = HttpContext.Session.GetString("AdminUser");
            string DirUser = HttpContext.Session.GetString("DirUser");
            string SMEUser = HttpContext.Session.GetString("SMEUser");

            if (string.IsNullOrEmpty(viewType))
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("ViewType")))
                {
                    viewType = HttpContext.Session.GetString("ViewType");
                }
                else
                {
                    if (ECUser == "True")
                        viewType = "EC";
                    else if (AdminUser == "True")
                        viewType = "Admin";
                    else if (DirUser == "True")
                        viewType = "Director";
                    else if (SMEUser == "True")
                    {
                        viewType = "SME";
                    }
                    else
                        viewType = "User";
                }
            }
            else
            {
                HttpContext.Session.SetString("FilterType", "");
            }

            HttpContext.Session.SetString("ChartFilter", filter);
            HttpContext.Session.SetString("ViewType", viewType);
            HttpContext.Session.SetString("SearchText", "");

            _logger.LogDebug("PSArchitect = " + PSArchitect + ", userId = " + userId + ", viewType = " + viewType);

            var sortType = "SORT:RequestDate_new";
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("SortType")))
            {
                sortType = HttpContext.Session.GetString("SortType");
            }
            else 
            { 
                HttpContext.Session.SetString("SortType", sortType); 
            }

            DataAccess data = new DataAccess(aContext, HttpContext.Session);

            RequestDisplayModel requestDisplay = new RequestDisplayModel();
            var filterType = "Open";
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("FilterType")))
            {
                filterType = HttpContext.Session.GetString("FilterType");
            }

            string areas = "";
            if (viewType == "SME")
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("SMEAreas")))
                {
                    areas = HttpContext.Session.GetString("SMEAreas");
                }
                else
                {
                    areas = GetSMEAreas(userId);
                    HttpContext.Session.SetString("SMEAreas", areas);
                }
            }
            //filter invoked from Allocation tab - pie chart click
            requestDisplay.Requests = data.LoadRequests(userId, 0, PSArchitect, "", filterType, sortType, filter, areas);

            HttpContext.Session.SetString("FilterType", filterType);

            requestDisplay.Carriers = await data.LoadActiveCarriers();
            requestDisplay.Status = await data.GetStatuses();
            requestDisplay.Progress = await data.GetProgress();
            requestDisplay.RequestTypes = await data.GetRequestTypes();
            requestDisplay.DurationTypes = await data.GetDurationTypes();
            return View("Index", requestDisplay);
        }

        [HttpPost]
        [Route("Request/Requests")]
        public async Task<IActionResult> Requests(string filterType, string sortType = "")
        {
            //_logger.LogDebug("Starting Get Requests: filterType = " + filterType + ", sortType = " + sortType);

            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string viewType = HttpContext.Session.GetString("ViewType");

            if (viewType != "Admin")
            {
                PSArchitect = "False";
                //HttpContext.Session.SetString("FilterType", "All");
            }
            var searchText = "";
            var chartFilter = ""; //will only use this for sort, but not if they click the filter dropdown
            if (!string.IsNullOrEmpty(filterType))
            {
                HttpContext.Session.SetString("FilterType", filterType);
                HttpContext.Session.SetString("SearchText", "");
                HttpContext.Session.SetString("ChartFilter", "");
                if (string.IsNullOrEmpty(sortType) && !string.IsNullOrEmpty(HttpContext.Session.GetString("SortType")))
                    sortType = HttpContext.Session.GetString("SortType");
            }
            else if (!string.IsNullOrEmpty(sortType))
            {
                HttpContext.Session.SetString("SortType", sortType);
                if (string.IsNullOrEmpty(filterType) && !string.IsNullOrEmpty(HttpContext.Session.GetString("FilterType")))
                    filterType = HttpContext.Session.GetString("FilterType");
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("SearchText")))
                    searchText = HttpContext.Session.GetString("SearchText");
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("ChartFilter")))
                    chartFilter = HttpContext.Session.GetString("ChartFilter");
            }
            else
            {
                if (string.IsNullOrEmpty(filterType) && !string.IsNullOrEmpty(HttpContext.Session.GetString("FilterType")))
                    filterType = HttpContext.Session.GetString("FilterType");
                if (string.IsNullOrEmpty(sortType) && !string.IsNullOrEmpty(HttpContext.Session.GetString("SortType")))
                    sortType = HttpContext.Session.GetString("SortType");
            }
            if (!string.IsNullOrEmpty(searchText))
            {
                // this is here in case they click on Sort after searching for a ticket #,  why Sort one ticket?
                var isNumeric = int.TryParse(searchText, out int n);
                if (isNumeric)
                {
                    return PartialView("/Views/Partial/Requests.cshtml");
                }
            }
            string areas = "";
            if (viewType == "SME")
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("SMEAreas")))
                {
                    areas = HttpContext.Session.GetString("SMEAreas");
                }
                else
                {
                    areas = GetSMEAreas(userId);
                    HttpContext.Session.SetString("SMEAreas", areas);
                }
            }

            RequestDisplayModel requestDisplay = new RequestDisplayModel();
            requestDisplay.Requests = data.LoadRequests(userId, 0, PSArchitect, searchText, filterType, sortType, chartFilter, areas);

            requestDisplay.Carriers = await data.LoadActiveCarriers();
            requestDisplay.Status = await data.GetStatuses();
            requestDisplay.Progress = await data.GetProgress();
            requestDisplay.RequestTypes = await data.GetRequestTypes();
            requestDisplay.DurationTypes = await data.GetDurationTypes();
            return PartialView("/Views/Partial/Requests.cshtml", requestDisplay);
        }

        [HttpPost]
        [Route("Request/SearchRequests")]
        public async Task<IActionResult> SearchRequests(string searchText, int searchType)
        {
            //_logger.LogDebug("Starting SearchRequests: searchText = " + searchText + ", searchType = " + searchType.ToString());

            var isNumeric = int.TryParse(searchText, out int n);
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string viewType = HttpContext.Session.GetString("ViewType");

            string sortType = "SORT:RequestDate_new";
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("SortType")))
            {
                sortType = HttpContext.Session.GetString("SortType");
            }
            string filterType = "Open";
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("FilterType")))
            {
                filterType = HttpContext.Session.GetString("FilterType");
            }

            string areas = HttpContext.Session.GetString("SMEAreas");

            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestDisplayModel requestDisplay = new RequestDisplayModel();

            if (searchType == 1)
            {
                HttpContext.Session.SetString("SearchText", searchText);
                if (isNumeric)
                {
                    requestDisplay.Requests = data.LoadRequests(userId, Convert.ToInt32(searchText), PSArchitect, "", "", sortType);
                    HttpContext.Session.SetString("ChartFilter", "");
                }
                else
                {
                    string chartFilter = HttpContext.Session.GetString("ChartFilter");
                    requestDisplay.Requests = data.LoadRequests(userId, 0, PSArchitect, searchText, filterType, sortType, chartFilter, areas);
                }
            }
            else
            {
                HttpContext.Session.SetString("SearchText", "");
                HttpContext.Session.SetString("ChartFilter", "");

                requestDisplay.Requests = data.LoadRequests(userId, 0, PSArchitect, "", filterType, sortType,"", areas);
            }
            requestDisplay.Carriers = await data.LoadActiveCarriers();
            requestDisplay.Status = await data.GetStatuses();
            requestDisplay.Progress = await data.GetProgress();
            requestDisplay.RequestTypes = await data.GetRequestTypes();
            requestDisplay.DurationTypes = await data.GetDurationTypes();
            return PartialView("/Views/Partial/Requests.cshtml", requestDisplay);
        }

        [HttpGet]
        [Route("Request/AddRequest")]
        public async Task<IActionResult> AddRequest()
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(0);
            requestEdit.Carriers = await data.LoadActiveCarriers();
            requestEdit.Status = await data.GetStatuses();
            requestEdit.Progress = await data.GetProgress();
            requestEdit.RequestTypes = await data.GetRequestTypes();
            requestEdit.DurationTypes = await data.GetDurationTypes();
            requestEdit.LoadTestResults = await data.LoadTestResults(0);

            //the load test results get saved immediately, this is in case they change their minds and close the request modal after entering load test results.
            if (requestEdit.LoadTestResults != null && requestEdit.LoadTestResults.Count() > 0)
            {
                foreach (var loadTest in requestEdit.LoadTestResults)
                {
                    var delLoadTest = aContext.tblLoadTestResults.Find(loadTest.LoadTestID);
                    aContext.tblLoadTestResults.Remove(delLoadTest);
                    aContext.SaveChanges();
                }
                requestEdit.LoadTestResults = await data.LoadTestResults(0);
            }
            return PartialView("/Views/Modal/AddRequest.cshtml", requestEdit);
        }

        [HttpPost]
        [Route("Request/AddRequest")]
        public async Task<IActionResult> AddRequest([FromServices]ApplicationDBContext context, RequestEditModel request, IFormFile Questionaire, IFormFile LoadTestResult, [FromServices]IHubContext<Services.SignalR_Notifications> hubContext)
        {
            _hubContext = hubContext;

            //add logic if session is blank
            if (HttpContext.Session.GetString("UserID") == "")
            {
                var member = oHelper.GetUserInfo(HttpContext.User.Claims);
                HttpContext.Session.SetString("UserID", member.Result.UserId);
                HttpContext.Session.SetString("UserName", member.Result.UserName);
                HttpContext.Session.SetString("UserEmail", member.Result.EmailAddress);
            }
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            int matchedId = 0;
            int newTicket = 0;
            do
            {
                Random newRandom = new Random();
                newTicket = newRandom.Next(100000, 999999);
                var matchLookup = context.tblReviewRequests.Where(x => x.TicketNumber == newTicket);
                matchedId = matchLookup.Select(a => a.TicketNumber).FirstOrDefault();
            }
            while (matchedId > 0);

            request.Requests.TicketNumber = newTicket;

            var DocName = "";
            var DocType = "";
            var DocExt = "";
            var DocModifiedBy = "";
            DateTime DocModifiedDate;
            var DocDirectory = "";

            if (Questionaire != null)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "Documents\\" + newTicket);

                System.IO.Directory.CreateDirectory(path + "\\Questionaire");
                var QDirectory = "Documents/" + newTicket + "/Questionaire";
                string QFullName = Path.Combine(QDirectory, Questionaire.FileName);
                using (FileStream stream = new FileStream(QFullName, FileMode.Create))
                {
                    await Questionaire.CopyToAsync(stream);
                }

                DocName = Questionaire.FileName;
                DocType = "Questionaire";
                DocExt = Path.GetExtension(Questionaire.FileName).Replace(".", "");
                DocModifiedBy = userName;
                DocModifiedDate = DateTime.Now;

                context.Add(new DocumentModel
                {
                    DocumentName = DocName,
                    DocumentModifiedByName = DocModifiedBy,
                    DocumentExt = DocExt,
                    DocumentModifiedDate = DocModifiedDate,
                    DocumentPath = DocDirectory,
                    DocumentType = DocType,
                    TicketNumber = newTicket
                });
            }

            if (LoadTestResult != null)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "Documents\\" + newTicket);

                System.IO.Directory.CreateDirectory(path + "\\Web_Service_Load_Test");
                var LTDirectory = "Documents/" + newTicket + "/Web_Service_Load_Test";
                string LTFullName = Path.Combine(LTDirectory, LoadTestResult.FileName);
                using (FileStream stream = new FileStream(LTFullName, FileMode.Create))
                {
                    await LoadTestResult.CopyToAsync(stream);
                }

                DocName = LoadTestResult.FileName;
                DocType = "Web Service Load Test";
                DocExt = Path.GetExtension(LoadTestResult.FileName).Replace(".", "");
                DocModifiedBy = userName;
                DocModifiedDate = DateTime.Now;

                context.Add(new DocumentModel
                {
                    DocumentName = DocName,
                    DocumentModifiedByName = DocModifiedBy,
                    DocumentExt = DocExt,
                    DocumentModifiedDate = DocModifiedDate,
                    DocumentPath = DocDirectory,
                    DocumentType = DocType,
                    TicketNumber = newTicket
                });
            }

            DataAccess data = new DataAccess(context, HttpContext.Session);
            request.LoadTestResults = await data.LoadTestResults(0);

            if (request.LoadTestResults != null)
            {
                foreach (var loadTest in request.LoadTestResults)
                {
                    loadTest.TicketNumber = newTicket;
                    context.tblLoadTestResults.Update(loadTest);
                }
            }

            var myProgressType = 1;
            var myRequestStatus = 1;
            if (request.Requests.RequestType == 5)
            {
                //myProgressType = 5;
                myRequestStatus = 8;
                request.Requests.RequestReviewDate = DateTime.Now;
            }
            if (request.Requests.RequestType == 2)
            {
                request.Requests.RequestReviewDate = request.Requests.TestTime;
            }
            request.Requests.RequestStatus = myRequestStatus;

            var carrier = context.tblCarriers.Where(t => t.CarrierId == request.Requests.CarrierId).FirstOrDefault();
            var myCarrierName = carrier.CarrierName;
            request.Requests.CarrierName = myCarrierName;
            request.Requests.RequestDate = DateTime.Now;
            request.Requests.RequestorEmail = userEmail;
            request.Requests.RequestorName = userName;

            _logger.LogDebug("AddRequest: TicketNumber = " + newTicket.ToString() + ", RequestType = " + request.Requests.RequestType.ToString() + ", CarrierName = " + myCarrierName + ", RequestorName = " + userName);

            context.Add(new RequestModel
            {
                ProjectName = System.Net.WebUtility.HtmlEncode(request.Requests.ProjectName),
                ProgressType = myProgressType,
                AssignedSA = "",
                AssignedSAEmail = "",
                AssignedSAName = "",
                CarrierId = request.Requests.CarrierId,
                CarrierName = myCarrierName,
                FeasibilityReviewDate = request.Requests.FeasibilityReviewDate,
                InitiationReviewDate = request.Requests.InitiationReviewDate,
                InitBuildReviewDate = request.Requests.InitBuildReviewDate,
                ImplementationReviewDate = request.Requests.ImplementationReviewDate,
                RequestReviewDate = request.Requests.RequestReviewDate,
                WebServiceURLs = request.Requests.WebServiceURLs,
                ProjectorCode = request.Requests.ProjectorCode,
                RequestDate = DateTime.Now,
                RequestDesc = request.Requests.RequestDesc,
                Requestor = userId,
                RequestorEmail = userEmail,
                RequestorName = userName,
                RequestStatus = myRequestStatus,
                RequestType = request.Requests.RequestType,
                Requirements = request.Requests.Requirements,
                TestTime = request.Requests.TestTime,
                TFSPath = request.Requests.TFSPath,
                TicketNumber = newTicket,
                Duration1 = request.Requests.Duration1,
                Duration2 = request.Requests.Duration2,
                DurationType1 = request.Requests.DurationType1,
                DurationType2 = request.Requests.DurationType2
            });

            context.SaveChanges();

            eHelper.sendEmails(request, "add", userName, userEmail, "");

            await _hubContext.Clients.All.SendAsync("Review_Added", request.Requests.TicketNumber);

            return Json(new { success = true, responseType = "Request", responseText = "Request Saved" });
        }

        [HttpGet]
        [Route("Request/EditRequest")]
        public async Task<IActionResult> EditRequest(int ticketNum)
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
            requestEdit.LoadTestResults = await data.LoadTestResults(Convert.ToInt32(ticketNum));
            requestEdit.DurationTypes = await data.GetDurationTypes();
            return PartialView("/Views/Modal/EditRequest.cshtml", requestEdit);
        }

        [HttpPost]
        [Route("Request/EditRequest")]
        public async Task<IActionResult> EditRequest([FromServices]ApplicationDBContext context, RequestEditModel request, string newComment, IFormFile Questionaire, IFormFile Review_Result)
        {
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            if (request.Requests.RequestType == 2)
            {
                request.Requests.RequestReviewDate = request.Requests.TestTime;
            }

            try
            {
                var carrier = context.tblCarriers.Where(t => t.CarrierId == request.Requests.CarrierId).FirstOrDefault();
                var myCarrierName = carrier.CarrierName;
                request.Requests.CarrierName = myCarrierName;
            }
            catch { }
            context.tblReviewRequests.Update(request.Requests);
            context.SaveChanges();

            _logger.LogDebug("EditRequest: TicketNumber = " + request.Requests.TicketNumber.ToString() + ", RequestType = " + request.Requests.RequestType.ToString() + ", userName = " + userName);

            if (newComment != "" || Questionaire.Length > 0 || Review_Result.Length > 0)
            {
                var DocName = "";
                var DocType = "";
                var DocExt = "";
                var DocModifiedBy = "";
                DateTime DocModifiedDate;
                var DocDirectory = "";

                if (Questionaire != null)
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "Documents\\" + request.Requests.TicketNumber);

                    System.IO.Directory.CreateDirectory(path + "\\Questionaire");
                    DocDirectory = "Documents/" + request.Requests.TicketNumber + "/Questionaire";
                    string DocFullName = Path.Combine(DocDirectory, Questionaire.FileName);
                    using (FileStream stream = new FileStream(DocFullName, FileMode.Create))
                    {
                        await Questionaire.CopyToAsync(stream);
                    }

                    DocName = Questionaire.FileName;
                    DocType = "Questionaire";
                    DocExt = Path.GetExtension(Questionaire.FileName).Replace(".", "");
                    DocModifiedBy = userName;
                    DocModifiedDate = DateTime.Now;
                    context.Add(new DocumentModel
                    {
                        DocumentName = DocName,
                        DocumentModifiedByName = DocModifiedBy,
                        DocumentExt = DocExt,
                        DocumentModifiedDate = DocModifiedDate,
                        DocumentPath = DocDirectory,
                        DocumentType = DocType,
                        TicketNumber = request.Requests.TicketNumber
                    });
                    context.SaveChanges();
                }

                if (Review_Result != null)
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "Documents\\" + request.Requests.TicketNumber);

                    System.IO.Directory.CreateDirectory(path + "\\Web Service Review");
                    DocDirectory = "Documents/" + request.Requests.TicketNumber + "/Web Service Review";
                    string DocFullName = Path.Combine(DocDirectory, Review_Result.FileName);
                    using (FileStream stream = new FileStream(DocFullName, FileMode.Create))
                    {
                        await Review_Result.CopyToAsync(stream);
                    }

                    DocName = Review_Result.FileName;
                    DocExt = Path.GetExtension(Review_Result.FileName).Replace(".", "");
                    DocType = "Web Service Review";
                    DocModifiedBy = userName;
                    DocModifiedDate = DateTime.Now;
                    context.Add(new DocumentModel
                    {
                        DocumentName = DocName,
                        DocumentModifiedByName = DocModifiedBy,
                        DocumentModifiedDate = DocModifiedDate,
                        DocumentExt = DocExt,
                        DocumentPath = DocDirectory,
                        DocumentType = DocType,
                        TicketNumber = request.Requests.TicketNumber
                    });
                    context.SaveChanges();
                }

                if (newComment != "" && newComment != null)
                {
                    _logger.LogDebug("EditRequest: newComment = " + newComment + ", userName = " + userName);

                    context.Add(new RequestHistoryModel
                    {
                        TicketNumber = request.Requests.TicketNumber,
                        AddDateTime = DateTime.Now,
                        History = newComment,
                        AddedBy = userName
                    });
                    context.SaveChanges();
                }
            }

            eHelper.sendEmails(request, "edit", userName, userEmail, newComment);
            if(request.Requests.RequestType > 99)
            {
                string SME = _config["SMEs:" + request.Requests.RequestType].ToString();

                eHelper.sendSMEmails(request, "edit", userName, userEmail, newComment, SME);

            }

            return Json(new { success = true, responseType = "Request", responseText = "Request Updated" });
            //return PartialView("/Views/Partial/Requests.cshtml", request);
        }

        [HttpPost]
        [Route("Request/DeleteRequest")]
        public async Task<IActionResult> DeleteRequest(int requestID)
        {
            var delDocument = aContext.tblReviewRequests.Find(requestID);
            aContext.tblReviewRequests.Remove(delDocument);
            aContext.SaveChanges();

            _logger.LogDebug("DeleteRequest: TicketNumber = " + requestID.ToString() + ", userName = " + HttpContext.Session.GetString("UserName"));

            return Json(new { success = true, responseType = "Request", responseText = "Request Deleted" });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetSMEAreas(string userId)
        {
            try
            {
                var smeAreas = _config.GetSection("SMEs");
                List<IConfigurationSection> smeChildren = smeAreas.GetChildren().ToList();
                var json = smeChildren.Where(t => t.Value.Contains(userId) && t.Key != "Ids").Select(p => p.Key).ToList();
                var areas = string.Join(",", json.ToArray());
                return areas;
            }
            catch
            {
                return "";
            }
        }
    }
}
