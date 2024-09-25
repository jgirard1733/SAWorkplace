using Newtonsoft.Json;
using System.Collections.Generic;

namespace InternalItems.Models
{
#pragma warning disable CS1591
    /// <summary>
    /// Generic Error Object to pass back when an error occurs.
    /// !!!SHOULD NOT NEED TO UPDATE THIS!!!
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// HTTP Status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Unique identifier that is added to log entries in <see cref="SA_OKTA_API.CustomLoggingMiddleware.LoggingMiddleware"/>.
        /// This will make it easy to query entries specific to the error in SPLUNK and the logs.
        /// </summary>
        public string CorrelationID { get; set; }

        /// <summary>
        /// Status Description
        /// </summary>
        public string StatusDescription { get; private set; }

        /// <summary>
        /// Exception message.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<string> Message { get; private set; } = new List<string>();

        public ErrorDetails(int statusCode, string correlationID, string statusDescription)
        {
            this.StatusCode = statusCode;
            this.CorrelationID = correlationID;
            this.StatusDescription = statusDescription;
        }

        public ErrorDetails(int statusCode, string correlationID, string statusDescription, string message)
            : this(statusCode, correlationID, statusDescription)
        {

            this.Message.Add(message);
        }

        public ErrorDetails(int statusCode, string correlationID, string statusDescription, IList<string> message)
    :       this(statusCode, correlationID, statusDescription)
        {
            this.Message = message;
        }

        /// <summary>
        /// This is needed to send the ErrorDetails object as a JSON object and not a string of the object type.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
#pragma warning restore CS1591
}
