using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Linq;

namespace OnBaseDocsApi.Attributes
{
    public class VerifyProfileHeaderAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (!actionContext.Request.Headers.TryGetValues("OnBase-Profile", out var profiles))
            {
                // The request does not have the required header.
                actionContext.Response = actionContext.Request
                    .CreateResponse(HttpStatusCode.Unauthorized);
                return;
            }

            if (profiles.Count() != 1)
            {
                // The request has more than has more than one header value.
                actionContext.Response = actionContext.Request
                    .CreateResponse(HttpStatusCode.Unauthorized);
                return;
            }
            else
            {
                var profile = profiles.First();

                var creds = Global.Profiles.GetProfile(profile);
                if (creds == null)
                {
                    // The request has a profile that is not known.
                    actionContext.Response = actionContext.Request
                        .CreateResponse(HttpStatusCode.Unauthorized);
                    return;
                }

                // The profile is valid.
                actionContext.Request.Properties.Add("Profile", profile);
            }
        }
    }
}
