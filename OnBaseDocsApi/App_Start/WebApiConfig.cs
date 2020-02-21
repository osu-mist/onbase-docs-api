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
                routeTemplate: "api/{controller}/{docId}/{action}",
                defaults: new { docId = RouteParameter.Optional },
                constraints: new { docId = numeric }
            );
            config.Routes.MapHttpRoute(
                name: "ApiNoAction",
                routeTemplate: "api/{controller}/{docId}",
                defaults: new { action = "", docId = RouteParameter.Optional },
                constraints: new { docId = numeric }
            );
            config.Routes.MapHttpRoute(
                name: "Default",
                routeTemplate: "api/{controller}"
            );
        }
    }
}
