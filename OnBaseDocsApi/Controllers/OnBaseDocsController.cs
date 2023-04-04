using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Security.Cryptography;
using System.Text;
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
        const string DefaultDocType = "";
        const string DefaultKeywords = "";
        const long DefaultStartDocId = 0;
        const int DefaultPageSize = 25;
        const int DefaultTimeToLive = 24;
        const int MaxHashLen = 10 * 1024;

        static readonly byte[] HashSalt;
        static readonly string HashSecret;

        static OnBaseDocsController()
        {
            var config = Global.Config.Hash;

            HashSalt = Encoding.ASCII.GetBytes(config.Salt);
            HashSecret = config.Secret;
        }

        [HttpGet]
        [ActionName("")]
        public IHttpActionResult Get(long id)
        {
            return TryHandleDocRequest(id, (_, doc) =>
            {
                return DocumentResult(doc, id.ToString());
            });
        }

        [HttpGet]
        [ActionName("Secure")]
        public IHttpActionResult SecureGet(string hashId, [FromUri]long osuId)
        {
            var json = TryDecryptString(hashId, $"{HashSecret}:{osuId}");
            if (string.IsNullOrWhiteSpace(json))
                return ForbiddenResult("Access to this document is not allowed.");

            var data = JsonConvert.DeserializeObject<SecureData>(json);

            /*
             * The OSU ID should match or the decryption would fail.
             * Just in case, validate OSU ID.
             */
            if (data.OsuId != osuId)
                return ForbiddenResult("The user does not have access to this document.");

            var now = DateTime.UtcNow;
            if (!((data.ActiveStart <= now) && (now <= data.ActiveEnd)))
                return ForbiddenResult("Access to this document has expired.");

            return TryHandleDocRequest(data.DocId, (_, doc) =>
            {
                return DocumentResult(doc, hashId);
            });
        }

        [HttpGet]
        [ActionName("File")]
        public IHttpActionResult GetFile(long id)
        {
            return GetDocFile(id);
        }

        [HttpGet]
        [ActionName("SecureFile")]
        public IHttpActionResult SecureGetFile(string hashId, [FromUri] long osuId)
        {
            var json = TryDecryptString(hashId, $"{HashSecret}:{osuId}");
            if (string.IsNullOrWhiteSpace(json))
                return ForbiddenResult("Access to this document is not allowed.");

            var data = JsonConvert.DeserializeObject<SecureData>(json);

            /*
             * The OSU ID should match or the decryption would fail.
             * Just in case, validate OSU ID.
             */
            if (data.OsuId != osuId)
                return ForbiddenResult("The user does not have access to this document.");

            var now = DateTime.UtcNow;
            if (!((data.ActiveStart <= now) && (now <= data.ActiveEnd)))
                return ForbiddenResult("Access to this document has expired.");

            return GetDocFile(data.DocId);
        }

        IHttpActionResult GetDocFile(long docId)
        {
            return TryHandleDocRequest(docId, (app, doc) =>
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

                        // Handle base64 file encoding.
                        if (content.Headers.ContentEncoding.Any(x => x == "base64")
                            || content.Headers.Any(x => x.Key == "Content-Transfer-Encoding" && x.Value.Any(v => v == "base64")))
                        {
                            /*
                             * We need to read the file content, bease64 decode it and set
                             * docStream to be the decoded content.
                             */
                            using (var reader = new StreamReader(docStream))
                            {
                                var str = reader.ReadToEnd();
                                docStream = new MemoryStream(Convert.FromBase64String(str));
                            }
                        }
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
                errors.Add(BadRequestError($"JSON parse error. {ex}"));
            }
            catch (IOException ex) when (ex.Message.Contains("MIME multipart message is not complete"))
            {
                errors.Add(BadRequestError(ex.ToString()));
            }
            catch (Exception ex)
            {
                return InternalErrorResult(ex.ToString());
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

                return DocumentResult(doc, docId.ToString(), docAttr.DocumentType);
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

            var filter = new DocListFilter
            {
                IndexKey = parms.Get("filter[indexKey]", DefaultIndexKey),
                DocTypeGroup = parms.Get("filter[typeGroup]", DefaultTypeGroup),
                DocType = parms.Get("filter[type]", DefaultDocType),
                StartDocId = parms.Get("filter[startDocId]", DefaultStartDocId),
                PageSize = parms.Get("filter[pageSize]", DefaultPageSize),
                KeywordsHasAll = parms.Get("filter[keywords][hasAll]", DefaultKeywords),
            };

            return ListDocs<DocumentAttributes>(filter, d => DocumentResource(d));
        }

        // We can't use [FromUri] to auto bind the query parameters because
        // the binding does not work with '[' and ']'. So we must manually
        // bind them.
        [HttpGet]
        [ActionName("Secure")]
        public IHttpActionResult SecureListDocs()
        {
            var parms = new ParamCollection(Request.RequestUri.ParseQueryString());

            var osuId = parms.Get("osuId", 0);
            if (osuId == 0)
                return BadRequest("A valid OSUID is required.");

            var timeToLive = parms.Get("timeToLive", DefaultTimeToLive);

            long startDocId = 0;
            var startDocHash = parms.Get("filter[startDocHash]", DefaultStartDocId);

            var filter = new DocListFilter
            {
                IndexKey = parms.Get("filter[indexKey]", DefaultIndexKey),
                DocTypeGroup = parms.Get("filter[docTypeGroup]", DefaultTypeGroup),
                DocType = parms.Get("filter[docType]", DefaultDocType),
                StartDocId = startDocId,
                PageSize = parms.Get("filter[pageSize]", DefaultPageSize),
                KeywordsHasAll = parms.Get("filter[keywords][hasAll]", DefaultKeywords),
            };

            return ListDocs<SecureDocumentAttributes>(filter, d => SecureDocumentResource(osuId, timeToLive, d));
        }

        IHttpActionResult ListDocs<T>(DocListFilter filter, Func<Document, DataResource<T>> resourceFactory)
        {
            // We bundle the bad requests in one response.
            var badRequestErrors = new List<Error>();

            //
            // Read the keywords.
            //
            var filterKeywords = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(filter.KeywordsHasAll))
            {
                foreach (var kw in filter.KeywordsHasAll.Split('|'))
                {
                    var parts = kw.Split(':');
                    if (parts.Length == 2)
                    {
                        filterKeywords[parts[0]] = parts[1];
                    }
                    else
                    {
                        badRequestErrors.Add(BadRequestError("The filter[keywords][hasAll] parameter is not valid."));
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(filter.IndexKey) && string.IsNullOrWhiteSpace(filter.DocType))
                badRequestErrors.Add(BadRequestError("One of filter[indexKey] or filter[type] parameters is required."));

            var config = Global.Config;

            return TryHandleRequest(app =>
            {
                DocumentTypeGroup docTypeGroup = null;
                if (!string.IsNullOrWhiteSpace(filter.DocTypeGroup))
                {
                    docTypeGroup = app.Core.DocumentTypeGroups.Find(filter.DocTypeGroup);
                    if (docTypeGroup == null)
                        badRequestErrors.Add(BadRequestError($"The document type group '{filter.DocTypeGroup}' could not be found."));
                }

                DocumentType docType = null;
                if (!string.IsNullOrWhiteSpace(filter.DocType))
                {
                    docType = app.Core.DocumentTypes.Find(filter.DocType);
                    if (docType == null)
                        badRequestErrors.Add(BadRequestError($"The document type '{filter.DocType}' could not be found."));
                }

                var query = app.Core.CreateDocumentQuery();
                if (query == null)
                    badRequestErrors.Add(BadRequestError("Unable to create document query."));

                // Check if there are any bad request errors. If there are then return them.
                if (badRequestErrors.Any())
                    return BadRequestResult(badRequestErrors);

                if (docTypeGroup != null)
                    query.AddDocumentTypeGroup(docTypeGroup);
                if (docType != null)
                    query.AddDocumentType(docType);
                if (!string.IsNullOrWhiteSpace(filter.IndexKey))
                    query.AddKeyword(config.DocIndexKeyName, filter.IndexKey);

                // Add the keywords to the query.
                foreach (var keyword in filterKeywords)
                    query.AddKeyword(keyword.Key, keyword.Value);

                // The OnBase API does not support a method for paging. The closest
                // we can get is to use a starting document ID.
                query.AddSort(DocumentQuery.SortAttribute.DocumentID, true);
                query.AddDocumentRange(filter.StartDocId, long.MaxValue);
                var queryResults = query.Execute(filter.PageSize);
                if (queryResults == null)
                    return InternalErrorResult("Document query returned null.");

                var docs = new List<DataResource<T>>();
                foreach (var doc in queryResults)
                {
                    docs.Add(resourceFactory(doc));
                }

                // Generate the query string for this request.
                var builder = new QueryStringBuilder();
                builder.Add("filter[indexKey]", filter.IndexKey, DefaultIndexKey);
                builder.Add("filter[docTypeGroup]", filter.DocTypeGroup, DefaultTypeGroup);
                builder.Add("filter[docType]", filter.DocType, DefaultDocType);
                builder.Add("filter[startDocId]", filter.StartDocId, DefaultStartDocId);
                builder.Add("filter[pageSize]", filter.PageSize, DefaultPageSize);
                builder.Add("filter[keywords][hasAll]", filter.KeywordsHasAll, DefaultKeywords);
                var queryStr = builder.ToString();

                return Ok(new ListResult<T>
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
                return AccessDeniedResult(ex.ToString());
            }
            catch (Exception ex)
            {
                return InternalErrorResult(ex.ToString());
            }
        }

        IHttpActionResult DocumentResult(Document doc, string docId, string createdDocType = null)
        {
            var config = Global.Config;

            var endpointUri = $"{config.ApiHost}/{config.ApiBasePath}";
            var selfUri = $"{endpointUri}/{doc.ID}";
            var docResource = DocumentResource(doc, docId);

            if (!string.IsNullOrWhiteSpace(createdDocType))
                docResource.Attributes.ReindexDocumentType = createdDocType;

            var result = new DataResult<DocumentAttributes>
            {
                Data = docResource,
                Links = new DataLinks
                {
                    Self = endpointUri,
                }
            };

            if (string.IsNullOrWhiteSpace(createdDocType))
                return Ok(result);
            else
                return Created(selfUri, result);
        }

        DataResource<DocumentAttributes> DocumentResource(Document doc, string docId = null)
        {
            var keywords = doc.KeywordRecords
                .SelectMany(x => x.Keywords.Select(k =>
                    new Keyword
                    {
                        Name = k.KeywordType.Name,
                        Value = k.Value.ToString()
                    }))
                .ToArray();

            if (string.IsNullOrEmpty(docId))
                docId = doc.ID.ToString();

            return new DataResource<DocumentAttributes>
            {
                ID = docId,
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

        DataResource<SecureDocumentAttributes> SecureDocumentResource(long osuId, int timeToLive, Document doc)
        {
            var now = DateTime.UtcNow;

            var data = new SecureData
            {
                DocId = doc.ID,
                OsuId = osuId,
                ActiveStart = now,
                ActiveEnd = now.AddHours(timeToLive),
            };

            string json = JsonConvert.SerializeObject(data);

            return new DataResource<SecureDocumentAttributes>
            {
                ID = EncryptString(json, $"{HashSecret}:{osuId}"),
                Type = "onbaseSecureDocument",
                Attributes = new SecureDocumentAttributes
                {
                    Name = doc.Name,
                }
            };
        }

        /// <summary>
        /// Encrypt the given string using AES.  The string can be decrypted using DecryptString.
        /// </summary>
        /// <param name="str">The text to encrypt.</param>
        /// <param name="secret">A secret used to generate a key for encryption.</param>
        static string EncryptString(string str, string secret)
        {
            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentNullException("str");
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentNullException("secret");

            string result = null;

            // Generate the key from the shared secret and the salt.
            var key = new Rfc2898DeriveBytes(secret, HashSalt);

            // Create a RijndaelManaged object.
            var aesAlg = new RijndaelManaged();
            aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

            // Create a encryptor to perform the transform.
            var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            // Encrypt the data.
            using (var stream = new MemoryStream())
            {
                // Prepend the IV. The IV is needed for decryption.
                stream.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                stream.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                using (var cryptStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
                {
                    using (var writer = new StreamWriter(cryptStream))
                    {
                        writer.Write(str);
                    }
                }

                //result = Convert.ToBase64String(stream.ToArray());
                result = System.Web.HttpServerUtility.UrlTokenEncode(stream.ToArray());
            }

            // Return the encrypted bytes from the memory stream.
            return result;
        }

        /// <summary>
        /// Decrypt the given string.  Assumes the string was encrypted using EncryptString.
        /// </summary>
        /// <param name="cipher">The text to decrypt.</param>
        /// <param name="secret">A secret used to generate a key for decryption.</param>
        static string DecryptString(string cipher, string secret)
        {
            if (string.IsNullOrWhiteSpace(cipher))
                throw new ArgumentNullException("cipher");
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentNullException("secret");

            string result = null;

            // Generate the key from the shared secret and the salt.
            var key = new Rfc2898DeriveBytes(secret, HashSalt);

            // Create the streams used for decryption.
            byte[] bytes = System.Web.HttpServerUtility.UrlTokenDecode(cipher);

            using (var stream = new MemoryStream(bytes))
            {
                // Create a RijndaelManaged object.
                var aesAlg = new RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                aesAlg.IV = ReadHashData(stream);

                // Create a decrytor to perform the transform.
                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var decryptStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
                {
                    using (var reader = new StreamReader(decryptStream))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }

            return result;
        }

        static string TryDecryptString(string cipher, string secret)
        {
            try
            {
                return DecryptString(cipher, secret);
            }
            catch
            {
                return null;
            }
        }

        static byte[] ReadHashData(Stream s)
        {
            byte[] rawLength = new byte[sizeof(int)];
            if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
                throw new SystemException("The hash is not valid.");

            var len = BitConverter.ToInt32(rawLength, 0);
            if ((len <= 0) || (len > MaxHashLen))
                throw new SystemException($"Invalid hash size ({len}).");

            byte[] buffer = new byte[len];
            if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
                throw new SystemException("The hash is not valid.");

            return buffer;
        }
    }
}
