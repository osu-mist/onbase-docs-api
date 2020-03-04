using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OnBaseDocsApi.Models
{
    public class Profiles
    {
        private readonly Dictionary<string, Credentials> _profiles;

        public Profiles(string path)
        {
            _profiles = new Dictionary<string, Credentials>();
            var config = JObject.Parse(File.ReadAllText(path));

            foreach (var prop in (config as JObject).Properties())
            {
                _profiles[prop.Name] = new Credentials
                {
                    Username = prop.Value.Value<string>("username"),
                    Password = prop.Value.Value<string>("password")
                };
            }
        }

        public Credentials GetProfile(string profileName)
        {
            if (!_profiles.ContainsKey(profileName))
            {
                return null;
            }
            return _profiles[profileName];
        }
    }
}
