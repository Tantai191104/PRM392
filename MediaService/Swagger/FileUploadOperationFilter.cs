using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MediaService.Swagger
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParams = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile)
                         || p.ParameterType == typeof(IFormFileCollection)
                         || (p.ParameterType.IsGenericType && p.ParameterType.GetGenericArguments().Any(t => t == typeof(IFormFile))))
                .ToList();

            if (!fileParams.Any())
                return;

            // Build multipart/form-data request body schema
            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = fileParams.ToDictionary(p => p.Name ?? "file", p => new OpenApiSchema { Type = "string", Format = "binary" } as OpenApiSchema)
            };

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType { Schema = schema }
                }
            };

            // Remove corresponding parameters (they're represented in the request body now)
            if (operation.Parameters != null)
            {
                foreach (var p in fileParams)
                {
                    var toRemove = operation.Parameters.FirstOrDefault(x => string.Equals(x.Name, p.Name, StringComparison.OrdinalIgnoreCase));
                    if (toRemove != null)
                        operation.Parameters.Remove(toRemove);
                }
            }
        }
    }
}
