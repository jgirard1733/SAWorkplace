using InternalItems.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace InternalItems.Controllers
{
    /// <summary>
    /// Error controller class that will handle returning our standard error message when accessing a URI that doesnt exist.
    /// !!!SHOULD NOT NEED TO EDIT THIS!!!
    /// </summary>
    [Produces("application/json")]
    [Route("/errors")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorsController : ControllerBase
    {
        private string _correlationID = string.Empty;

        /// <summary>
        /// The action that actually sends the resulting error back.
        /// </summary>
        [Route("{code}")]
        public IActionResult Error(int code)
        {
            _correlationID = NLog.MappedDiagnosticsLogicalContext.Get("correlationid");
            HttpStatusCode parsedCode = (HttpStatusCode)code;
            ErrorDetails error = new ErrorDetails(code, _correlationID, parsedCode.ToString(), "Route does not exist.");

            return new ObjectResult(error);
        }
    }
}