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
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using LoggerService;

namespace SAWorkplace.Controllers
{
    public class AllocationController : Controller
    {
        protected ApplicationDBContext aContext;
        private readonly IConfiguration _config;
        protected EmailHelper eHelper;
        private static ILoggerManager _logger;
        public AllocationController(ApplicationDBContext context, IConfiguration configuration, ILoggerManager logger)
        {
            aContext = context;
            _config = configuration;
            _logger = logger;
            eHelper = new EmailHelper(context, configuration, logger);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string viewType)
        {
            _logger.LogInfo("Starting Get Allocation Index.");
  
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string ECUser = HttpContext.Session.GetString("ECUser");
            string AdminUser = HttpContext.Session.GetString("AdminUser");
            string userId = HttpContext.Session.GetString("UserID");

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
                    else
                        viewType = "User";
                }
            }
            else
            {
                HttpContext.Session.SetString("FilterType", "");
            }

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
            else
            {
                HttpContext.Session.SetString("FilterType", filterType);
            }

            string areas = HttpContext.Session.GetString("SMEAreas");

            requestDisplay.Requests = data.LoadRequests(userId, 0, PSArchitect, "", filterType, sortType, "", areas);

            requestDisplay.Carriers = await data.LoadActiveCarriers();
            requestDisplay.Status = await data.GetStatuses();
            requestDisplay.Progress = await data.GetProgress();
            requestDisplay.RequestTypes = await data.GetRequestTypes();
            requestDisplay.DurationTypes = await data.GetDurationTypes();
            return View("Index", requestDisplay);
        }

        [Route("Allocation/Chart")]
        public async Task<IActionResult> Requests(string filterType)
        {
            //_logger.LogDebug("Starting Get Requests: filterType = " + filterType + ", sortType = " + sortType);

            DataAccess data = new DataAccess(aContext, HttpContext.Session);
            string PSArchitect = HttpContext.Session.GetString("PSArchitect");
            string userId = HttpContext.Session.GetString("UserID");
            string viewType = HttpContext.Session.GetString("ViewType");
            if (viewType != "Admin")
            {
                PSArchitect = "False";
                HttpContext.Session.SetString("FilterType", "");
            }

            if (!string.IsNullOrEmpty(filterType))
            {
                HttpContext.Session.SetString("FilterType", filterType);
                HttpContext.Session.SetString("SearchText", "");
            }
            else
            {
                if (string.IsNullOrEmpty(filterType) && !string.IsNullOrEmpty(HttpContext.Session.GetString("FilterType")))
                    filterType = HttpContext.Session.GetString("FilterType");
            }

            string areas = HttpContext.Session.GetString("SMEAreas");

            RequestDisplayModel requestDisplay = new RequestDisplayModel();
            requestDisplay.Requests = data.LoadRequests(userId, 0, PSArchitect, "", filterType, "", "", areas);

            requestDisplay.Carriers = await data.LoadActiveCarriers();
            requestDisplay.Status = await data.GetStatuses();
            requestDisplay.Progress = await data.GetProgress();
            requestDisplay.RequestTypes = await data.GetRequestTypes();
            requestDisplay.DurationTypes = await data.GetDurationTypes();
            return PartialView("/Views/Partial/Charts.cshtml", requestDisplay);
        }
  
    }
}
