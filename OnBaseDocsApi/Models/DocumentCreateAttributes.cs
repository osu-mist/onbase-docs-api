using System;
using System.IO;

namespace OnBaseDocsApi.Models
{
    public class DocumentCreateAttributes : DocumentPostAttributes
    {
        public string Ext { get; set; }
        public Stream Stream { get; set; }
        public bool ToStaging { get; set; }
    }
}
