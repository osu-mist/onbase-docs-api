using System;
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
    }
}
