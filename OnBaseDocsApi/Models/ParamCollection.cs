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
            if (value != null)
                return int.Parse(value);
            else
                return defValue;
        }

        public long Get(string name, long defValue)
        {
            var value = Params[name];
            if (value != null)
                return long.Parse(value);
            else
                return defValue;
        }

        public string Get(string name, string defValue)
        {
            var value = Params[name];
            if (value != null)
                return value;
            else
                return defValue;
        }
    }
}
