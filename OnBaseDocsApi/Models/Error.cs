using System;

namespace OnBaseDocsApi.Models
{
    public class Error
    {
        public string Status { get; set; }
        public string Title { get; set; }
        public string Code { get; set; }
        public string Detail { get; set; }
        public ErrorLinks Links { get; set; }
    }
}
