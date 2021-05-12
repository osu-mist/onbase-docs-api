using System;
using System.Collections.Generic;
using System.Linq;

namespace OnBaseDocsApi.Models
{
    class QueryStringBuilder
    {
        readonly List<string> Parts = new List<string>();

        public void Add<T>(string name, T val, T defValue)
        {
            if (!val.Equals(defValue))
                Parts.Add($"{name}={val}");
        }

        public override string ToString()
        {
            return Parts.Any() ? "?" + string.Join("&", Parts) : string.Empty;
        }
    }
}
