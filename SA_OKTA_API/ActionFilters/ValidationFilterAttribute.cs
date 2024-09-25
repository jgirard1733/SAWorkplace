using InternalItems.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace SA_OKTA_API.ActionFilters
{
    /// <summary>
    /// Used to check if the ModelState (object passed to the API) is valid.
    /// For more information look at https://docs.microsoft.com/en-us/aspnet/web-api/overview/formats-and-model-binding/model-validation-in-aspnet-web-api
    /// </summary>
    public class ValidationFilterAttribute : IActionFilter
    {
        /// <summary>
        /// Like name suggests, called when action is being executed.  This is where we check if the ModelState is valid. 
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuting(ActionExecutingContext context)
        {

            if (!context.ModelState.IsValid)
            {
                var _correlationID = NLog.MappedDiagnosticsLogicalContext.Get("correlationid");
                context.Result = new BadRequestObjectResult(new BadRequestError(_correlationID, context.ModelState.Values
                        .SelectMany(state => state.Errors)
                        .Select(error => {
                            if (error.Exception != null)
                            {
                                return error.Exception.Message;
                            }
                            else if (error.ErrorMessage != null)
                            {
                                return error.ErrorMessage;
                            }
                            else
                            {
                                return "Something bad happened.";
                            }
                        }).ToList()
                    ));
            }
        }

        /// <summary>
        /// Not used in this example, but can be used to perform actions after action was executed.
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
