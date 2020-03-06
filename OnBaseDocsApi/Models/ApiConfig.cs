namespace OnBaseDocsApi.Models
{
    public class ApiConfig
    {
        public string ApiBasePath { get; private set; }
        public string ApiHost { get; private set; }
        public string ServiceUrl { get; private set; }
        public string DataSource { get; private set; }
        public string DocIndexKeyName { get; private set; }
        public string StagingDocType { get; private set; }
    }
}
