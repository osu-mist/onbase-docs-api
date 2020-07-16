using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyland.Unity;

namespace OnBaseDocsApi.Models
{
    public class ProfileCollection : IDisposable
    {
        readonly object Lock = new object();

        Dictionary<string, Profile> Profiles;

        public ProfileCollection(Dictionary<string, Credential> credentials)
        {
            Profiles = credentials.ToDictionary(
                x => x.Key,
                x => new Profile
                {
                    Application = null,
                    Credential = x.Value,
                });
            LogInAll(Profiles);
        }

        public bool IsValid(string profileName)
        {
            if (!Profiles.ContainsKey(profileName))
                return false;

            var profile = Profiles[profileName];
            return profile.Application != null;
        }

        public Application LogIn(string profileName)
        {
            var profile = Profiles[profileName];
            if (profile.Application == null)
            {
                // Log in should have happened at startup or last refresh.
                throw new Exception("Application has not initially logged in.");
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
                newProfiles = Profiles.ToDictionary(
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
                oldProfiles = Profiles;
                Profiles = newProfiles;
            }

            // Log out all of old OnBase Application.
            LogOutAll(oldProfiles);
        }

        static void LogInAll(Dictionary<string, Profile> profiles)
        {
            var config = Global.Config;

            Task.WaitAll(profiles.Select(profile => Task.Run(() =>
            {
                try
                {
                    var props = Application.CreateOnBaseAuthenticationProperties(
                        config.ServiceUrl,
                        profile.Value.Credential.Username,
                        profile.Value.Credential.Password,
                        config.DataSource
                    );
                    profile.Value.Application = Application.Connect(props);
                }
                catch
                {
                    // TODO: Log the error.
                    profile.Value.Application = null;
                }
            })).ToArray());
        }

        static void LogOutAll(Dictionary<string, Profile> profiles)
        {
            Task.WaitAll(profiles.Select(profile => Task.Run(() =>
            {
                try
                {
                    if (profile.Value.Application != null)
                    {
                        profile.Value.Application.Disconnect();
                        profile.Value.Application = null;
                    }
                }
                catch
                {
                    // TODO: Log the error.
                    profile.Value.Application = null;
                }
            })).ToArray());
        }

        public void Dispose()
        {
            LogOutAll(Profiles);
        }

        class Profile
        {
            public Credential Credential { get; set; }
            public Application Application { get; set; }
        }
    }
}