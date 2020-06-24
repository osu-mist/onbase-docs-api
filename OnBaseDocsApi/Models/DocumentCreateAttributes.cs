using System;
using System.IO;

namespace OnBaseDocsApi.Models
{
    public class DocumentCreateAttributes : DocumentPostAttributes
    {
        public Stream Stream { get; set; }
        public bool ToStaging { get; set; }
    }
}
