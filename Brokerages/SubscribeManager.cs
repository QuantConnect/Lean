using QuantConnect.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages
{
    public class SubscribeManager
    {
        public virtual void Subscribe(SubscriptionDataConfig dataConfig) { }

        public virtual void Unsubscribe(SubscriptionDataConfig dataConfig) { }
    }
}
