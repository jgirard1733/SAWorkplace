using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAWorkplace.Data;
using SAWorkplace.Models;

namespace SAWorkplace.Controllers
{
    public class DocumentController : Controller
    {
        protected ApplicationDBContext dContext;
        public DocumentController(ApplicationDBContext context)
        {
            dContext = context;
        }

        [HttpGet]
        [Route("Document/DeleteDocument")]
        public async Task<IActionResult> DeleteDocument(int ticketNum, int requestType)
        {
            DataAccess data = new DataAccess(dContext, HttpContext.Session);
            RequestEditModel requestEdit = new RequestEditModel();
            requestEdit.Requests = data.LoadRequest(Convert.ToInt32(ticketNum));

            var requestStatus = requestEdit.Requests.RequestStatus;
            //don't delete if the request is closed
            if (requestStatus == 3 || requestStatus == 4 || requestStatus == 10 || requestStatus == 15)
            {
                return Json(new { success = false, responseType = "Document", responseText = "Document cannot be deleted" });
            }

            return Json(new { success = true, responseType = "Document", responseText = "Document can be deleted" });
        }
        [HttpPost]
        [Route("Document/DeleteDocument")]
        public async Task<IActionResult> DeleteDocument(int documentID, int ticketNum, int requestType)
        {
            var delDocument = dContext.tblDocuments.Find(documentID);
            dContext.tblDocuments.Remove(delDocument);
            dContext.SaveChanges();

            DataAccess data = new DataAccess(dContext, HttpContext.Session);
            DocumentDisplayModel documentModel = new DocumentDisplayModel();
            documentModel.Documents = await data.LoadDocuments(ticketNum);
            documentModel.TicketNumber = ticketNum;
            documentModel.RequestType = requestType;

            return PartialView("/Views/Partial/Documents.cshtml",documentModel); 

            //return Json(new { success = true, responseType = "Document", responseText = "Document Deleted" });
        }

        [HttpGet]
        [Route("Document/AddDocument")]
        public IActionResult AddDocument(int ticketNum)
        {

            return PartialView("/Views/Modal/AddDocument.cshtml", ticketNum);
        }

        [HttpPost]
        [Route("Document/AddDocument")]
        public async Task<IActionResult> AddDocument([FromServices]ApplicationDBContext context, int ticketNum, int requestType, string documentType, IFormFile Document)
        {
            var DocName = "";
            var DocType = "";
            var DocExt = "";
            var DocModifiedBy = "";
            DateTime DocModifiedDate;
            var DocDirectory = "";
            string userName = HttpContext.Session.GetString("UserName");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Documents\\" + ticketNum);

            System.IO.Directory.CreateDirectory(path + "\\" + documentType.Replace(" ", "_"));
            DocDirectory = "Documents/" + ticketNum + "/" + documentType.Replace(" ", "_");
            string SavedDocName = Document.FileName.Replace(" ", "_");
            string QFullName = Path.Combine(DocDirectory, SavedDocName);
            using (FileStream stream = new FileStream(QFullName, FileMode.Create))
            {
                await Document.CopyToAsync(stream);
            }

            DocName = Document.FileName.Replace(" ", "_"); ;
            DocType = documentType;
            DocExt = Path.GetExtension(Document.FileName).Replace(".", "");
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
                TicketNumber = ticketNum
            });
            context.SaveChanges();

            DataAccess data = new DataAccess(dContext, HttpContext.Session);
            DocumentDisplayModel documentModel = new DocumentDisplayModel();
            documentModel.Documents = await data.LoadDocuments(ticketNum);
            documentModel.TicketNumber = ticketNum;
            documentModel.RequestType = requestType;

            return PartialView("/Views/Partial/Documents.cshtml", documentModel);
        }
    }
}