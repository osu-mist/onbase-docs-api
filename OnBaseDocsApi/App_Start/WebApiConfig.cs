using System.Web.Http;
using System.Net.Http.Formatting;
using Newtonsoft.Json.Serialization;

namespace OnBaseDocsApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            const string numeric = @"^\d+$";

            // Web API configuration and services
            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            // Web API routes
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "ApiWithAction",
                routeTemplate: "api/v1/{controller}/{id}/{action}",
                defaults: new { },
                constraints: new { id = numeric }
            );
            config.Routes.MapHttpRoute(
                name: "ApiNoAction",
                routeTemplate: "api/v1/{controller}/{id}",
                defaults: new { action = "" },
                constraints: new { id = numeric }
            );
            config.Routes.MapHttpRoute(
                name: "Default",
                routeTemplate: "api/v1/{controller}"
            );
            config.Routes.MapHttpRoute(
                name: "HealthCheck",
                routeTemplate: "api/v1",
                defaults: new { controller = "HealthCheck" }
            );
        }
    }
}
