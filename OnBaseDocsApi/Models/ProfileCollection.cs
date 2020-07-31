using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Hyland.Unity;

namespace OnBaseDocsApi.Models
{
    public class ProfileCollection : IDisposable
    {
        readonly object Lock = new object();
        readonly ConcurrentDictionary<string, Profile> Profiles =
            new ConcurrentDictionary<string, Profile>();

        public ProfileCollection(Dictionary<string, Credential> credentials)
        {
            foreach (var cred in credentials)
            {
                Profiles[cred.Key] = new Profile
                {
                    Application = null,
                    Credential = cred.Value,
                };
            }

            LogInAll();
        }

        public bool IsValid(string profileName)
        {
            if (!Profiles.ContainsKey(profileName))
                return false;

            var profile = Profiles[profileName];

            if (profile.Application == null)
            {
                // We don't have an OnBase application for this profile, try to create one.
                profile.Application = LogIn(Global.Config, profile.Credential);
            }

            return profile.Application != null;
        }

        public Application LogIn(string profileName)
        {
            var config = Global.Config;
            var profile = Profiles[profileName];

            if (profile.Application == null)
            {
                // We don't have an OnBase application for this profile, try to create one.
                profile.Application = LogIn(config, profile.Credential);
            }
            if (profile.Application == null)
            {
                // Log in should have happened at startup or last refresh.
                throw new Exception("Application has not initially logged in.");
            }

            // Log in using the session ID.
            var props = Application.CreateSessionIDAuthenticationProperties(
                config.ServiceUrl,
                profile.Application.SessionID,
                false
            );
            return Application.Connect(props);
        }

        public void Refresh()
        {
            LogInAll();
        }

        Application LogIn(ApiConfig config, Credential cred)
        {
            var props = Application.CreateOnBaseAuthenticationProperties(
                config.ServiceUrl,
                cred.Username,
                cred.Password,
                config.DataSource
            );
            return Application.Connect(props);
        }

        void LogInAll()
        {
            var config = Global.Config;

            Task.WaitAll(Profiles.Select(p => Task.Run(() =>
            {
                try
                {
                    var currApp = p.Value.Application;
                    var newApp = LogIn(config, p.Value.Credential);
                    // TODO: Log login failure error.

                    if (newApp != null)
                    {
                        // Only update if we have a new application.
                        Profiles[p.Key] = new Profile
                        {
                            Application = newApp,
                            Credential = p.Value.Credential,
                        };

                        if (currApp != null)
                        {
                            /*
                             * Wait to allow requests using the old application
                             * to complete before releasing it.
                             */
                            System.Threading.Thread.Sleep(10000);
                            currApp.Disconnect();
                        }
                    }
                }
                catch
                {
                    // TODO: Log the error.
                }
            })).ToArray());
        }

        void LogOutAll()
        {
            Task.WaitAll(Profiles.Select(profile => Task.Run(() =>
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
            LogOutAll();
        }

        class Profile
        {
            public Credential Credential { get; set; }
            public Application Application { get; set; }
        }
    }
}