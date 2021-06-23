using QuantConnect.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data
{
    public class RestSubscriptionDataSource : WebSubscriptionDataSource
    {
        public string Source { get; }

        public RestSubscriptionDataSource(string source, bool isLiveMode, IEnumerable<KeyValuePair<string, string>> headers = null, bool returnsACollection = false)
            : base(_ => new Transport.RestSubscriptionStreamReader(source, headers, isLiveMode), source, returnsACollection ? FileFormat.Collection : FileFormat.Csv)
        {
            Source = source;
        }
    }
}
