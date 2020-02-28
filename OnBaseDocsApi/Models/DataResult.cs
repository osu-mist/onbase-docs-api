using System;

namespace OnBaseDocsApi.Models
{
    public class DataResult<T>
    {
        public DataResource<T> Data { get; set; }
        public DataLinks Links { get; set; }
    }
}
