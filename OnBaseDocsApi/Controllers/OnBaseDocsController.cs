using System;
using System.Linq;
using System.Web.Http;
using Hyland.Types;
using Hyland.Unity;
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
                var creds = Global.GetProfile(profile);
                if (creds == null)
                    return InternalErrorResult($"The profile '{profile}' is not valid.");

                var config = Global.Config;
                var props = Application.CreateOnBaseAuthenticationProperties(
                    config.ServiceUrl, creds.Username, creds.Password, config.DataSource);
                using (var app = Application.Connect(props))
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
