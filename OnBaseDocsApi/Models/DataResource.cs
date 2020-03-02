using System;

namespace OnBaseDocsApi.Models
{
    public class DataResource<T>
    {
        public string ID { get; set; }
        public string Type { get; set; }
        public DataLinks Links { get; set; }
        public T Attributes { get; set; }
    }
}
