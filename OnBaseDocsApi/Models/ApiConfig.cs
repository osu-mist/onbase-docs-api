using System.IO;
using Newtonsoft.Json.Linq;

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

        public ApiConfig(string path)
        {
            var config = JObject.Parse(File.ReadAllText(path));

            ApiBasePath = config.Value<string>("apiBasePath");
            ApiHost = config.Value<string>("apiHost");
            ServiceUrl = config.Value<string>("serviceUrl");
            DataSource = config.Value<string>("dataSource");
            DocIndexKeyName = config.Value<string>("docIndexKeyName");
            StagingDocType = config.Value<string>("stagingDocType");
        }
    }
}
