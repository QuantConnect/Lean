using QuantConnect.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages
{
    public class EventBasedSubscribeManager : SubscribeManager
    {
        public Func<IEnumerable<Symbol>, TickType, bool> SubscribeImpl;
        public Func<IEnumerable<Symbol>, TickType, bool> UnsubscribeImpl;

        public Func<TickType, string> GetChannelName;

        protected ConcurrentDictionary<Channel, int> _subscribersByChannel = new ConcurrentDictionary<Channel, int>();

        public override void Subscribe(SubscriptionDataConfig dataConfig)
        {
            var channel = GetChannel(dataConfig);
            int count;
            if (_subscribersByChannel.TryGetValue(channel, out count))
            {
                _subscribersByChannel.TryUpdate(channel, count + 1, count);
                return;
            }

            if (SubscribeImpl?.Invoke(new[] { dataConfig.Symbol }, dataConfig.TickType) == true)
            {
                _subscribersByChannel.AddOrUpdate(channel, 1);
            }
        }

        public override void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            var channel = GetChannel(dataConfig);
            int count;
            if (_subscribersByChannel.TryGetValue(channel, out count))
            {
                if (count == 1 && UnsubscribeImpl?.Invoke(new[] { dataConfig.Symbol }, dataConfig.TickType) == true)
                {
                    _subscribersByChannel.TryRemove(channel, out count);
                }
                else
                {
                    _subscribersByChannel.TryUpdate(channel, count - 1, count);
                }
            }
        }

        private Channel GetChannel(SubscriptionDataConfig dataConfig)
        {
            var channels = _subscribersByChannel.Keys;
            var channelName = GetChannelName(dataConfig.TickType);

            return channels.FirstOrDefault(c => c.Symbol == dataConfig.Symbol.ID.ToString() && c.Name == channelName)
                ?? new Channel() { Symbol = dataConfig.Symbol.ID.ToString(), Name = channelName };
        }
    }
}
