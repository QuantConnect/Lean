using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data
{
    public class RemoteFileSubscriptionDataSource : SubscriptionDataSource
    {
        public string Source { get; }

        public RemoteFileSubscriptionDataSource(string source, IEnumerable<KeyValuePair<string, string>> headers = null, FileFormat fileFormat = FileFormat.Csv)
            : base((dataCacheProvider) => new Transport.RemoteFileSubscriptionStreamReader(dataCacheProvider, source, Globals.Cache, headers), source, SubscriptionTransportMedium.RemoteFile, fileFormat)
        {
            Source = source;
        }
    }
}
