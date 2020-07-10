using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyland.Unity;

namespace OnBaseDocsApi.Models
{
    public class Profiles
    {
        readonly object Lock = new object();

        Dictionary<string, Profile> _profiles;

        public Profiles(Dictionary<string, Credential> credentials)
        {
            _profiles = credentials.ToDictionary(
                x => x.Key,
                x => new Profile
                {
                    Application = null,
                    Credential = x.Value,
                });
            LogInAll(_profiles);
        }

        public Application LogIn(string profileName)
        {
            var profile = _profiles[profileName];
            if (profile.Application == null)
            {
                // Log in should have happened at startup or last refresh. It's
                // possible
                throw new Exception("Application has not been initially logged in.");
            }

            // Log in using the session ID.
            var config = Global.Config;
            var props = Application.CreateSessionIDAuthenticationProperties(
                config.ServiceUrl,
                profile.Application.SessionID,
                false
            );
            return Application.Connect(props);
        }

        public void Refresh()
        {
            // Make a copy of the existing credentials.
            Dictionary<string, Profile> newProfiles;
            lock (Lock)
            {
                newProfiles = _profiles.ToDictionary(
                    x => x.Key,
                    x => new Profile
                    {
                        Application = null,
                        Credential = x.Value.Credential,
                    });
            }

            // Login each credential so that we get a new OnBase Application.
            LogInAll(newProfiles);

            // Swap in the new profiles.
            Dictionary<string, Profile> oldProfiles;
            lock (Lock)
            {
                oldProfiles = _profiles;
                _profiles = newProfiles;
            }

            // Log out all of old OnBase Application.
            LogOutAll(oldProfiles);
        }

        static void LogInAll(Dictionary<string, Profile> profiles)
        {
            var config = Global.Config;

            Task.WaitAll(profiles.Select(profile => Task.Run(() =>
            {
                var props = Application.CreateOnBaseAuthenticationProperties(
                    config.ServiceUrl,
                    profile.Value.Credential.Username,
                    profile.Value.Credential.Password,
                    config.DataSource
                );
                profile.Value.Application = Application.Connect(props);
            })).ToArray());
        }

        static void LogOutAll(Dictionary<string, Profile> profiles)
        {
            Task.WaitAll(profiles.Select(profile => Task.Run(() =>
            {
                profile.Value.Application.Disconnect();
                profile.Value.Application = null;
            })).ToArray());
        }

        class Profile
        {
            public Credential Credential { get; set; }
            public Application Application { get; set; }
        }

        public class ProfileNotFoundException : Exception
        {
            public ProfileNotFoundException()
            {
            }
        }
    }
}