using System;
<<<<<<< HEAD
using System.IO;

namespace OnBaseDocsApi.Models
{
    public class DocumentCreateAttributes : DocumentPostAttributes
    {
        public string Ext { get; set; }
        public Stream Stream { get; set; }
        public bool ToStaging { get; set; }
=======
using System.Collections.Generic;

namespace OnBaseDocsApi.Models
{
    public class DocumentPostAttributes
    {
        public string DocumentType { get; set; }
        public string Comment { get; set; }
        public string Name { get; set; }
        public string IndexKey { get; set; }
        public IEnumerable<Keyword> Keywords { get; set; }
>>>>>>> Adding support for document upload.
    }
}
