using System;

namespace OnBaseDocsApi.Models
{
    public class DataResource<T>
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public DataLinks Links { get; set; }
        public T Attributes { get; set; }
    }
}
