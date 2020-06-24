using System.Collections.Generic;

namespace OnBaseDocsApi.Models
{
    public class Profiles
    {
        private readonly Dictionary<string, Credentials> _profiles;

        public Profiles(Dictionary<string, Credentials> credentials)
        {
            _profiles = credentials;
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