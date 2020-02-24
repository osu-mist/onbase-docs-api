using System;

namespace OnBaseDocsApi.Models
{
    public class DataResult<T>
    {
        public T Data { get; set; }
        public DataLinks Links { get; set; }
    }
}
