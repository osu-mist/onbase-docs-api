using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace OnBaseDocsApi.Attributes
{
    public class BasicAuthenticationAttribute : BaseAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext.Request.Headers.Authorization == null)
            {
                // The request is unauthorized since the Authorization
                // header is missing.
                SetUnauthorizedResult(actionContext, "The authorization credentials were not included.");
                actionContext.Response.Headers.Add("WWW-Authenticate", "Basic");
            }
            else
            {
                var config = Global.Config;

                // The request has an Authorization header. Get the
                // authentication token from the header and validate it.
                var authToken = TryParseToken(
                    actionContext.Request.Headers.Authorization.Parameter);
                if (string.IsNullOrEmpty(authToken))
                {
                    SetUnauthorizedResult(actionContext, "The authorization credentials are invalid.");
                    return;
                }

                // Convert the string into an string array.
                string[] parts = authToken.Split(':');
                // First element of the array is the username.
                string username = parts[0];
                // Second element of the array is the password.
                string password = parts[1];

                // Validate the username and password.
                if ((username != config.Authentication.Username)
                    || (password != config.Authentication.Password))
                {
                    SetUnauthorizedResult(actionContext, "Authorization failed.");
                }
            }
        }

        string TryParseToken(string authToken)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(authToken));
            }
            catch
            {
                return null;
            }
        }
    }
}
