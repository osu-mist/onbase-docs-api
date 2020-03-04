using System.IO;
using System.Collections.Generic;

namespace OnBaseDocsApi.Models
{
    public class Profiles
    {
        private readonly Dictionary<string, Credentials> _profiles;

        public Profiles(YamlDotNet.Serialization.IDeserializer deserializer, string path)
        {
            _profiles = deserializer.Deserialize<Dictionary<string, Credentials>>(File.ReadAllText(path));
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
