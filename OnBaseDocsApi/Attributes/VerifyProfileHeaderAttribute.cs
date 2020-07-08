using System;
using System.Web.Http.Controllers;
using System.Linq;

namespace OnBaseDocsApi.Attributes
{
    public class VerifyProfileHeaderAttribute : BaseAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (!actionContext.Request.Headers.TryGetValues("OnBase-Profile", out var profiles)
                || (profiles.Count() != 1))
            {
                // The request does not have the required header.
                SetUnauthorizedResult(actionContext, "The account has not been granted access.");
                return;
            }

            var profile = profiles.First();
            var creds = Global.Profiles.GetProfile(profile);
            if (creds == null)
            {
                // The request has a profile that is not known.
                SetUnauthorizedResult(actionContext, $"The account profile '{profile}' is not valid.");
                return;
            }

            // The profile is valid.
            actionContext.Request.Properties.Add("Profile", profile);
        }
    }
}
