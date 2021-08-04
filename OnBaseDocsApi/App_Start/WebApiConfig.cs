using System.Web.Http;
using System.Net.Http.Formatting;
using Newtonsoft.Json.Serialization;

namespace OnBaseDocsApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            // Web API routes
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "ApiSecureList",
                routeTemplate: "api/v1/onbase-docs/Secure",
                defaults: new { controller = "onbasedocs", action = "Secure" }
            );
            config.Routes.MapHttpRoute(
                name: "ApiSecureGet",
                routeTemplate: "api/v1/onbase-docs/Secure/{id}",
                defaults: new { controller = "onbasedocs", action = "Secure" }
            );
            config.Routes.MapHttpRoute(
                name: "ApiSecureGetFile",
                routeTemplate: "api/v1/onbase-docs/Secure/{id}/File",
                defaults: new { controller = "onbasedocs", action = "SecureFile" }
            );
            config.Routes.MapHttpRoute(
                name: "ApiWithAction",
                routeTemplate: "api/v1/onbase-docs/{id}/{action}",
                defaults: new { controller = "onbasedocs" }
            );
            config.Routes.MapHttpRoute(
                name: "ApiNoAction",
                routeTemplate: "api/v1/onbase-docs/{id}",
                defaults: new { controller = "onbasedocs", action = "" }
            );
            config.Routes.MapHttpRoute(
                name: "Default",
                routeTemplate: "api/v1/onbase-docs",
                defaults: new { controller = "onbasedocs", action = "" }
            );
            config.Routes.MapHttpRoute(
                name: "HealthCheck",
                routeTemplate: "api/v1",
                defaults: new { controller = "HealthCheck" }
            );
        }
    }
}
