using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Integrations;

namespace Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Options;

internal class ConfigureMvcOptions : IConfigureOptions<MvcOptions>
{
    private readonly IOptions<JsonOptions> _systemTextJsonOptions;
    private readonly IOptions<MvcNewtonsoftJsonOptions> _newtonsoftJsonOptions;

    public ConfigureMvcOptions(
        IOptions<JsonOptions> systemTextJsonOptions,
        IOptions<MvcNewtonsoftJsonOptions> newtonsoftJsonOptions)
    {
        _systemTextJsonOptions = systemTextJsonOptions;
        _newtonsoftJsonOptions = newtonsoftJsonOptions;
    }

    public void Configure(MvcOptions options)
    {
        switch (JsonMultipartFormDataOptions.JsonSerializerChoice)
        {
            case JsonSerializerChoice.SystemText:
            {
                options.ModelBinderProviders.Insert(0, new FormDataJsonBinderProvider(_systemTextJsonOptions));
                break;
            }

            case JsonSerializerChoice.Newtonsoft:
            {
                options.ModelBinderProviders.Insert(0, new FormDataJsonBinderProvider(_newtonsoftJsonOptions));
                break;
            }
        }
    }
}