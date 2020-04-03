using System.Collections.Generic;

namespace OnBaseDocsApi.Models
{
    public class ApiConfig
    {
        public string ApiBasePath { get; set; }
        public string ApiHost { get; set; }
        public string ServiceUrl { get; set; }
        public string DataSource { get; set; }
        public string DocIndexKeyName { get; set; }
        public string StagingDocType { get; set; }
        public Dictionary<string, Credentials> Profiles { get; set; }
    }
}
