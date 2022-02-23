namespace ChonkyWeb.Extensions
{
    using ChonkyWeb.Helpers;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.OpenApi.Models;
    using System;
    using System.IO;
    using System.Reflection;

    public static class ServiceCollectionExtensions
    {
        public static void AddCustomSwaggerGen(this IServiceCollection services, IHostEnvironment env)
        {
            services.AddSwaggerGen(c =>
                {
                    c.DocumentFilter<HideInternalFilter>();
                    c.DocumentFilter<LowerCaseDocumentFilter>();
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "ChonkMarket v1",
                        Contact = new OpenApiContact
                        {
                            Name = "ctide",
                            Url = new Uri("https://twitter.com/ctide"),
                            Email = "chris@chonk.market"
                        }
                    });
                    c.DescribeAllParametersInCamelCase();
                    c.UseInlineDefinitionsForEnums();
                    if (env.IsDevelopment())
                    {
                        c.SwaggerDoc("internal", new OpenApiInfo
                        {
                            Title = "Internal"
                        });
                    }

                    OpenApiSecurityScheme securityDefinition = new OpenApiSecurityScheme()
                    {
                        Name = "Bearer",
                        BearerFormat = "JWT",
                        Scheme = "bearer",
                        Description = "Specify the authorization token. <br/><br/>Signed in users can generate a token in <a href=\"/user/apis\">user settings</a>.",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                    };
                    c.AddSecurityDefinition("jwt_auth", securityDefinition);

                    // Make sure swagger UI requires a Bearer token specified
                    OpenApiSecurityScheme securityScheme = new OpenApiSecurityScheme()
                    {
                        Reference = new OpenApiReference()
                        {
                            Id = "jwt_auth",
                            Type = ReferenceType.SecurityScheme
                        }
                    };
                    OpenApiSecurityRequirement securityRequirements = new OpenApiSecurityRequirement()
                    {
                        { securityScheme, Array.Empty<string>() },
                    };
                    c.AddSecurityRequirement(securityRequirements);

                    // Set the comments path for the Swagger JSON and UI.
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                });
        }
    }
}
