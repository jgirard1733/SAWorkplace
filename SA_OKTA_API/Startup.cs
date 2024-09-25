using SA_OKTA_API.ActionFilters;
using SA_OKTA_API.Contracts;
using SA_OKTA_API.Extensions;
using SA_OKTA_API.Repositories;
using InternalItems.Models;
using LoggerService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NLog;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using SAWorkplace.Data;
using SAWorkplace.Helpers;
using Microsoft.EntityFrameworkCore;

namespace SA_OKTA_API
{
//This is here to prevent a warning about missing an XML comment.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            LogManager.LoadConfiguration(string.Concat(Directory.GetCurrentDirectory(), "/nlog.config"));
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDBContext>(options =>
            options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<ISAOktaRepository, SAOktaRepository>();
            services.AddScoped<ValidationFilterAttribute>();
            services.AddOptions();
            services.AddSingleton(Configuration);
            services.AddSingleton<ILoggerManager, LoggerManager>();

            // Register the Swagger generator, defining 1 or more Swagger documents
            // Document settings can be changed in the appSettings.json file under ApiInformation
            var settings = Configuration.GetSection("ApiInformation").Get<ApiInformation>();
            string json = JsonSerializer.Serialize(settings); ;
            OpenApiInfo swaggerInfo = new OpenApiInfo();
            swaggerInfo = JsonSerializer.Deserialize<OpenApiInfo>(json);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", swaggerInfo);
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerManager logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Handles exceptions and generates a custom response body
                app.UseExceptionHandler("/errors/500");

                // Handles non-success status codes with empty body
                app.UseStatusCodePagesWithReExecute("/errors/{0}");
                app.UseHsts();
            }

            app.ConfigureCustomExceptionMiddleware();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("./swagger/v1/swagger.json", "SA OKTA API V1");
                c.RoutePrefix = string.Empty;
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
