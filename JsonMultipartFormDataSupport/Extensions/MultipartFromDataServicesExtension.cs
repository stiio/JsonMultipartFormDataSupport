using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Integrations;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Options;

namespace Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Extensions;

public static class MultipartFromDataServicesExtension
{
    public static IServiceCollection AddJsonMultipartFormDataSupport(this IServiceCollection services, JsonSerializerChoice jsonSerializerChoice)
    {
        JsonMultipartFormDataOptions.JsonSerializerChoice = jsonSerializerChoice;

        services.ConfigureOptions<ConfigureMvcOptions>();
        services.ConfigureOptions<ConfigureSwaggerGenOptions>();

        return services;
    }
}