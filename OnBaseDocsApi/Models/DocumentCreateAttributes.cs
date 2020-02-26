using System;
<<<<<<< HEAD
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
=======
using System.IO;
>>>>>>> Adding support for document upload.

namespace OnBaseDocsApi.Models
{
    public class DocumentCreateAttributes : DocumentPostAttributes
    {
<<<<<<< HEAD
        public string DocumentType { get; set; }
        public string Comment { get; set; }
        public string Name { get; set; }
        public string IndexKey { get; set; }
        public IEnumerable<Keyword> Keywords { get; set; }
>>>>>>> Adding support for document upload.
=======
        public string Ext { get; set; }
        public Stream Stream { get; set; }
        public bool ToStaging { get; set; }
>>>>>>> Adding support for document upload.
    }
}
