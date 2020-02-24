﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using OnBaseDocsApi.Models;

namespace OnBaseDocsApi
{
    public class Global : HttpApplication
    {
        public static ApiConfig Config { get; private set; }
        public static Dictionary<string, Credentials> Profiles { get; private set; }

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

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
        }
    }
}

