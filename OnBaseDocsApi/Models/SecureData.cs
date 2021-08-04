using System;

namespace OnBaseDocsApi.Models
{
    public class SecureData
    {
        public long DocId { get; set; }
        public long OsuId { get; set; }
        public DateTime ActiveStart { get; set; }
        public DateTime ActiveEnd { get; set; }
    }
}
