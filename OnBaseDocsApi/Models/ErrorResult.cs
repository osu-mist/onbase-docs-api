using System;
using System.Collections.Generic;

namespace OnBaseDocsApi.Models
{
    public class ErrorResult
    {
        public IEnumerable<ErrorResource> Errors { get; set; }
    }
}
