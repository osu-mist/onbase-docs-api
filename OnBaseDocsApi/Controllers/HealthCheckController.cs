using System;
using System.Web.Http;
using OnBaseDocsApi.Attributes;
using OnBaseDocsApi.Models;

namespace OnBaseDocsApi.Controllers
{
    [BasicAuthentication]
    public class HealthCheckController : BaseApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            var now = DateTime.Now;
            var unixNow = (UInt64) now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            return Ok(new HealthCheck
            {
                Meta = new HealthCheckMeta
                {
                    Name = "OnBase Documents",
                    Time = now.ToString("yyyy-MM-dd HH:mm:sszz"),
                    UnixTime = unixNow,
                    Commit = "",
                    Documentation = "openapi.yaml",
                }
            });
        }
    }
}
