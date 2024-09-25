using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SAWorkplace.Data;
using SAWorkplace.Models;

namespace SAWorkplace.Controllers
{
    public class LoadTestController : Controller
    {
        protected ApplicationDBContext dContext;
        private IHubContext<Services.SignalR_Notifications> _hubContext;

        public LoadTestController(ApplicationDBContext context)
        {
            dContext = context;
        }

        [HttpPost]
        [Route("LoadTest/DeleteLoadTest")]
        public async Task<IActionResult> DeleteLoadTest(int LoadTestID, int ticketNum, int carrierId)
        {
            var delLoadTest = dContext.tblLoadTestResults.Find(LoadTestID);
            dContext.tblLoadTestResults.Remove(delLoadTest);
            dContext.SaveChanges();

            //DataAccess data = new DataAccess(dContext);

            //RequestEditModel requestEdit = new RequestEditModel();
            //requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));
            //requestEdit.Documents = await data.LoadDocuments(ticketNum);
            //requestEdit.History = data.LoadHistory(ticketNum);
            //requestEdit.Carriers = await data.LoadSingleCarrier(carrierId);
            //requestEdit.LoadTestResults = await data.LoadTestResults(Convert.ToInt32(ticketNum));

            //string tempResults = "";

            //foreach(var result in requestEdit.LoadTestResults)
            //{
            //    tempResults = tempResults + "," + result.TestResult;

            //}

            //if(tempResults.Contains("Fail"))
            //{
            //    requestEdit.Requests.RequestStatus = 9;
            //}
            //else if(tempResults.Contains("Off"))
            //{
            //    requestEdit.Requests.RequestStatus = requestEdit.Requests.RequestStatus;
            //}
            //else
            //{
            //    requestEdit.Requests.RequestStatus = 10;
            //}

            //return PartialView("/Views/Modal/SAViews/LoadTest.cshtml", requestEdit);

            DataAccess data = new DataAccess(dContext, HttpContext.Session);
            LoadTestDisplayModel loadTestModel = new LoadTestDisplayModel();
            loadTestModel.LoadTests = await data.LoadTestResults(ticketNum);
            loadTestModel.TicketNumber = ticketNum;

            return PartialView("/Views/Partial/LoadTestResults.cshtml", loadTestModel);

        }

        [HttpGet]
        [Route("LoadTest/AddLoadTest")]
        public IActionResult AddLoadTest(int ticketNum, int carrierId)
        {
            LoadTestDisplayModel loadTestModel = new LoadTestDisplayModel();
            loadTestModel.TicketNumber = ticketNum;
            loadTestModel.CarrierID = carrierId;
            return PartialView("/Views/Modal/AddLoadTest.cshtml", loadTestModel);
        }

        [HttpPost]
        [Route("LoadTest/AddLoadTest")]
        public async Task<IActionResult> AddLoadTest([FromServices]ApplicationDBContext context, int ticketNum, int carrierId, string interfaceName, string reviewLTStatus, DateTime dcTestedTime, double Burst_Min, double Burst_Max, double Burst_Avg, double Variance_Min, double Variance_Max, double Variance_Avg,[FromServices]IHubContext<Services.SignalR_Notifications> hubContext)
        {
            _hubContext = hubContext;
            context.Add(new LoadTestModel
            {
                TicketNumber = ticketNum,
                InterfaceName = interfaceName,
                TestResult = reviewLTStatus,
                DateTested = dcTestedTime,
                Burst_Min = Burst_Min,
                Burst_Max = Burst_Max,
                Burst_Avg = Burst_Avg,
                Variance_Min = Variance_Min,
                Variance_Max = Variance_Max,
                Variance_Avg = Variance_Avg
            });
            context.SaveChanges();

            DataAccess data = new DataAccess(dContext, HttpContext.Session);

            LoadTestDisplayModel loadTestModel = new LoadTestDisplayModel();
            loadTestModel.LoadTests = await data.LoadTestResults(ticketNum);
            loadTestModel.TicketNumber = ticketNum;

            await _hubContext.Clients.All.SendAsync("Review_Added", ticketNum);

            return PartialView("/Views/Partial/LoadTestResults.cshtml", loadTestModel);
        }
    }
}