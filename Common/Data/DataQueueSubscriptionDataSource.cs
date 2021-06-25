using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data
{
    internal class DataQueueSubscriptionDataSource : SubscriptionDataSource
    {
        public DataQueueSubscriptionDataSource()
            : base(null, string.Empty, SubscriptionTransportMedium.Streaming)
        {

        }
    }
}
