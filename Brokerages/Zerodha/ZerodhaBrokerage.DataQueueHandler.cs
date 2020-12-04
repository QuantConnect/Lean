using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Packets;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// ZerodhaBrokerage: IDataQueueHandler implementation
    /// </summary>
    public partial class ZerodhaBrokerage
    {
        #region IDataQueueHandler implementation

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            var symbol = dataConfig.Symbol;
            if (CanSubscribe(symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }


        /// <summary>
        /// Returns true if this data provide can handle the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to be handled</param>
        /// <returns>True if this data provider can get data for the symbol, false otherwise</returns>
        private static bool CanSubscribe(Symbol symbol)
        {
            var market = symbol.ID.Market;
            var securityType = symbol.ID.SecurityType;
            if (symbol.Value.IndexOfInvariant("universe", true) != -1) return false;
            // Include future options as a special case with no matching market, otherwise
            // our subscriptions are removed without any sort of notice.
            return
                (securityType == SecurityType.Equity ||
                securityType == SecurityType.Option||
                securityType == SecurityType.Future);
        }
        #endregion
    }
}
