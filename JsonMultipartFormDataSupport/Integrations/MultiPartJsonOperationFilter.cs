using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Attributes;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Integrations;

internal class MultiPartJsonOperationFilter : IOperationFilter
{
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
                    propertyInfo.Name.Equals(property.Key, StringComparison.OrdinalIgnoreCase)
                    && propertyInfo.GetCustomAttribute<RequiredAttribute>() != null);

                if (isRequired)
                {
                    mediaType.Schema.Required.Add(property.Key.ToCamelCase());
                }

                var jsonPropertyInfo = GetJsonPropertyInfo(descriptor, property.Key);
                if (property.Key.Equals(jsonPropertyInfo?.Name, StringComparison.OrdinalIgnoreCase))
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
                && property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
    }
}