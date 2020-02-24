<<<<<<< HEAD
using System.Web;
using System.Web.Http;
=======
﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json.Linq;
>>>>>>> Adding get document by id.
using OnBaseDocsApi.Models;

namespace OnBaseDocsApi
{
    public class Global : HttpApplication
    {
        public static ApiConfig Config { get; private set; }
<<<<<<< HEAD
        public static Profiles Profiles { get; private set; }
=======
        public static Dictionary<string, Credentials> Profiles { get; private set; }
>>>>>>> Adding get document by id.

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

<<<<<<< HEAD
<<<<<<< HEAD
            // Load api config
            Config = new ApiConfig("api-config.json");
            // Load profiles
            Profiles = new Profiles("profiles.json");
=======
=======
>>>>>>> Adding get document by id.
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
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> Fixing a config read bug.
                ApiBasePath = config.Value<string>("apiBasePath"),
                ApiHost = config.Value<string>("apiHost"),
                ServiceUrl = config.Value<string>("serviceUrl"),
                DataSource = config.Value<string>("dataSource"),
<<<<<<< HEAD
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
>>>>>>> Adding support for document upload.
=======
                ApiUri = config["config"].Value<string>("apiUri"),
=======
                ApiUri = config["config"].Value<string>("apiBasePath"),
>>>>>>> Fixing error in config loading.
                ApiHost = config["config"].Value<string>("apiHost"),
                ServiceUrl = config["config"].Value<string>("serviceUrl"),
                DataSource = config["config"].Value<string>("dataSource"),
=======
>>>>>>> Fixing a config read bug.
            };
            Profiles = new Dictionary<string, Credentials>();
            foreach (JObject profile in config["profiles"])
            {
                // We only support one set of credentials per profile.
                var prop = profile.Properties().FirstOrDefault();
                if (prop != null)
                {
                    Profiles[prop.Name] = new Credentials
                    {
                        Username = prop.Value.Value<string>("username"),
                        Password = prop.Value.Value<string>("password")
                    };
                }
            }
>>>>>>> Adding get document by id.
        }
    }
}

