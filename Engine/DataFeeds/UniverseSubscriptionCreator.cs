using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Creates subscriptions for universe selection
    /// </summary>
    public class UniverseSubscriptionCreator
    {
        private readonly IResultHandler _resultHandler;

        public UniverseSubscriptionCreator(IResultHandler resultHandler)
        {
            _resultHandler = resultHandler;
        }

        public Subscription Create(Universe universe, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            // grab the relevant exchange hours
            SubscriptionDataConfig config = universe.Configuration;

            var exchangeHours = SecurityExchangeHoursProvider.FromDataFolder()
                .GetExchangeHours(config.Market, null, config.SecurityType);

            // create a canonical security object
            var security = new Security(exchangeHours, config, universe.SubscriptionSettings.Leverage);

            var localStartTime = startTimeUtc.ConvertFromUtc(config.TimeZone);
            var localEndTime = endTimeUtc.ConvertFromUtc(config.TimeZone);

            // define our data enumerator
            IEnumerator<BaseData> enumerator;

            var tradeableDates = Time.EachTradeableDay(security, localStartTime, localEndTime);

            var userDefined = universe as UserDefinedUniverse;
            if (userDefined != null)
            {
                // spoof a tick on the requested interval to trigger the universe selection function
                enumerator = LinqExtensions.Range(localStartTime, localEndTime, dt => dt + userDefined.Interval)
                    .Where(dt => security.Exchange.IsOpenDuringBar(dt, dt + userDefined.Interval, config.ExtendedMarketHours))
                    .Select(dt => new Tick {Time = dt}).GetEnumerator();
            }
            else
            {
                // normal reader for all others
                enumerator = new SubscriptionDataReader(config, localStartTime, localEndTime, _resultHandler, tradeableDates, false);
            }

            // create the subscription
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(security.SubscriptionDataConfig.TimeZone, startTimeUtc, endTimeUtc);
            var subscription = new Subscription(universe, security, enumerator, timeZoneOffsetProvider, startTimeUtc, endTimeUtc, true);

            return subscription;
        }
    }

}
