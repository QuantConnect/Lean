using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data
{
    public class LocalFileSubscriptionDataSource : SubscriptionDataSource
    {
        public LocalFileSubscriptionDataSource(string source, FileFormat fileFormat = FileFormat.Csv)
            : base((dataCacheProvider) => new Transport.LocalFileSubscriptionStreamReader(dataCacheProvider, source), source, SubscriptionTransportMedium.LocalFile,  fileFormat)
        {
        }
    }
}
