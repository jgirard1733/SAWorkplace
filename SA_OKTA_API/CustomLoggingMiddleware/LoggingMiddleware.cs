using InternalItems.Models;
using LoggerService;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;
using SA_OKTA_API.Helpers;
using Microsoft.Extensions.Configuration;

namespace SA_OKTA_API.CustomLoggingMiddleware
{
    /// <summary>
    /// Provides a Correlation ID and custom exception handler.
    /// Uses <see cref="LoggerService"/> for the log manager.
    /// </summary>
    public class LoggingMiddleware
    {
        //This is here to prevent a warnng about missing an XML comment.
        #pragma warning disable CS1591
        private readonly RequestDelegate _next;
        private readonly ILoggerManager _logger;
        private string _guid;
        protected EmailHelper _eHelper;
        public LoggingMiddleware(RequestDelegate next, IConfiguration configuration, ILoggerManager logger)
        {
            _logger = logger;
            _next = next;
            _eHelper = new EmailHelper(configuration, logger);
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            _guid = Guid.NewGuid().ToString();
            NLog.MappedDiagnosticsLogicalContext.Set("correlationid", _guid);
            try
            {
                _logger.LogInfo($"About to start {httpContext.Request.Method} {httpContext.Request.Path} request");
                await _next(httpContext);
                _logger.LogInfo($"Request completed with status code: {httpContext.Response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong");
                _eHelper.sendEmails(ex.ToString());
                await HandleExceptionAsync(httpContext, ex, _guid);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, string guid)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            return context.Response.WriteAsync(new InternalServerError(
                guid,
                exception.ToString()
            ).ToString());
        }

        #pragma warning restore CS1591
    }
}