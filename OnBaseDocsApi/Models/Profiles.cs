// #define LOG_IN_WITH_SID
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyland.Unity;

namespace OnBaseDocsApi.Models
{
    public class Profiles
    {
#if LOG_IN_WITH_SID
        private readonly Dictionary<string, Profile> _profiles;

        public Profiles(Dictionary<string, Credentials> credentials)
        {
            _profiles = new Dictionary<string, Profile>();
            foreach (KeyValuePair<string, Credentials> entry in credentials)
            {
                // Initialize each Profile with credentials from the config and a null Application
                _profiles.Add(entry.Key, new Profile() { Credentials = entry.Value, Application = null });
            }
        }
#else
        private readonly Dictionary<string, Credentials> _profiles;

        public Profiles(Dictionary<string, Credentials> credentials)
        {
            _profiles = credentials;
        }
#endif
#if LOG_IN_WITH_SID
        public void LogInAll()
        {
            Task.WaitAll(_profiles.Select(profile => Task.Run(() =>
            {
                var config = Global.Config;
                var props = Application.CreateOnBaseAuthenticationProperties(
                    config.ServiceUrl,
                    profile.Value.Credentials.Username,
                    profile.Value.Credentials.Password,
                    config.DataSource
                );
                profile.Value.Application = Application.Connect(props);
            })).ToArray());
        }

        public void LogOutAll()
        {
            Task.WaitAll(_profiles.Select(profile => Task.Run(() =>
            {
                profile.Value.Application.Disconnect();
                profile.Value.Application = null;
            })).ToArray());
        }

        // TODO: implement. Make sure to avoid race conditions
        public void RefreshAll()
        {
        }

        public Application LogIn(string profileName)
        {
            if (!_profiles.ContainsKey(profileName))
            {
                throw new ProfileNotFoundException();
            }
            Profile profile = _profiles[profileName];
            if (profile.Application == null)
            {
                /*
                 * Initial log in should have happened at startup. Logins will periodically refresh
                 * as well
                 */
                throw new System.Exception("Application has not been initially logged in");
            }
            else
            {
                // Log in using sid
                var config = Global.Config;
                var props = Application.CreateSessionIDAuthenticationProperties(
                    config.ServiceUrl,
                    profile.Application.SessionID,
                    false
                );
                return Application.Connect(props);
            }
        }
#else
        public Application LogIn(string profileName)
        {
            if (!_profiles.ContainsKey(profileName))
            {
                throw new ProfileNotFoundException();
            }
            var config = Global.Config;
            var props = Application.CreateOnBaseAuthenticationProperties(
                config.ServiceUrl,
                _profiles[profileName].Username,
                _profiles[profileName].Password,
                config.DataSource
            );
            return Application.Connect(props);
        }
#endif

#if LOG_IN_WITH_SID
    private class Profile
        {
            public Credentials Credentials { get; set; }
            public Application Application { get; set; }
        }
#endif

        public class ProfileNotFoundException : Exception
        {
            public ProfileNotFoundException()
            {
            }
        }

    }
}
