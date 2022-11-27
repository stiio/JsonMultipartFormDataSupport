﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Integrations;

namespace Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Extensions;

public static class MultipartFromDataServicesExtension
{
    public static IServiceCollection AddJsonMultipartFormDataSupport(this IServiceCollection services, JsonSerializerChoice jsonSerializerChoice)
    {
        JsonMultipartFormDataOptions.JsonSerializerChoice = jsonSerializerChoice;
        switch (jsonSerializerChoice)
        {
            case JsonSerializerChoice.SystemText:
                services.AddMvc(options => {
                    var jsonOptions = services.BuildServiceProvider().GetRequiredService<IOptions<JsonOptions>>();
                    options.ModelBinderProviders.Insert(0, new FormDataJsonBinderProvider(jsonOptions));
                });
                break;
            case JsonSerializerChoice.Newtonsoft:
                services.AddMvc(options => {
                    var jsonOptions = services.BuildServiceProvider().GetRequiredService<IOptions<MvcNewtonsoftJsonOptions>>();
                    options.ModelBinderProviders.Insert(0, new FormDataJsonBinderProvider(jsonOptions));
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(jsonSerializerChoice), jsonSerializerChoice, null);
        }



        services.AddSwaggerGen(options => {
            options.OperationFilter<MultiPartJsonOperationFilter>();
        });

        return services;
    }
}