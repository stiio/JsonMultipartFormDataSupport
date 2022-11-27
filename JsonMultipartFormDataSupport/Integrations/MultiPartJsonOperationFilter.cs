using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Attributes;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Integrations;

public class MultiPartJsonOperationFilter : IOperationFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<JsonOptions> _jsonOptions;
    private readonly IOptions<MvcNewtonsoftJsonOptions> _newtonsoftJsonOption;
    private readonly IOptions<SwaggerGeneratorOptions> _generatorOptions;

    public MultiPartJsonOperationFilter(
        IServiceProvider serviceProvider,
        IOptions<JsonOptions> jsonOptions,
        IOptions<MvcNewtonsoftJsonOptions> newtonsoftJsonOption,
        IOptions<SwaggerGeneratorOptions> generatorOptions)
    {
        _serviceProvider = serviceProvider;
        _jsonOptions = jsonOptions;
        _newtonsoftJsonOption = newtonsoftJsonOption;
        _generatorOptions = generatorOptions;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var descriptors = context.ApiDescription.ActionDescriptor.Parameters.ToList();
        foreach (var descriptor in descriptors)
        {
            if (!HasJsonProperties(descriptor))
            {
                continue;
            }
            
            var mediaType = operation.RequestBody.Content.First().Value;

            mediaType.Schema.Required.Clear();

            // Group all exploded properties.
            var groupedProperties = mediaType.Schema.Properties
                .GroupBy(pair => pair.Key.Split('.')[0]);

            var schemaProperties = new Dictionary<string, OpenApiSchema>();

            foreach (var property in groupedProperties)
            {
                var isRequired = descriptor.ParameterType.GetProperties().Any(propertyInfo =>
                    propertyInfo.Name == property.Key
                    && propertyInfo.GetCustomAttribute<RequiredAttribute>() != null);

                if (isRequired)
                {
                    mediaType.Schema.Required.Add(property.Key.ToCamelCase());
                }

                var jsonPropertyInfo = GetJsonPropertyInfo(descriptor, property.Key);
                if (property.Key == jsonPropertyInfo?.Name)
                {
                    AddEncoding(mediaType, jsonPropertyInfo);

                    var openApiSchema = GetSchema(context, jsonPropertyInfo);
                    schemaProperties.Add(property.Key.ToCamelCase(), openApiSchema);
                }
                else
                {
                    schemaProperties.Add(property.Key.ToCamelCase(), property.First().Value);
                }
            }

            // Override schema properties
            mediaType.Schema.Properties = schemaProperties;
        }
    }

    /// <summary>
    /// Generate schema for propertyInfo
    /// </summary>
    /// <returns></returns>
    private OpenApiSchema GetSchema(OperationFilterContext context, PropertyInfo propertyInfo)
    {
        var present = context.SchemaRepository.TryLookupByType(propertyInfo.PropertyType, out var openApiSchema);
        if (present)
        {
            return openApiSchema;
        }

        openApiSchema = context.SchemaGenerator.GenerateSchema(propertyInfo.PropertyType, context.SchemaRepository);
        AddDescription(openApiSchema, openApiSchema.Title);
        this.AddExample(propertyInfo, openApiSchema);

        return openApiSchema;
    }

    private static void AddDescription(OpenApiSchema openApiSchema, string schemaDisplayName)
    {
        openApiSchema.Description += $"\n See {schemaDisplayName} model.";
    }

    private static void AddEncoding(OpenApiMediaType mediaType, PropertyInfo propertyInfo)
    {
        mediaType.Encoding = mediaType.Encoding
            .Where(pair => !pair.Key.ToLower().Contains(propertyInfo.Name.ToLower()))
            .ToDictionary(pair => pair.Key.ToCamelCase(), pair => pair.Value);

        mediaType.Encoding.Add(propertyInfo.Name.ToCamelCase(), new OpenApiEncoding()
        {
            Style = ParameterStyle.Form,
            ContentType = "application/json",
        });
    }

    private void AddExample(PropertyInfo propertyInfo, OpenApiSchema openApiSchema)
    {
        var example = GetExampleFor(propertyInfo.PropertyType);

        // Example do not exist. Use default.
        if (example == null) return;

        var json = JsonMultipartFormDataOptions.JsonSerializerChoice switch
        {
            JsonSerializerChoice.SystemText => JsonSerializer.Serialize(example,
                _jsonOptions.Value.JsonSerializerOptions),
            JsonSerializerChoice.Newtonsoft => JsonConvert.SerializeObject(example,
                _newtonsoftJsonOption.Value.SerializerSettings),
            _ => JsonSerializer.Serialize(example)
        };

        openApiSchema.Example = new OpenApiString(json);
    }

    private object? GetExampleFor(Type parameterType)
    {
        var makeGenericType = typeof(IExamplesProvider<>).MakeGenericType(parameterType);
        var method = makeGenericType.GetMethod("GetExamples");
        var exampleProvider = _serviceProvider.GetService(makeGenericType);

        // Example do not exist. Use default.
        if (exampleProvider == null)
        {
            return null;
        }
        
        var example = method?.Invoke(exampleProvider, null);
        return example;
    }

    private static bool HasJsonProperties(ParameterDescriptor descriptor)
    {
        return descriptor.ParameterType.GetProperties()
            .Any(property => property.GetCustomAttribute<FromJsonAttribute>() != null);
    }

    private static PropertyInfo? GetJsonPropertyInfo(ParameterDescriptor descriptor, string propertyName)
    {
        return descriptor.ParameterType.GetProperties()
            .SingleOrDefault(property =>
                property.GetCustomAttribute<FromJsonAttribute>() != null
                && property.Name == propertyName);
    }
}