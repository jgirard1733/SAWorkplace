using System.Collections.Generic;
using System.Net;

namespace InternalItems.Models
{
 #pragma warning disable CS1591
    /// <summary>
    /// Used when we have a 400 error like in <see cref="SA_OKTA_API.ActionFilters.ValidationFilterAttribute"/>
    /// !!!SHOULD NOT NEED TO UPDATE THIS!!!
    /// </summary>
    public class BadRequestError : ErrorDetails
        {
            public BadRequestError(string correlationID)
                : base(400, correlationID, HttpStatusCode.BadRequest.ToString())
            {
            }

            public BadRequestError(string correlationID, IList<string> message)
                : base(400, correlationID, HttpStatusCode.BadRequest.ToString(), message)
            {
            }
        }
#pragma warning restore CS1591
}
