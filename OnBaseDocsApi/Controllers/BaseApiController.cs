using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using OnBaseDocsApi.Models;

namespace OnBaseDocsApi.Controllers
{
    public abstract class BaseApiController : ApiController
    {
        protected string ToMessage(Exception ex)
        {
            if (IsRequestVerbose())
                return ex.ToString();
            else
                return ex.Message;
        }

        protected bool IsRequestVerbose()
        {
            var queryString = Request.GetQueryNameValuePairs()
                .LastOrDefault(x => x.Key == "verbose").Value;

            if (Boolean.TryParse(queryString, out var isVerbose))
                return isVerbose;
            else
                return false;
        }

        protected Error BadRequestError(string detail)
        {
            return new Error
            {
                Status = ((int)HttpStatusCode.BadRequest).ToString(),
                Title = "Bad request",
                Detail = detail,
            };
        }

        protected IHttpActionResult AccessDeniedResult(string detail)
        {
            return ErrorResult(HttpStatusCode.Unauthorized, "Access denied", detail);
        }

        protected IHttpActionResult BadRequestResult(string detail)
        {
            return ErrorResult(HttpStatusCode.BadRequest, "Bad request", detail);
        }

        protected IHttpActionResult BadRequestResult(IEnumerable<Error> errors)
        {
            return ErrorResult(HttpStatusCode.BadRequest, errors);
        }

        protected IHttpActionResult ForbiddenResult(string detail)
        {
            return ErrorResult(HttpStatusCode.Forbidden, "Forbidden", detail);
        }

        protected IHttpActionResult NotFoundResult(string title, string detail)
        {
            return ErrorResult(HttpStatusCode.NotFound, title, detail);
        }

        protected IHttpActionResult InternalErrorResult(string detail)
        {
            return ErrorResult(HttpStatusCode.InternalServerError, "Internal Server Error", detail);
        }

        protected IHttpActionResult ErrorResult(HttpStatusCode statusCode, string title, string detail)
        {
            var strStatus = ((int)statusCode).ToString();
            var strCode = "1" + strStatus;

            return Content(statusCode,
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

        protected IHttpActionResult ErrorResult(HttpStatusCode statusCode, IEnumerable<ErrorResource> errors)
        {
            return Content(statusCode,
                new ErrorResult
                {
                    Errors = errors
                });
        }

        protected IHttpActionResult ErrorResult(HttpStatusCode statusCode, IEnumerable<Error> errors)
        {
            return ErrorResult(statusCode,
                errors.Select(x =>
                {
                    var strCode = $"1{x.Status}";

                    return new ErrorResource
                    {
                        Status = x.Status,
                        Code = strCode,
                        Title = x.Title,
                        Detail = x.Detail,
                        Links = new ErrorLinks
                        {
                            About = $"https://developer.oregonstate.edu/documentation/error-reference#{strCode}"
                        }
                    };
                }));
        }
    }
}
