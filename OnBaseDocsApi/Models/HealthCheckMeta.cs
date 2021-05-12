using System;

namespace OnBaseDocsApi.Models
{
    public class HealthCheckMeta
    {
        public string Name { get; set; }
        public string Time { get; set; }
        public UInt64 UnixTime { get; set; }
        public string Commit { get; set; }
        public string Documentation { get; set; }
    }
}

