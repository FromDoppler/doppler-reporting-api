using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Hellang.Middleware.ProblemDetails;
using System.Reflection;
using System.IO;
using Doppler.ReportingApi.Infrastructure;

namespace Doppler.ReportingApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DopplerDatabaseSettings>(Configuration.GetSection(nameof(DopplerDatabaseSettings)));
            services.AddCors();
            services.AddProblemDetails();
            services.AddDopplerSecurity();
            services.AddControllers();
            services.AddRepositories();
            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter the token into field as 'Bearer {token}'",
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme },
                            },
                            Array.Empty<string>()
                        }
                    });

                c.SwaggerDoc("v1",
                    new OpenApiInfo
                    {
                        Description = "Doppler Reporting REST API",
                        Title = "Doppler.ReportingApi",
                        Version = "v1"
                    });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                var baseUrl = Configuration.GetValue<string>("BaseURL");
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    c.AddServer(new OpenApiServer() { Url = baseUrl });
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseProblemDetails();

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "Doppler.ReportingApi v1"));

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors(policy => policy
            .SetIsOriginAllowed(isOriginAllowed: _ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
