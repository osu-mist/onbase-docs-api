using System.Web;
using System.Web.Http;
using OnBaseDocsApi.Models;

namespace OnBaseDocsApi
{
    public class Global : HttpApplication
    {
        public static ApiConfig Config { get; private set; }
        public static Profiles Profiles { get; private set; }

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // Load api config
            Config = new ApiConfig("api-config.json");
            // Load profiles
            Profiles = new Profiles("profiles.json");
        }
    }
}
