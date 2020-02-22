<<<<<<< HEAD
using System.IO;
using Newtonsoft.Json.Linq;
=======
﻿using System;
>>>>>>> Adding get document by id.

namespace OnBaseDocsApi.Models
{
    public class ApiConfig
    {
<<<<<<< HEAD
<<<<<<< HEAD
        public string ApiBasePath { get; private set; }
        public string ApiHost { get; private set; }
        public string ServiceUrl { get; private set; }
        public string DataSource { get; private set; }
        public string DocIndexKeyName { get; private set; }
        public string StagingDocType { get; private set; }

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
=======
        public string ApiBasePath { get; set; }
        public string ApiHost { get; set; }
        public string ServiceUrl { get; set; }
        public string DataSource { get; set; }
        public string DocIndexKeyName { get; set; }
        public string StagingDocType { get; set; }
>>>>>>> Adding support for document upload.
=======
        public string ApiUri { get; set; }
        public string ApiHost { get; set; }
        public string ServiceUrl { get; set; }
        public string DataSource { get; set; }
>>>>>>> Adding get document by id.
    }
}
