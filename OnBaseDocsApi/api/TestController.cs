using System;
using System.Web.Http;

namespace OnBaseDocsApi.Controllers
{
    public class TestController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            var values = new int[] { 1, 2, 3, 4, 5 };
            return Ok(values);
        }
    }
}
