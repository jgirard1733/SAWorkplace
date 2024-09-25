using System.Net;

namespace InternalItems.Models
{
#pragma warning disable CS1591
    /// <summary>
    /// Used when we have a 500 error like in like in <see cref="SA_OKTA_API.CustomLoggingMiddleware.LoggingMiddleware"/> and <see cref="SA_OKTA_API.Extensions.ExceptionMiddlewareExtensions"/>
    /// !!!SHOULD NOT NEED TO UPDATE THIS!!!
    /// </summary>
    public class InternalServerError : ErrorDetails
        {
            public InternalServerError(string correlationID)
                : base(500, correlationID, HttpStatusCode.InternalServerError.ToString())
            {
            }


            public InternalServerError(string correlationID, string message)
                : base(500, correlationID, HttpStatusCode.InternalServerError.ToString(), message)
            {
            }
#pragma warning restore CS1591
    }
}
