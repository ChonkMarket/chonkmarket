namespace ChonkyWeb.Helpers
{
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System.Linq;

    public class HideInternalFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (context.DocumentName == "v1")
            {
                var internalRoutes = swaggerDoc.Paths
                    .Where(x => !x.Key.ToLower().Contains("v1"))
                    .ToList();
                internalRoutes.ForEach(x => { swaggerDoc.Paths.Remove(x.Key); });
            }
        }
    }

    public class LowerCaseDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = swaggerDoc.Paths.ToDictionary(entry => LowercaseEverythingButParameters(entry.Key),
                entry => entry.Value);
            swaggerDoc.Paths = new OpenApiPaths();
            foreach (var (key, value) in paths)
            {
                swaggerDoc.Paths.Add(key, value);
            }
        }

        private static string LowercaseEverythingButParameters(string key) => string.Join('/', key.Split('/').Select(x => x.Contains("{") ? x : x.ToLower()));
    }
}
