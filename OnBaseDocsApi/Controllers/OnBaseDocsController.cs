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
    [VerifyProfileHeaderAttribute]
    public class OnBaseDocsController : BaseApiController
    {
        const string DefaultIndexKey = "";
        const string DefaultTypeGroup = "";
        const long DefaultStartDocId = 0;
        const int DefaultPageSize = 25;

        [HttpGet]
        [ActionName("")]
        public IHttpActionResult Get(long id)
        {
            return TryHandleDocRequest(id, (_, doc) =>
            {
                return DocumentResult(doc, false);
            });
        }

        [HttpGet]
        [ActionName("File")]
        public IHttpActionResult GetFile(long id)
        {
            return TryHandleDocRequest(id, (app, doc) =>
            {
                var pdf = app.Core.Retrieval.PDF.GetDocument(
                    doc.DefaultRenditionOfLatestRevision);

                using (var stream = new MemoryStream())
                {
                    pdf.Stream.CopyTo(stream);

                    /**
                     * OnBase adds extra zero bytes to the end of the PDF stream.
                     * Remove them because it causes Acrobat Reader to consider the
                     * PDF corrupt.
                     */
                    var result = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(TrimAfterEOF(stream.GetBuffer()))
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

        byte[] TrimAfterEOF(byte[] content)
        {
            int eofPos = content.Length;

            for (int i = eofPos - 1; content[i] == 0; i--)
                eofPos = i;

            return content.Take(eofPos).ToArray();
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
            string docExtension = null;
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
                        if ((docStream != null) || !string.IsNullOrWhiteSpace(docExtension))
                            return BadRequestResult("More than one document content was included.");

                        var fileName = content.Headers.ContentDisposition.FileName.Trim('"');
                        docStream = content.ReadAsStreamAsync().Result;
                        docExtension = Path.GetExtension(fileName).TrimStart('.');
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

            if ((docStream == null) || string.IsNullOrWhiteSpace(docExtension))
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
                // Validate that the doc type is actually valid.
                var docType = app.Core.DocumentTypes.Find(docAttr.DocumentType);
                if (docType == null)
                    errors.Add(BadRequestError($"The DocumentType '{docAttr.DocumentType}' could not be found."));

                var createAttr = new DocumentCreateAttributes
                {
                    DocumentType = Global.Config.StagingDocType,
                    Comment = docAttr.Comment,
                    IndexKey = docAttr.IndexKey,
                    Keywords = docAttr.Keywords,
                    FileType = docAttr.FileType ?? docExtension,
                    FileExtension = docExtension,
                    Stream = docStream,
                };
                var error = CreateDocument(errors, app, createAttr, out var doc);
                if (error != null)
                    return error;

                // We must get the document ID from this thread and not the task thread since
                // the document could go out of scope before the task starts.
                var docId = doc.ID;
                Task.Run(() =>
                {
                    MoveDocumentToWorkflow(docId, docType);
                });

                return DocumentResult(doc, true);
            });
        }

        // We can't use [FromUri] to auto bind the query parameters because
        // the binding does not work with '[' and ']'. So we must manually
        // bind them.
        [HttpGet]
        [ActionName("")]
        public IHttpActionResult ListDocs()
        {
            var parms = new ParamCollection(Request.RequestUri.ParseQueryString());
            var indexKey = parms.Get("filter[indexKey]", DefaultIndexKey);
            var typeGroup = parms.Get("filter[typeGroup]", DefaultTypeGroup);
            var startDocId = parms.Get("filter[startDocId]", DefaultStartDocId);
            var pageSize = parms.Get("filter[pageSize]", DefaultPageSize);
            // We bundle the bad requests in one response.
            var badRequestErrors = new List<Error>();

            if (string.IsNullOrWhiteSpace(indexKey))
                badRequestErrors.Add(BadRequestError("The required parameter filter[indexKey] is missing."));

            var config = Global.Config;

            return TryHandleRequest(app =>
            {
                DocumentTypeGroup docTypeGroup = null;
                if (!string.IsNullOrWhiteSpace(typeGroup))
                {
                    docTypeGroup = app.Core.DocumentTypeGroups.Find(typeGroup);
                    if (docTypeGroup == null)
                        badRequestErrors.Add(BadRequestError($"The document type group '{typeGroup}' could not be found."));
                }

                var query = app.Core.CreateDocumentQuery();
                if (query == null)
                    badRequestErrors.Add(BadRequestError("Unable to create document query."));

                // Check if there are any bad request errors. If there are then return them.
                if (badRequestErrors.Any())
                    return BadRequestResult(badRequestErrors);

                if (docTypeGroup != null)
                    query.AddDocumentTypeGroup(docTypeGroup);
                if (!string.IsNullOrWhiteSpace(indexKey))
                    query.AddKeyword(config.DocIndexKeyName, indexKey);

                // The OnBase API does not support a method for paging. The closest
                // we can get is to use a starting document ID.
                query.AddSort(DocumentQuery.SortAttribute.DocumentID, true);
                query.AddDocumentRange(startDocId, long.MaxValue);
                var queryResults = query.Execute(pageSize);
                if (queryResults == null)
                    return InternalErrorResult("Document query returned null.");

                var docs = new List<DataResource<DocumentAttributes>>();
                foreach (var doc in queryResults)
                {
                    docs.Add(DocumentResource(doc));
                }

                // Generate the query string for this request.
                var builder = new QueryStringBuilder();
                builder.Add("filter[indexKey]", indexKey, DefaultIndexKey);
                builder.Add("filter[typeGroup]", typeGroup, DefaultTypeGroup);
                builder.Add("filter[startDocId]", startDocId, DefaultStartDocId);
                builder.Add("filter[pageSize]", pageSize, DefaultPageSize);
                var queryStr = builder.ToString();

                return Ok(new ListResult<DocumentAttributes>
                {
                    Data = docs,
                    Links = new DataLinks
                    {
                        Self = $"{config.ApiHost}/{config.ApiBasePath}{queryStr}",
                    }
                });
            });
        }

        /// <summary>
        /// CreateDocument returns an error result when an error occurs; null otherwise. If CreateDocument
        /// return null, then the out parameter doc is guaranteed to be valid.
        /// </summary>
        /// <returns>An error result or null. A null result indicates that the document was created successfully.</returns>
        IHttpActionResult CreateDocument(List<Error> errors, Application app, DocumentCreateAttributes attr, out Document doc)
        {
            doc = null;

            var docType = app.Core.DocumentTypes.Find(attr.DocumentType);
            if (docType == null)
                errors.Add(BadRequestError($"The DocumentType '{attr.DocumentType}' could not be found."));

            var fileType = app.Core.FileTypes.Find(attr.FileType);
            if (fileType == null)
                errors.Add(BadRequestError($"The FileType '{attr.FileType}' could not be found."));

            var pageData = app.Core.Storage.CreatePageData(attr.Stream, attr.FileExtension);
            if (pageData == null)
                errors.Add(BadRequestError($"Unable to create page data for '{attr.FileExtension}'."));

            var props = app.Core.Storage.CreateStoreNewDocumentProperties(docType, fileType);
            if (props == null)
                return BadRequestResult($"Unable to create document properties for '{attr.DocumentType}' and '{attr.FileType}'.");

            if (errors.Any())
                return BadRequestResult(errors);

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
            Request.Properties.TryGetValue("Profile", out var profile);

            try
            {
                using (var app = Global.Profiles.LogIn(profile as string))
                {
                    return handler(app);
                }
            }
            catch (InvalidOperationException ex)
                when (ex.Message.Contains("User does not have rights"))
            {
                return AccessDeniedResult(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalErrorResult(ex.Message);
            }
        }

        IHttpActionResult DocumentResult(Document doc, bool createdDoc)
        {
            var config = Global.Config;

            var endpointUri = $"{config.ApiHost}/{config.ApiBasePath}";
            var selfUri = $"{endpointUri}/{doc.ID}";

            var result = new DataResult<DocumentAttributes>
            {
                Data = DocumentResource(doc),
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

        DataResource<DocumentAttributes> DocumentResource(Document doc)
        {
            var keywords = doc.KeywordRecords
                .SelectMany(x => x.Keywords.Select(k =>
                    new Keyword
                    {
                        Name = k.KeywordType.Name,
                        Value = k.Value.ToString()
                    }))
                .ToArray();

            return new DataResource<DocumentAttributes>
            {
                ID = doc.ID.ToString(),
                Type = "onbaseDocument",
                Attributes = new DocumentAttributes
                {
                    CreatedBy = doc.CreatedBy?.ID ?? 0,
                    DateStored = doc.DateStored,
                    DocumentDate = doc.DocumentDate,
                    Status = doc.Status.ToString(),
                    Name = doc.Name,
                    DocumentType = doc.DocumentType?.Name ?? null,
                    DefaultFileType = doc.DefaultFileType?.Name ?? null,
                    LatestAllowedRevisionID = doc.LatestAllowedRevisionID,
                    Keywords = keywords,
                }
            };
        }
    }
}
