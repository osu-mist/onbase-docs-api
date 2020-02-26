using System;
using System.Collections.Generic;

namespace OnBaseDocsApi.Models
{
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
    public class DocumentPostAttributes
    {
        public string DocumentType { get; set; }
        public string Comment { get; set; }
        public string IndexKey { get; set; }
=======
    public class DocumentAttributes
=======
    public class DocumentPostAttributes
>>>>>>> Adding support for document upload.
    {
        public string DocumentType { get; set; }
<<<<<<< HEAD
=======
    public class DocumentAttributes
    {
        public long CreatedBy { get; set; }
        public DateTime DateStored { get; set; }
        public DateTime DocumentDate { get; set; }
        public string DocumentType { get; set; }
>>>>>>> Adding support for document upload.
        public string Status { get; set; }
        public string Name { get; set; }
        public long ID { get; set; }
        public long LatestAllowedRevisionID { get; set; }
        public string DefaultFileType { get; set; }
<<<<<<< HEAD
>>>>>>> Adding support for document upload.
=======
        public string Comment { get; set; }
        public string IndexKey { get; set; }
>>>>>>> Adding support for document upload.
=======
>>>>>>> Adding support for document upload.
        public IEnumerable<Keyword> Keywords { get; set; }
    }
}
