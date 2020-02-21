using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using OnBaseDocsApi.Models;

namespace OnBaseDocsApi.Controllers
{
    public class DocumentsController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            DocumentAttributes d = new DocumentAttributes();
            d.CreatedBy = 1;
            d.DateStored = DateTime.Now;
            d.DocumentDate = DateTime.Now;
            d.DocumentType = "testDocumentType";
            d.Status = "testDocumentStatus";
            d.Name = "testDocumentName";
            d.ID = 2;
            d.LatestAllowedRevisionID = 3;
            d.DefaultFileType = "testDefaultFileType";

            Keyword keyword1 = new Keyword();
            keyword1.Name = "keyword1";
            keyword1.Value = "keywordVal1";

            Keyword keyword2 = new Keyword();
            keyword2.Name = "keyword2";
            keyword2.Value = "keywordVal2";

            List<Keyword> keywords = new List<Keyword>();
            keywords.Add(keyword1);
            keywords.Add(keyword2);
            d.Keywords = keywords;


            return Ok(d);
        }
    }
}
