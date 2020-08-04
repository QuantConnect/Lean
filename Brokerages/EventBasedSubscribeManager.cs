using QuantConnect.Data;
using QuantConnect.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
            try
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
            catch (Exception exception)
            {
                Log.Error(exception);
                throw;
            }
        }

        public override void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            try
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
            catch (Exception exception)
            {
                Log.Error(exception);
                throw;
            }
        }

        private Channel GetChannel(SubscriptionDataConfig dataConfig)
        {
            var channels = _subscribersByChannel.Keys;
            var channelName = GetChannelName(dataConfig.TickType);

            var channel = Find(dataConfig.Symbol, dataConfig.TickType);
            return channel ?? new Channel() { Symbol = dataConfig.Symbol.ID.ToString(), Name = channelName };
        }

        internal bool IsSubscribed(Symbol symbol, TickType tickType)
        {
            var channel = Find(symbol, tickType);
            return channel != null;
        }

        private Channel Find(Symbol symbol, TickType tickType)
        {
            var channels = _subscribersByChannel.Keys;
            var channelName = GetChannelName(tickType);

            return channels.FirstOrDefault(c => c.Symbol == symbol.ID.ToString() && c.Name == channelName);
        }
    }
}
