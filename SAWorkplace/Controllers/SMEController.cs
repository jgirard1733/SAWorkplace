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


namespace SAWorkplace.Controllers
{
    public class SMEController : Controller
    {
        protected ApplicationDBContext aContext;
        private readonly IConfiguration _config;
        protected EmailHelper eHelper;
        protected OKTAHelper oHelper;
        private IHubContext<Services.SignalR_Notifications> _hubContext;
        private static ILoggerManager _logger;
        public SMEController(ApplicationDBContext context, IConfiguration configuration, ILoggerManager logger)
        {
            aContext = context;
            _config = configuration;
            _logger = logger;
            eHelper = new EmailHelper(context, configuration, logger);
            oHelper = new OKTAHelper(context, configuration, logger);
        }

        public async Task<IActionResult> Index(string viewType, string filter = "")
        {
            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            RequestEditModel requestSMEEdit = new RequestEditModel();

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


           

            requestSMEEdit.Requests = data.LoadRequest(0);
            requestSMEEdit.Carriers = await data.LoadActiveCarriers();
            requestSMEEdit.DurationTypes = await data.GetDurationTypes();
            requestSMEEdit.RequestTypes = await data.GetRequestTypes();

            return View("/Views/Request/AddSMEReview.cshtml", requestSMEEdit);
        }

        [HttpPost]
        [Route("SME/AddSMERequest")]
        public async Task<IActionResult> AddSMERequest([FromServices]ApplicationDBContext context, [FromForm]RequestEditModel request, [FromServices]IHubContext<Services.SignalR_Notifications> hubContext)
        {
            _hubContext = hubContext;
            //add logic if session is blank
            if (HttpContext.Session.GetString("UserID") == "" || HttpContext.Session.GetString("UserName") == "")
            {
                _logger.LogDebug("Add SME Request: User is blank! TicketNumber = " + request.Requests.TicketNumber + ", ");

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
                var matchLookup = aContext.tblReviewRequests.Where(x => x.TicketNumber == newTicket);
                matchedId = matchLookup.Select(a => a.TicketNumber).FirstOrDefault();
            }
            while (matchedId > 0);

            var carrier = context.tblCarriers.Where(t => t.CarrierId == request.Requests.CarrierId).FirstOrDefault();
            var myCarrierName = carrier.CarrierName;
            request.Requests.CarrierName = myCarrierName;
            request.Requests.RequestReviewDate = request.Requests.RequestReviewDate;
            request.Requests.RequestType = request.Requests.RequestType;
            request.Requests.RequestStatus = 1;
            request.Requests.RequestDate = DateTime.Now;
            request.Requests.RequestorEmail = userEmail;
            request.Requests.RequestorName = userName;
            //request.Requests.Duration1 = request.Requests.Duration1;
            //request.Requests.Duration2 = request.Requests.Duration2;
            //request.Requests.DurationType1 = request.Requests.DurationType1;
            //request.Requests.DurationType2 = request.Requests.DurationType2;
            request.Requests.TicketNumber = newTicket;


            _logger.LogDebug("Add SME Request: TicketNumber = " + request.Requests.TicketNumber + ", CarrierName = " + myCarrierName + ", RequestorName = " + userName);

            context.Add(new RequestModel
            {
                ProjectName = System.Net.WebUtility.HtmlEncode(request.Requests.ProjectName),
                ProgressType = 1,
                AssignedSA = "",
                AssignedSAEmail = "",
                AssignedSAName = "",
                CarrierId = request.Requests.CarrierId,
                CarrierName = myCarrierName,
                RequestReviewDate = request.Requests.RequestReviewDate,
                ProjectorCode = request.Requests.ProjectorCode,
                RequestDate = DateTime.Now,
                RequestDesc = request.Requests.RequestDesc,
                Requestor = userId,
                RequestorEmail = userEmail,
                RequestorName = userName,
                RequestStatus = 1,
                RequestType = request.Requests.RequestType,
                TicketNumber = newTicket,            
                Duration1 = request.Requests.Duration1,
                Duration2 = request.Requests.Duration2,
                DurationType1 = request.Requests.DurationType1,
                DurationType2 = request.Requests.DurationType2,
            });

            //var updatedCarrier = context.tblCarriers.FirstOrDefault(updateableCarrier => updateableCarrier.CarrierId == request.Requests.CarrierId);

            context.SaveChanges();

            string SME = _config["SMEs:" + request.Requests.RequestType].ToString();

            eHelper.sendSMEmails(request, "add", userName, userEmail, "", SME);

            await _hubContext.Clients.All.SendAsync("Review_Added", request.Requests.TicketNumber);

            return Json(new { success = true, responseType = "Request", responseText = "Request Saved" });
        }
    }
}