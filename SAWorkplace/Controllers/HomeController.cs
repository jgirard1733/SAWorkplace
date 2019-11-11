using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SAWorkplace.Data;
using SAWorkplace.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SAWorkplace.Controllers
{
    public class HomeController : Controller
    {
        protected ApplicationDBContext aContext;

        public HomeController(ApplicationDBContext context)
        {
            aContext = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string filterType)
        {
            DataAccess data = new DataAccess(aContext);
            RequestDisplayModel requestDisplay = new RequestDisplayModel();
            HttpContext.Session.SetString("PSArchitect", "false");

            using (var userContext = new PrincipalContext(ContextType.Domain, "CORP"))
            {
                if (HttpContext.User.Identity.Name != null)
                {
                    var requestor = UserPrincipal.FindByIdentity(userContext, HttpContext.User.Identity.Name);
                    if (requestor != null)
                    {
                        HttpContext.Session.SetString("UserID", HttpContext.User.Identity.Name);
                        HttpContext.Session.SetString("PSArchitect", requestor.IsMemberOf(userContext, IdentityType.SamAccountName, "VDI_PSarchitects").ToString());
                        HttpContext.Session.SetString("UserName", string.Format("{0} {1}", requestor.GivenName, requestor.Surname));
                        HttpContext.Session.SetString("UserEmail", requestor.EmailAddress);
                    }
                }
            }

            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");

            if (PSArchitect == "True")
            {
                requestDisplay.Requests = data.LoadRequests("All", 0, PSArchitect);
            }
            else
            {
                requestDisplay.Requests = data.LoadRequests(userId, 0, PSArchitect);
            }

            requestDisplay.Carriers = await data.LoadActiveCarriers();
            requestDisplay.Status = await data.GetStatuses();
            requestDisplay.Alerts = await data.GetAlerts();
            requestDisplay.RequestTypes = await data.GetRequestTypes();
            return View("Index", requestDisplay);
        }

        
        public async Task<IActionResult> Requests(string filterType)
        {
            DataAccess data = new DataAccess(aContext);
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");

            RequestDisplayModel requestDisplay = new RequestDisplayModel();
            if (filterType == "My")
            {
                requestDisplay.Requests = data.LoadRequests(userId,0,PSArchitect);

            }
            else if (filterType == "Unassigned")
            {
                requestDisplay.Requests = data.LoadRequests("Unassigned",0, PSArchitect);
            }
            else
            {
               requestDisplay.Requests = data.LoadRequests("All",0, PSArchitect);

            }

            requestDisplay.Carriers = await data.LoadActiveCarriers();
            requestDisplay.Status = await data.GetStatuses();
            requestDisplay.Alerts = await data.GetAlerts();
            requestDisplay.RequestTypes = await data.GetRequestTypes();
            return PartialView("/Views/Partial/Requests.cshtml", requestDisplay);
        }

        [HttpPost]
        public async Task<IActionResult> SearchRequests(string searchText)
        {
            var isNumeric = int.TryParse(searchText, out int n);
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");

            DataAccess data = new DataAccess(aContext);
            RequestDisplayModel requestDisplay = new RequestDisplayModel();

            if (isNumeric)
            {
                requestDisplay.Requests = data.LoadRequests("", Convert.ToInt32(searchText), PSArchitect);
            }
            else
            {
                requestDisplay.Requests = data.LoadRequests("All", 0, PSArchitect);
            }
            requestDisplay.Carriers = await data.LoadActiveCarriers();
            requestDisplay.Status = await data.GetStatuses();
            requestDisplay.Alerts = await data.GetAlerts();
            requestDisplay.RequestTypes = await data.GetRequestTypes();
            return PartialView("/Views/Partial/Requests.cshtml", requestDisplay);
        }

        [HttpGet]
        public async Task<IActionResult> AddRequest()
        {
            DataAccess data = new DataAccess(aContext);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(0);
            requestEdit.Carriers =  await data.LoadActiveCarriers();
            requestEdit.Status = await data.GetStatuses();
            requestEdit.Alerts = await data.GetAlerts();
            requestEdit.RequestTypes = await data.GetRequestTypes();
            return PartialView("/Views/Modal/AddRequest.cshtml", requestEdit);
        }

        [HttpPost]
        public async Task<IActionResult> AddRequest([FromServices]ApplicationDBContext context, RequestEditModel request, IFormFile Questionaire)
        {
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            int matchedId = 0;
                int newTicket = 0;
                do
                {
                    Random newRandom = new Random();
                    newTicket = newRandom.Next(100000, 999999);
                    matchedId = context.tblReviewRequests.Where(x => Convert.ToInt32(x.TicketNumber) == newTicket).Select(a => a.TicketNumber).FirstOrDefault();
                }
                while (matchedId > 0);

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

            var myAlertType = 1;
            if (request.Requests.RequestType == 5)
            {
                myAlertType = 8;
            }
                context.Add(new RequestModel
                {
                    ProjectName = System.Net.WebUtility.HtmlEncode(request.Requests.ProjectName),
                    AlertType = myAlertType,
                    AssignedSA = "",
                    AssignedSAEmail = "",
                    AssignedSAName = "",
                    CarrierId = request.Requests.CarrierId,
                    FeasibilityReviewDate = request.Requests.FeasibilityReviewDate,
                    InitiationReviewDate = null,
                    ImplementationReviewDate = null,
                    IssueReviewDate = request.Requests.IssueReviewDate,
                    RequestReviewDate = request.Requests.RequestReviewDate,
                    WebServiceURLs = request.Requests.WebServiceURLs,
                    ProjectorCode = request.Requests.ProjectorCode,
                    RequestDate = DateTime.Now,
                    RequestDesc = request.Requests.RequestDesc,
                    Requestor = userId,
                    RequestorEmail = userEmail,
                    RequestorName = userName,
                    RequestStatus = 1,
                    RequestType = request.Requests.RequestType,
                    Requirements = request.Requests.Requirements,
                    SAFinalReview = request.Requests.SAFinalReview,
                    SAInitialReview = request.Requests.SAInitialReview,
                    TestTime = request.Requests.TestTime,
                    TFSPath = request.Requests.TFSPath,
                    TicketNumber = newTicket
                });

            context.SaveChanges();
            return Json(new { success = true, responseType = "Request", responseText = "Request Saved" });
        }

        [HttpGet]
        public async Task<IActionResult> EditRequest(int ticketNum)
        {
            DataAccess data = new DataAccess(aContext);
            RequestEditModel requestEdit = new RequestEditModel();

            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));
            requestEdit.Carriers = await data.LoadActiveCarriers();
            requestEdit.Status = await data.GetStatuses();
            requestEdit.Alerts = await data.GetAlerts();
            requestEdit.RequestTypes = await data.GetRequestTypes();
            requestEdit.Documents = await data.LoadDocuments(ticketNum);
            requestEdit.History = data.LoadHistory(ticketNum);
            return PartialView("/Views/Modal/EditRequest.cshtml", requestEdit);
        }

        [HttpPost]
        public async Task<IActionResult> EditRequest([FromServices]ApplicationDBContext context, RequestEditModel request, string newComment, IFormFile Questionaire, IFormFile Review_Result)
        {
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string userName = HttpContext.Session.GetString("UserName");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            context.tblReviewRequests.Update(request.Requests);
            context.SaveChanges();


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
            return Json(new { success = true, responseType = "Request", responseText = "Request Updated" });
            //return PartialView("/Views/Partial/Requests.cshtml", request);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
