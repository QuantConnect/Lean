using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data
{
    /// <summary>
    /// Base data source for anything
    /// </summary>
    public class WebSubscriptionDataSource : SubscriptionDataSource
    {
        public WebSubscriptionDataSource(Func<IDataCacheProvider, IStreamReader> getStreamReader, string source, FileFormat format = FileFormat.Csv)
            :base(getStreamReader, source, SubscriptionTransportMedium.Web, format)
        {
        }
    }
}
