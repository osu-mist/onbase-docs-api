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

<<<<<<< HEAD
            // Load api config
            Config = new ApiConfig("api-config.json");
            // Load profiles
            Profiles = new Profiles("profiles.json");
=======
            // Load the api config.
            LoadConfig();
        }

        public static Credentials GetProfile(string profileName)
        {
            if (!Profiles.ContainsKey(profileName))
                return null;
            return Profiles[profileName];
        }

        void LoadConfig()
        {
            var config = JObject.Parse(File.ReadAllText("api-config.json"));

            Config = new ApiConfig
            {
                ApiBasePath = config.Value<string>("apiBasePath"),
                ApiHost = config.Value<string>("apiHost"),
                ServiceUrl = config.Value<string>("serviceUrl"),
                DataSource = config.Value<string>("dataSource"),
                DocIndexKeyName = config.Value<string>("docIndexKeyName"),
                StagingDocType = config.Value<string>("stagingDocType"),
            };
            Profiles = new Dictionary<string, Credentials>();
            foreach (var prop in (config["profiles"] as JObject).Properties())
            {
                Profiles[prop.Name] = new Credentials
                {
                    Username = prop.Value.Value<string>("username"),
                    Password = prop.Value.Value<string>("password")
                };
            }
>>>>>>> 59818c9c39e6e7719cdbe93cb6fb631b5acbe53e
        }
    }
}

