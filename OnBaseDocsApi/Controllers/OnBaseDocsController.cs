using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Threading.Tasks;
using Hyland.Types;
using Hyland.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OnBaseDocsApi.Attributes;
using OnBaseDocsApi.Models;

using Keyword = OnBaseDocsApi.Models.Keyword;

namespace OnBaseDocsApi.Controllers
{
    [BasicAuthentication]
    public class OnBaseDocsController : BaseApiController
    {
        [HttpGet]
        [ActionName("")]
        public IHttpActionResult Get(int id)
        {
            return TryHandleDocRequest(id, (_, doc) =>
            {
                return DocumentResult(doc, false);
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

        [HttpPost]
        [ActionName("")]
        public async Task<IHttpActionResult> Post()
        {
            // We only support multi-part post content.
            if (!Request.Content.IsMimeMultipartContent())
            {
                return BadRequestResult("Uploading a document requires a multi-part POST request.");
            }

            Stream docStream = null;
            string docExt = null;
            DocumentPostAttributes docAttr = null;

            // We bundle the bad requests in one response.
            var errors = new List<Error>();

            try
            {
                var provider = await Request.Content.ReadAsMultipartAsync(
                    new MultipartMemoryStreamProvider());

                foreach (HttpContent content in provider.Contents)
                {
                    // The Content-Disposition header is required.
                    if (content.Headers.ContentDisposition == null)
                        return BadRequestResult("A Content-Disposition header is required.");

                    var dispoName = content.Headers.ContentDisposition.Name.Trim('"');

                    if (dispoName == "file")
                    {
                        // The content is a file.
                        // Only allow one content file.
                        if ((docStream != null) || !string.IsNullOrEmpty(docExt))
                            return BadRequestResult("More than one document content was included.");

                        var fileName = content.Headers.ContentDisposition.FileName.Trim('"');
                        docStream = content.ReadAsStreamAsync().Result;
                        docExt = Path.GetExtension(fileName).TrimStart('.');
                    }
                    else if (dispoName == "attributes")
                    {
                        // The content is JSON.
                        // Only allow one JSON attributes.
                        if (docAttr != null)
                            return BadRequestResult("More than one document attributes was included.");

                        docAttr = JObject
                            .Parse(content.ReadAsStringAsync().Result)
                            .ToObject<DocumentPostAttributes>();
                    }
                    else
                    {
                        return BadRequestResult($"Unexpected Content-Disposition header of name '{dispoName}'.");
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                errors.Add(BadRequestError($"JSON parse error. {ex.Message}"));
            }
            catch (IOException ex) when (ex.Message.Contains("MIME multipart message is not complete"))
            {
                errors.Add(BadRequestError(ex.Message));
            }
            catch (Exception ex)
            {
                return InternalErrorResult(ex.Message);
            }

            if ((docStream == null) || string.IsNullOrWhiteSpace(docExt))
                errors.Add(BadRequestError("The required parameter 'file' is missing."));
            if (docAttr == null)
                errors.Add(BadRequestError("The required parameter 'attributes' is missing."));
            if ((docAttr == null) || string.IsNullOrWhiteSpace(docAttr.IndexKey))
                errors.Add(BadRequestError("The required parameter 'IndexKey' is empty."));
            if ((docAttr == null) || string.IsNullOrWhiteSpace(docAttr.DocumentType))
                errors.Add(BadRequestError("The required parameter 'DocumentType' is empty."));

            // Check if there are any errors. If there are then return them.
            if (errors.Any())
                return BadRequestResult(errors);

            return TryHandleRequest(app =>
            {
                // The document is moved to staging when there are no keywords included
                // so that we can kick off a re-index to generate the autofill keywords.
                bool toStaging = (docAttr.Keywords == null) || !docAttr.Keywords.Any();

                // When going to staging, confirm that the final doc type is actually valid.
                DocumentType finalDocType = null;
                if (toStaging)
                {
                    finalDocType = app.Core.DocumentTypes.Find(docAttr.DocumentType);
                    if (finalDocType == null)
                        return BadRequestResult($"The DocumentType '{docAttr.DocumentType}' could not be found.");
                }

                var createAttr = new DocumentCreateAttributes
                {
                    DocumentType = toStaging ? Global.Config.StagingDocType : docAttr.DocumentType,
                    Comment = docAttr.Comment,
                    IndexKey = docAttr.IndexKey,
                    Keywords = docAttr.Keywords,
                    Ext = docExt,
                    Stream = docStream,
                    ToStaging = toStaging,
                };
                var error = CreateDocument(app, createAttr, out var doc);
                if (error != null)
                    return error;

                if (toStaging)
                {
                    // We must get the document ID from this thread and not the task thread since
                    // the document could go out of scope before the task starts.
                    var docId = doc.ID;
                    Task.Run(() =>
                    {
                        MoveDocumentToWorkflow(docId, finalDocType);
                    });
                }

                return DocumentResult(doc, true);
            });
        }

        /// <summary>
        /// CreateDocument returns an error result when an error occurs; null otherwise. If CreateDocument
        /// return null, then the out parameter doc is guaranteed to be valid.
        /// </summary>
        /// <returns>An error result or null. A null result indicates that the document was created successfully.</returns>
        IHttpActionResult CreateDocument(Application app, DocumentCreateAttributes attr, out Document doc)
        {
            doc = null;

            var errors = new List<Error>();

            var docType = app.Core.DocumentTypes.Find(attr.DocumentType);
            if (docType == null)
                errors.Add(BadRequestError($"The DocumentType '{attr.DocumentType}' could not be found."));

            var fileType = app.Core.FileTypes.Find(attr.Ext);
            if (fileType == null)
                errors.Add(BadRequestError($"The FileType '{attr.Ext}' could not be found."));

            var pageData = app.Core.Storage.CreatePageData(attr.Stream, attr.Ext);
            if (pageData == null)
                errors.Add(BadRequestError($"Unable to create page data for '{attr.Ext}'."));

            if (errors.Any())
                return BadRequestResult(errors);

            var props = app.Core.Storage.CreateStoreNewDocumentProperties(docType, fileType);
            if (props == null)
                return BadRequestResult($"Unable to create document properties for '{attr.DocumentType}' and '{attr.Ext}'.");

            props.AddKeyword(Global.Config.DocIndexKeyName, attr.IndexKey);
            props.DocumentDate = DateTime.Now;
            props.Comment = attr.Comment;
            props.ExpandKeysets = false;

            if (attr.Keywords != null)
            {
                foreach (var kw in attr.Keywords)
                {
                    props.AddKeyword(kw.Name, kw.Value);
                }
            }

            doc = app.Core.Storage.StoreNewDocument(pageData, props);
            if (doc == null)
                return BadRequestResult("Unable to create document.");

            return null;
        }

        void MoveDocumentToWorkflow(long docId, DocumentType docType)
        {
            TryHandleDocRequest(docId, (app, doc) =>
            {
                // First index the document so that the autofill keywords are populated
                // then add the document to the workflow.
                var reindexProps = app.Core.Storage.CreateReindexProperties(doc, docType);
                reindexProps.ExpandKeysets = true;
                doc = app.Core.Storage.ReindexDocument(reindexProps);

                return null;
            });
        }

        IHttpActionResult TryHandleDocRequest(long id, Func<Application, Document, IHttpActionResult> handler)
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
            const string profile = "test";

            try
            {
                var creds = Global.Profiles.GetProfile(profile);
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

        IHttpActionResult DocumentResult(Document doc, bool createdDoc)
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

            var endpointUri = $"{config.ApiHost}/{config.ApiBasePath}";
            var selfUri = $"{endpointUri}/{doc.ID}";

            var result = new DataResult<DocumentAttributes>
            {
                Data = new DataResource<DocumentAttributes>
                {
                    ID = doc.ID.ToString(),
                    Type = "onbaseDocument",
                    Attributes = new DocumentAttributes
                    {
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
                        Self = selfUri,
                    }
                },
                Links = new DataLinks
                {
                    Self = endpointUri,
                }
            };

            if (createdDoc)
                return Created(selfUri, result);
            else
                return Ok(result);
        }
    }
}
