using System;

namespace OnBaseDocsApi.Models
{
    public class HealthCheck
    {
        public HealthCheckMeta Meta { get; set; }
    }
}
/*
    meta: {
            name: openapi.info.title,
            time: now.format('YYYY-MM-DD HH:mm:ssZZ'),
            unixTime: now.unix(),
            commit: commit.trim(),
            documentation: 'openapi.yaml',
        }
      */
