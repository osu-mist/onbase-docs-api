﻿using System.Net;
using System.Web.Http;
using OnBaseDocsApi.Models;

namespace OnBaseDocsApi.Controllers
{
    public abstract class BaseApiController : ApiController
    {
        protected IHttpActionResult BadRequestResult(string detail)
        {
            return ErrorResult(HttpStatusCode.BadRequest, "Bad request", detail);
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
                    Errors = new Error[]
                    {
                        new Error
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
