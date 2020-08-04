using QuantConnect.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages
{
    public abstract class SubscribeManager
    {
        public abstract void Subscribe(SubscriptionDataConfig dataConfig);

        public abstract void Unsubscribe(SubscriptionDataConfig dataConfig);
    }
}
