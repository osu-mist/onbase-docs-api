using System.IO;
using System.Web;
using System.Web.Http;
using OnBaseDocsApi.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OnBaseDocsApi
{
    public class Global : HttpApplication
    {
        public static ApiConfig Config { get; private set; }
        public static Profiles Profiles { get; private set; }

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            // Load api config
            Config = deserializer.Deserialize<ApiConfig>(File.ReadAllText("api-config.yaml"));
            // Load profiles
            Profiles = new Profiles(deserializer, "profiles.yaml");
        }
    }
}
