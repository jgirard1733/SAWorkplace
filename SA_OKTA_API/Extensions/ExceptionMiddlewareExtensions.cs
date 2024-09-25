using SA_OKTA_API.CustomLoggingMiddleware;
using InternalItems.Models;
using LoggerService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace SA_OKTA_API.Extensions
{
    /// <summary>
    /// Global error handler.
    /// See https://code-maze.com/global-error-handling-aspnetcore/ for more details
    /// </summary>
    public static class ExceptionMiddlewareExtensions
    {
        //This is here to prevent a warning about missing an XML comment.
        #pragma warning disable CS1591
        public static void ConfigureExceptionHandler(this IApplicationBuilder app, ILoggerManager logger)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        logger.LogError(contextFeature.Error, "Something went wrong");
                        var _correlationID = NLog.MappedDiagnosticsLogicalContext.Get("correlationid");
                        await context.Response.WriteAsync(new InternalServerError(
                            _correlationID
                        ).ToString());
                    }
                });
            });
        }

        public static void ConfigureCustomExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<LoggingMiddleware>();
        }
    #pragma warning restore CS1591
    }
}