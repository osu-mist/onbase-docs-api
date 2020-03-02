using System;
using System.Collections.Generic;

namespace OnBaseDocsApi.Models
{
    public class DocumentAttributes
    {
        public long CreatedBy { get; set; }
        public DateTime DateStored { get; set; }
        public DateTime DocumentDate { get; set; }
        public string DocumentType { get; set; }
        public string Status { get; set; }
        public string Name { get; set; }
        public long LatestAllowedRevisionID { get; set; }
        public string DefaultFileType { get; set; }
        public IEnumerable<Keyword> Keywords { get; set; }
    }
}
