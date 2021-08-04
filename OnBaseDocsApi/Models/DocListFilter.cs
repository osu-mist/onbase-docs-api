using System;

namespace OnBaseDocsApi.Models
{
    public class DocListFilter
    {
        public string IndexKey { get; set; }
        public string DocTypeGroup { get; set; }
        public string DocType { get; set; }
        public long StartDocId { get; set; }
        public int PageSize { get; set; }
        public string KeywordsHasAll { get; set; }
    }
}
