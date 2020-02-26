<<<<<<< HEAD
using System;
using System.IO;
<<<<<<< HEAD
using System.Linq;
=======
using System.Linq;
>>>>>>> Adding support for document upload.
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
<<<<<<< HEAD
using System.Threading.Tasks;
=======
using System.Threading.Tasks;
>>>>>>> Adding support for document upload.
using System.Web.Http;
<<<<<<< HEAD
using Hyland.Types;
using Hyland.Unity;
using System.Linq;
=======
using Hyland.Types;
using Hyland.Unity;
using Newtonsoft.Json.Linq;
>>>>>>> Adding support for document upload.
=======
﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web.Http;
using Hyland.Types;
using Hyland.Unity;
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
>>>>>>> Adding get document by id.
=======
using System.Linq;
>>>>>>> Addingget document content by id.
=======
=======
using Newtonsoft.Json;
>>>>>>> Addressing code review comments.
using Newtonsoft.Json.Linq;
>>>>>>> Adding support for document upload.
using OnBaseDocsApi.Models;

using Keyword = OnBaseDocsApi.Models.Keyword;

namespace OnBaseDocsApi.Controllers
{
    public class OnBaseDocsController : BaseApiController
    {
        [HttpGet]
        [ActionName("")]
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        public IHttpActionResult Get(long id)
=======
        public IHttpActionResult Get(int id)
>>>>>>> Adding get document by id.
=======
        public IHttpActionResult Get(long id)
>>>>>>> Updating doc ID to long.
=======
        public IHttpActionResult Get(long id)
>>>>>>> Updating doc ID to long.
        {
            return TryHandleDocRequest(id, (_, doc) =>
            {
                return DocumentResult(doc);
            });
        }

<<<<<<< HEAD
<<<<<<< HEAD
        [HttpGet]
        [ActionName("File")]
        public IHttpActionResult GetFile(long id)
=======
        [HttpGet]
        [ActionName("File")]
<<<<<<< HEAD
<<<<<<< HEAD
        public IHttpActionResult GetFile(int id)
>>>>>>> Addingget document content by id.
=======
        public IHttpActionResult GetFile(long id)
>>>>>>> Updating doc ID to long.
=======
        public IHttpActionResult GetFile(long id)
>>>>>>> Updating doc ID to long.
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

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> Adding support for document upload.
=======
>>>>>>> Adding support for document upload.
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

            try
            {
                var provider = await Request.Content.ReadAsMultipartAsync(
                    new MultipartMemoryStreamProvider());

                foreach (HttpContent content in provider.Contents)
                {
<<<<<<< HEAD
                    // The Content-Disposition header is required.
                    if (content.Headers.ContentDisposition == null)
                        return BadRequestResult($"A Content-Disposition header is required.");

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
=======
                    if (content.Headers.Contains("Content-Type")
                        && content.Headers.Contains("Content-Disposition"))
                    {
                        // The content is a file.
                        if ((docStream != null) || !string.IsNullOrEmpty(docExt))
                            return ConflictResult("More than one document content was included.");

                        var dispo = new ContentDisposition(
                            content.Headers.GetValues("Content-Disposition").First());
                        docStream = content.ReadAsStreamAsync().Result;
                        docExt = Path.GetExtension(dispo.FileName).TrimStart('.');
                    }
                    else
                    {
                        // The content is JSON.
                        if (docAttr != null)
                            return ConflictResult("More than one document attributes was included.");
>>>>>>> Adding support for document upload.

                        docAttr = JObject
                            .Parse(content.ReadAsStringAsync().Result)
                            .ToObject<DocumentPostAttributes>();
                    }
<<<<<<< HEAD
                    else
                    {
                        return BadRequestResult($"Unexpected Content-Disposition header of name '{dispoName}'.");
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                return BadRequestResult($"JSON parse error. {ex.Message}");
            }
            catch (IOException ex) when (ex.Message.Contains("MIME multipart message is not complete"))
            {
                return BadRequestResult(ex.Message);
            }
=======
                }
            }
>>>>>>> Adding support for document upload.
            catch (Exception ex)
            {
                return InternalErrorResult(ex.Message);
            }

            if ((docStream == null) || string.IsNullOrWhiteSpace(docExt))
<<<<<<< HEAD
                return BadRequestResult("The required parameter 'file' is missing.");
            if (docAttr == null)
                return BadRequestResult("The required parameter 'attributes' is missing.");
=======
                return ConflictResult("The required parameter document content is missing.");
            if (docAttr == null)
                return ConflictResult("The required parameter document attributes are missing.");
>>>>>>> Adding support for document upload.
            if (string.IsNullOrWhiteSpace(docAttr.IndexKey))
                return BadRequestResult("The required parameter 'IndexKey' is empty.");
            if (string.IsNullOrWhiteSpace(docAttr.DocumentType))
                return BadRequestResult("The required parameter 'DocumentType' is empty.");

            return TryHandleRequest(app =>
            {
                // The document is moved to staging when there are no keywords included
                // so that we can kick off a re-index to generate the autofill keywords.
                bool toStaging = (docAttr.Keywords == null) || !docAttr.Keywords.Any();

<<<<<<< HEAD
                // When going to staging, confirm that the final doc type is actually valid.
                DocumentType finalDocType = null;
                if (toStaging)
                {
                    finalDocType = app.Core.DocumentTypes.Find(docAttr.DocumentType);
                    if (finalDocType == null)
                        return BadRequestResult($"The DocumentType '{docAttr.DocumentType}' could not be found.");
                }

=======
>>>>>>> Adding support for document upload.
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
                var result = CreateDocument(app, createAttr, out var doc);
                if (result != null)
                    return result;

                if (toStaging)
                {
                    // We must get the document ID from this thread and not the task thread since
                    // the document could go out of scope before the task starts.
                    var docId = doc.ID;
                    Task.Run(() =>
                    {
<<<<<<< HEAD
                        MoveDocumentToWorkflow(docId, finalDocType);
                    });
                }

                return DocumentResult(doc, true);
=======
                        MoveDocumentToWorkflow(docId, docAttr.DocumentType);
                    });
                }

                return DocumentResult(doc);
>>>>>>> Adding support for document upload.
            });
        }

        IHttpActionResult CreateDocument(Application app, DocumentCreateAttributes attr, out Document doc)
        {
            doc = null;

            var docType = app.Core.DocumentTypes.Find(attr.DocumentType);
            if (docType == null)
                return InternalErrorResult($"The DocumentType '{attr.DocumentType}' could not be found.");

            var fileType = app.Core.FileTypes.Find(attr.Ext);
            if (fileType == null)
                return InternalErrorResult($"The FileType '{attr.Ext}' could not be found.");

            var props = app.Core.Storage.CreateStoreNewDocumentProperties(docType, fileType);
            if (props == null)
                return InternalErrorResult($"Unable to create document properties for '{attr.DocumentType}' and '{attr.Ext}'.");

            props.AddKeyword(Global.Config.DocIndexKeyName, attr.IndexKey);
            props.DocumentDate = DateTime.Now;
            props.Comment = attr.Comment;
            props.ExpandKeysets = false;

            if (attr.ToStaging)
            {
                // When uploading a document into staging do not add it to the workflow.
                props.Options = StoreDocumentOptions.SkipWorkflow;
            }

            if (attr.Keywords != null)
            {
                foreach (var kw in attr.Keywords)
                {
                    props.AddKeyword(kw.Name, kw.Value);
                }
            }

            var pageData = app.Core.Storage.CreatePageData(attr.Stream, attr.Ext);
                if (pageData == null)
                    InternalErrorResult($"Unable to create page data for '{attr.Ext}'.");

            doc = app.Core.Storage.StoreNewDocument(pageData, props);
            if (doc == null)
                return InternalErrorResult($"Unable to create document.");

            return null;
        }

<<<<<<< HEAD
        void MoveDocumentToWorkflow(long docId, DocumentType docType)
        {
<<<<<<< HEAD
<<<<<<< HEAD
            TryHandleDocRequest(docId, (app, doc) =>
            {
<<<<<<< HEAD
=======
            TryHandleRequest(app =>
            {
                var doc = app.Core.GetDocumentByID(docId);
>>>>>>> Adding support for document upload.
=======
            TryHandleDocRequest(docId, (app, doc) =>
            {
>>>>>>> Updating doc ID to long.
                var docType = app.Core.DocumentTypes.Find(documentType);

=======
>>>>>>> Addressing another code review comment.
=======
        void MoveDocumentToWorkflow(long docId, string documentType)
        {
            TryHandleDocRequest(docId, (app, doc) =>
            {
                var docType = app.Core.DocumentTypes.Find(documentType);

>>>>>>> Adding support for document upload.
                // First index the document so that the autofill keywords are populated
                // then add the document to the workflow.
                var reindexProps = app.Core.Storage.CreateReindexProperties(doc, docType);
                reindexProps.ExpandKeysets = true;
                doc = app.Core.Storage.ReindexDocument(reindexProps);

                return null;
            });
        }

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        IHttpActionResult TryHandleDocRequest(long id,
            Func<Application, Document, IHttpActionResult> handler)
=======
=======
>>>>>>> Addingget document content by id.
        IHttpActionResult TryHandleDocRequest(int id, Func<Application, Document, IHttpActionResult> handler)
>>>>>>> Adding get document by id.
=======
        IHttpActionResult TryHandleDocRequest(int id,
=======
        IHttpActionResult TryHandleDocRequest(long id,
>>>>>>> Updating doc ID to long.
=======
        IHttpActionResult TryHandleDocRequest(long id,
>>>>>>> Updating doc ID to long.
            Func<Application, Document, IHttpActionResult> handler)
>>>>>>> Adding support for document upload.
=======
        IHttpActionResult TryHandleDocRequest(int id,
            Func<Application, Document, IHttpActionResult> handler)
>>>>>>> Adding support for document upload.
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
<<<<<<< HEAD
                var creds = Global.Profiles.GetProfile(profile);
=======
                var creds = Global.GetProfile(profile);
>>>>>>> Adding get document by id.
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

        IHttpActionResult DocumentResult(Document doc, bool createdDoc = false)
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

            var selfUri = $"{config.ApiHost}/{config.ApiBasePath}/{doc.ID}";

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
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
                    Self = $"{config.ApiHost}/{config.ApiBasePath}/{doc.ID}",
=======
                    Self = $"{config.ApiHost}/{config.ApiUri}/{doc.ID}",
>>>>>>> Adding get document by id.
=======
                    Self = $"{config.ApiHost}/{config.ApiBasePath}/{doc.ID}",
>>>>>>> Fixing a config read bug.
=======
                    Self = $"{config.ApiHost}/{config.ApiBasePath}",
>>>>>>> Addressing code review comments.
                }
            };

            if (createdDoc)
                return Created(selfUri, result);
            else
                return Ok(result);
        }
    }
}
