using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Hyland.Types;
using Hyland.Unity;
using System.Linq;
using OnBaseDocsApi.Models;

using Keyword = OnBaseDocsApi.Models.Keyword;

namespace OnBaseDocsApi.Controllers
{
    public class OnBaseDocsController : BaseApiController
    {
        [HttpGet]
        [ActionName("")]
        public IHttpActionResult Get(int id)
        {
            return TryHandleDocRequest(id, (_, doc) =>
            {
                return DocumentResult(doc);
            });
        }

        [HttpGet]
        [ActionName("File")]
        public IHttpActionResult GetFile(int id)
        {
            return TryHandleDocRequest(id, (app, doc) =>
            {
                var pdf = app.Core.Retrieval.PDF.GetDocument(
                    doc.DefaultRenditionOfLatestRevision);

                using (var stream = new MemoryStream())
                {
                    pdf.Stream.CopyTo(stream);

                    var result = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(stream.GetBuffer())
                    };
                    result.Content.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("inline")
                        {
                            FileName = $"{doc.ID}.pdf"
                        };
                    result.Content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/pdf");

                    return ResponseMessage(result);
                }
            });
        }

        IHttpActionResult TryHandleDocRequest(int id, Func<Application, Document, IHttpActionResult> handler)
        {
            return TryHandleRequest((app) =>
            {
                var doc = app.Core.GetDocumentByID(id);
                if (doc == null)
                    return NotFoundResult(
                        "Document not found",
                        "The OnBase API GetDocumentByID returned a null result.");

                return handler(app, doc);
            });
        }

        IHttpActionResult TryHandleRequest(Func<Application, IHttpActionResult> handler)
        {
            const string profile = "default";


            try
            {
                var app = Global.Profiles.LogIn(profile);
                if (app == null)
                {
                    return InternalErrorResult($"The profile '{profile}' is not valid.");
                }
                using (app)
                {
                    return handler(app);
                }
            }
            catch (Exception ex)
            {
                return InternalErrorResult(ex.Message);
            }
        }

        IHttpActionResult DocumentResult(Document doc)
        {
            var config = Global.Config;

            var keywords = doc.KeywordRecords
                .SelectMany(x => x.Keywords.Select(k =>
                    new Keyword
                    {
                        Name = k.KeywordType.Name,
                        Value = k.Value.ToString()
                    }))
                .ToArray();

            return Ok(new DataResult<DocumentAttributes>
            {
                Data = new DocumentAttributes
                {
                    ID = doc.ID,
                    CreatedBy = doc.CreatedBy.ID,
                    DateStored = doc.DateStored,
                    DocumentDate = doc.DocumentDate,
                    Status = doc.Status.ToString(),
                    Name = doc.Name,
                    DocumentType = doc.DocumentType.Name,
                    DefaultFileType = doc.DefaultFileType.Name,
                    LatestAllowedRevisionID = doc.LatestAllowedRevisionID,
                    Keywords = keywords,
                },
                Links = new DataLinks
                {
                    Self = $"{config.ApiHost}/{config.ApiBasePath}/{doc.ID}",
                }
            });
        }
    }
}
