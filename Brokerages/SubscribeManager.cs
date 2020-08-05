using QuantConnect.Data;
using QuantConnect.Logging;
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

        protected ConcurrentDictionary<Channel, int> _subscribersByChannel = new ConcurrentDictionary<Channel, int>();

        public void Subscribe(SubscriptionDataConfig dataConfig)
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

                if (Subscribe(new[] { dataConfig.Symbol }, dataConfig.TickType))
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

        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            try
            {
                var channel = GetChannel(dataConfig);
                int count;
                if (_subscribersByChannel.TryGetValue(channel, out count))
                {
                    if (count > 1)
                    {
                        _subscribersByChannel.TryUpdate(channel, count - 1, count);
                        return;
                    }

                    if (Unsubscribe(new[] { dataConfig.Symbol }, dataConfig.TickType))
                    {
                        _subscribersByChannel.TryRemove(channel, out count);
                    } 
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                throw;
            }
        }

        protected abstract bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType);

        protected abstract bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType);

        protected abstract string ChannelNameFromTickType(TickType tickType);

        public bool IsSubscribed(Symbol symbol, TickType tickType)
        {
            var channel = Find(symbol, tickType);
            return channel != null;
        }

        private Channel GetChannel(SubscriptionDataConfig dataConfig)
        {
            var channels = _subscribersByChannel.Keys;
            var channelName = ChannelNameFromTickType(dataConfig.TickType);

            var channel = Find(dataConfig.Symbol, dataConfig.TickType);
            return channel ?? new Channel() { Symbol = dataConfig.Symbol.ID.ToString(), Name = channelName };
        }

        private Channel Find(Symbol symbol, TickType tickType)
        {
            var channels = _subscribersByChannel.Keys;
            var channelName = ChannelNameFromTickType(tickType);

            return channels.FirstOrDefault(c => c.Symbol == symbol.ID.ToString() && c.Name == channelName);
        }
    }
}
