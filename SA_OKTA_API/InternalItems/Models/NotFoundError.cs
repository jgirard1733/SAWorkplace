using System.Net;

namespace InternalItems.Models
{
#pragma warning disable CS1591
    /// <summary>
    /// Used when we have a 404 error like in <see cref="SA_OKTA_API.Controllers.SAOktaController.Get(int)"/>
    /// !!!SHOULD NOT NEED TO UPDATE THIS!!!
    /// </summary>
    public class NotFoundError : ErrorDetails
        {
            public NotFoundError(string correlationID)
                : base(404, correlationID, HttpStatusCode.NotFound.ToString())
            {
            }


            public NotFoundError(string correlationID, string message)
                : base(404, correlationID, HttpStatusCode.NotFound.ToString(), message)
            {
            }
    }
#pragma warning restore CS1591
}
