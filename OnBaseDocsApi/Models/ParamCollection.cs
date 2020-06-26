using System;
using System.Collections.Specialized;

namespace OnBaseDocsApi.Models
{
    class ParamCollection
    {
        readonly NameValueCollection Params;

        public ParamCollection(NameValueCollection parms)
        {
            Params = parms;
        }

        public int Get(string name, int defValue)
        {
            var value = Params[name];
            if (string.IsNullOrEmpty(value))
                return defValue;
            else
                return int.Parse(value);
        }

        public long Get(string name, long defValue)
        {
            var value = Params[name];
            if (string.IsNullOrEmpty(value))
                return defValue;
            else
                return long.Parse(value);
        }

        public string Get(string name, string defValue)
        {
            var value = Params[name];
            if (string.IsNullOrEmpty(value))
                return defValue;
            else
                return value;
        }
    }
}
