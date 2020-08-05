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

        protected override bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            return SubscribeImpl?.Invoke(symbols, tickType) == true;
        }

        protected override bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            return UnsubscribeImpl?.Invoke(symbols, tickType) == true;
        }

        protected override string ChannelNameFromTickType(TickType tickType)
        {
            return GetChannelName?.Invoke(tickType);
        }
    }
}
