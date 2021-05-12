using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using OnBaseDocsApi.Models;

namespace OnBaseDocsApi.Attributes
{
    public class BaseAttribute : AuthorizationFilterAttribute
    {
        protected void SetUnauthorizedResult(HttpActionContext actionContext,  string detail)
        {
            SetResult(actionContext, HttpStatusCode.Unauthorized, "Unauthorized", detail);
        }

        protected void SetResult(HttpActionContext actionContext, HttpStatusCode statusCode, string title, string detail)
        {
            var strStatus = ((int)statusCode).ToString();
            var strCode = "1" + strStatus;

            actionContext.Response = actionContext.Request.CreateResponse(statusCode,
                new ErrorResult
                {
                    Errors = new ErrorResource[]
                    {
                        new ErrorResource
                        {
                            Status = strStatus,
                            Code = strCode,
                            Title = title,
                            Detail = detail,
                            Links = new ErrorLinks
                            {
                                About = $"https://developer.oregonstate.edu/documentation/error-reference#{strCode}"
                            }
                        }
                    }
                });
        }
    }
}
