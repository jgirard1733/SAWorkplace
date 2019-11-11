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

        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int documentID, int ticketNum, int requestType)
        {
            var delDocument = dContext.tblDocuments.Find(documentID);
            dContext.tblDocuments.Remove(delDocument);
            dContext.SaveChanges();

            DataAccess data = new DataAccess(dContext);
            DocumentDisplayModel documentModel = new DocumentDisplayModel();
            documentModel.Documents = await data.LoadDocuments(ticketNum);
            documentModel.TicketNumber = ticketNum;
            documentModel.RequestType = requestType;

            return PartialView("/Views/Partial/Documents.cshtml",documentModel); 

            //return Json(new { success = true, responseType = "Document", responseText = "Document Deleted" });
        }

        [HttpGet]
        public IActionResult AddDocument(int ticketNum)
        {
            
            return PartialView("/Views/Modal/AddDocument.cshtml", ticketNum);
        }

        [HttpPost]
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

            System.IO.Directory.CreateDirectory(path + "\\" + documentType);
            DocDirectory = "Documents/" + ticketNum + "/" + documentType;
            string QFullName = Path.Combine(DocDirectory, Document.FileName);
            using (FileStream stream = new FileStream(QFullName, FileMode.Create))
            {
                await Document.CopyToAsync(stream);
            }

            DocName = Document.FileName;
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

            DataAccess data = new DataAccess(dContext);
            DocumentDisplayModel documentModel = new DocumentDisplayModel();
            documentModel.Documents = await data.LoadDocuments(ticketNum);
            documentModel.TicketNumber = ticketNum;
            documentModel.RequestType = requestType;

            return PartialView("/Views/Partial/Documents.cshtml", documentModel);
        }
    }
}