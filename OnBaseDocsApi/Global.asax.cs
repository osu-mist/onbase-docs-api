using System;
using System.IO;
using System.Text.RegularExpressions;
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

            var regex = new Regex(@"\${([^}]+)}", RegexOptions.Compiled);

            // Load api config
            Config = deserializer.Deserialize<ApiConfig>(File.ReadAllText("api-config.yaml"));
            // Replace the environment variables.
            Config.ApiBasePath = ReplaceVar(regex, Config.ApiBasePath);
            Config.ApiHost = ReplaceVar(regex, Config.ApiHost);
            Config.ServiceUrl = ReplaceVar(regex, Config.ServiceUrl);
            Config.DataSource = ReplaceVar(regex, Config.DataSource);
            Config.DocIndexKeyName = ReplaceVar(regex, Config.DocIndexKeyName);
            Config.StagingDocType = ReplaceVar(regex, Config.StagingDocType);
            foreach (var c in Config.Profiles.Values)
            {
                c.Username = ReplaceVar(regex, c.Username);
                c.Password = ReplaceVar(regex, c.Password);
            }
            // Load profiles
            Profiles = new Profiles(Config.Profiles);
        }

        string ReplaceVar(Regex r, string val)
        {
            return r.Replace(val, match =>
            {
                var key = match.Groups[1].Value.Trim();
                return Environment.GetEnvironmentVariable(key);
            });
        }
    }
}
