﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Integrations;

public class JsonModelBinder : IModelBinder
{
    private readonly IOptions<JsonOptions>? _jsonOptions;
    private readonly IOptions<MvcNewtonsoftJsonOptions>? _newtonsoftJsonOptions;

    public JsonModelBinder() { }

    public JsonModelBinder(IOptions<JsonOptions> jsonOptions)
    {
        _jsonOptions = jsonOptions;
    }

    public JsonModelBinder(IOptions<MvcNewtonsoftJsonOptions> newtonsoftJsonOptions)
    {
        _newtonsoftJsonOptions = newtonsoftJsonOptions;
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var modelBindingKey = bindingContext.IsTopLevelObject ? bindingContext.BinderModelName! : bindingContext.ModelName;

        // Check the value sent in
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            var file = bindingContext.HttpContext.Request.Form.Files.GetFile(bindingContext.ModelName);
            if (file is null)
            {
                return;
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var text = await reader.ReadToEndAsync();
            valueProviderResult = new ValueProviderResult(text);
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        // Attempt to convert the input value
        var valueAsString = valueProviderResult.FirstValue;
        try
        {
            if (string.IsNullOrEmpty(valueAsString))
            {
                return;
            }

            object? result;
            if (_jsonOptions != null)
            {
                result = this.DeserializeUsingSystemSerializer(bindingContext, valueAsString!);
            }
            else if (_newtonsoftJsonOptions != null)
            {
                result = this.DeserializeUsingJsonNet(bindingContext, valueAsString!);
            }
            else
            {
                result = this.DeserializeUsingSystemSerializer(bindingContext, valueAsString!);
            }

            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (Exception e)
        {
            bindingContext.ModelState.AddModelError(modelBindingKey ?? string.Empty, e.Message);
        }
    }

    private object? DeserializeUsingSystemSerializer(ModelBindingContext bindingContext, string valueAsString)
    {
        return JsonSerializer.Deserialize(valueAsString, bindingContext.ModelType, _jsonOptions?.Value?.JsonSerializerOptions);
    }

    private object? DeserializeUsingJsonNet(ModelBindingContext bindingContext, string valueAsString)
    {
        return JsonConvert.DeserializeObject(valueAsString, bindingContext.ModelType, _newtonsoftJsonOptions?.Value?.SerializerSettings);
    }
}