using System;
using System.Collections.Generic;

namespace OnBaseDocsApi.Models
{
    public class ListResult<T>
    {
        public IEnumerable<DataResource<T>> Data { get; set; }
        public DataLinks Links { get; set; }
    }
}
