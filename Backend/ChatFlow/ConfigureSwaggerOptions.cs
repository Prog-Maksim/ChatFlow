using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ChatFlow;

public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = $"ChatFlow Api {description.ApiVersion}",
                    Version = description.ApiVersion.ToString(),
                    Description = "ASP.NET Core RESTful Web API для управления мессенджером ChatFlow.",
                    Contact = new OpenApiContact
                    {
                        Name = "Telegram Contact",
                        Url = new Uri("https://t.me/ProgMaksim")
                    }
                });
        }
    }
}