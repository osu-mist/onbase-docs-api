using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using Hyland.Unity;

namespace OnBaseDocsApi.Models
{
    public class ProfileCollection : IDisposable
    {
        readonly object Lock = new object();
        readonly ConcurrentDictionary<string, Profile> Profiles =
            new ConcurrentDictionary<string, Profile>();

        static readonly ILog log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ProfileCollection(Dictionary<string, Credential> credentials)
        {
            foreach (var cred in credentials)
            {
                Profiles[cred.Key] = new Profile
                {
                    Name = cred.Key,
                    Application = null,
                    Credential = cred.Value,
                };
            }

            Refresh();
        }

        public bool IsValid(string profileName)
        {
            return Profiles.ContainsKey(profileName);
        }

        public Application LogIn(string profileName)
        {
            var config = Global.Config;
            var profile = Profiles[profileName];

            Application app;
            // Try a session id login.
            try
            {
                app = SessionIdLogIn(config, profile);
            }
            catch (SessionNotFoundException ex)
            {
                // The session id was not found, try login with credentials.
                app = null;
                log.Error($"Session not found. {ex}");
            }

            // If we don't have a valid OnBase application do a credential login.
            if (app == null)
            {
                if (!CredentialLogIn(config, profile))
                {
                    var msg = $"OnBase login failed for profile '{profile.Name}'.";
                    log.Error(msg);
                    throw new Exception(msg);
                }

                /*
                 * The profile is no longer valid since there was
                 * a credential login so lookup the profile again.
                 */
                profile = Profiles[profileName];
                app = SessionIdLogIn(config, profile);
            }

            if (app == null)
            {
                var msg = $"Could not get an OnBase application for profile {profile.Name}.";
                log.Error(msg);
                throw new Exception(msg);
            }
            return app;
        }

        Application SessionIdLogIn(ApiConfig config, Profile profile)
        {
            if (profile.Application == null)
            {
                // Log in should have happened at startup or last refresh.
                log.Error($"No profile application exists: {profile.Name}.");
                return null;
            }

            // Log in using the session ID.
            var props = Application.CreateSessionIDAuthenticationProperties(
                config.ServiceUrl,
                profile.Application.SessionID,
                false
            );
            return Application.Connect(props);
        }

        bool CredentialLogIn(ApiConfig config, Profile profile)
        {
            var props = Application.CreateOnBaseAuthenticationProperties(
                config.ServiceUrl,
                profile.Credential.Username,
                profile.Credential.Password,
                config.DataSource
            );
            var app = Application.Connect(props);
            if (app == null)
                return false;

            var oldApp = profile.Application;

            Profiles[profile.Name] = new Profile
            {
                Name = profile.Name,
                Application = app,
                Credential = profile.Credential,
            };

            // Release the old OnBase application.
            Task.Run(() =>
            {
                /*
                 * Wait to allow requests using the old application
                 * to login before releasing it.
                 */
                System.Threading.Thread.Sleep(10000);
                oldApp.Disconnect();
                oldApp.Dispose();
            });

            return true;
        }

        public void Refresh()
        {
            var config = Global.Config;
            var profileNames = Profiles.Keys.ToList();

            // Do a credential login for all of the profiles.
            Task.WaitAll(profileNames.Select(profileName => Task.Run(() =>
            {
                try
                {
                    CredentialLogIn(config, Profiles[profileName]);
                }
                catch (Exception ex)
                {
                    log.Error($"Credential login failed. {ex}");
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
                        profile.Value.Application.Dispose();
                        profile.Value.Application = null;
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Logout failed. {ex}");
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
            public string Name { get; set; }
            public Credential Credential { get; set; }
            public Application Application { get; set; }
        }
    }
}