using System;

namespace OnBaseDocsApi.Models
{
    public class ErrorResource : Error
    {
        public string Code { get; set; }
        public ErrorLinks Links { get; set; }
    }
}
