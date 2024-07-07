/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect
{
    /// <summary>
    /// Per-symbol capacity estimations, tightly coupled with the <see cref="CapacityEstimate"/> class.
    /// </summary>
    internal class SymbolCapacity
    {
        /// <summary>
        /// The period for which a symbol trade influentiates capacity estimate
        /// </summary>
        public static TimeSpan CapacityEffectPeriod = TimeSpan.FromDays(30);

        /// <summary>
        /// An estimate of how much volume the FX market trades per minute
        /// </summary>
        /// <remarks>
        /// Any mentions of "dollar volume" are in account currency. They are not always in dollars.
        /// </remarks>
        private const decimal _forexMinuteVolume = 25000000m;

        /// <summary>
        /// An estimate of how much volume the CFD market trades per minute
        /// </summary>
        /// <remarks>
        /// This is pure estimation since we don't have CFD volume data. Based on 300k per day.
        /// </remarks>
        private const decimal _cfdMinuteVolume = 200m;
        private const decimal _fastTradingVolumeScalingFactor = 2m;

        private readonly IAlgorithm _algorithm;
        private readonly Symbol _symbol;

        private decimal _previousVolume;
        private DateTime? _previousTime;

        private bool _isInternal;
        private decimal _averageDollarVolume;
        private decimal _resolutionScaleFactor;
        private decimal _marketCapacityDollarVolume;
        private bool _resetMarketCapacityDollarVolume;
        private decimal _fastTradingVolumeDiscountFactor;
        private OrderEvent _previousOrderEvent;

        /// <summary>
        /// Total trades made in between snapshots
        /// </summary>
        public int Trades { get; private set; }

        /// <summary>
        /// The Symbol's Security
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// The absolute dollar volume (in account currency) we've traded
        /// </summary>
        public decimal SaleVolume { get; private set; }

        /// <summary>
        /// Market capacity dollar volume, i.e. the capacity the market is able to provide for this Symbol
        /// </summary>
        /// <remarks>
        /// Dollar volume is in account currency, but name is used for consistency with financial literature.
        /// </remarks>
        public decimal MarketCapacityDollarVolume =>
            _marketCapacityDollarVolume * _resolutionScaleFactor;

        /// <summary>
        /// Creates a new SymbolCapacity object, capable of determining market capacity for a Symbol
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="symbol"></param>
        public SymbolCapacity(IAlgorithm algorithm, Symbol symbol)
        {
            _algorithm = algorithm;
            Security = _algorithm.Securities[symbol];
            _symbol = symbol;

            _isInternal = _algorithm
                .SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(
                    symbol,
                    includeInternalConfigs: true
                )
                .All(config => config.IsInternalFeed);
        }

        /// <summary>
        /// New order event handler. Handles the aggregation of SaleVolume and
        /// sometimes resetting the <seealso cref="MarketCapacityDollarVolume"/>
        /// </summary>
        /// <param name="orderEvent">Parent class filters out other events so only fill events reach this method.</param>
        public void OnOrderEvent(OrderEvent orderEvent)
        {
            SaleVolume +=
                Security.QuoteCurrency.ConversionRate
                * orderEvent.FillPrice
                * orderEvent.AbsoluteFillQuantity
                * Security.SymbolProperties.ContractMultiplier;

            // To reduce the capacity of high frequency strategies, we scale down the
            // volume captured on each bar proportional to the trades per day.
            // Default to -1 day for the first order to not reduce the volume of the first order.
            _fastTradingVolumeDiscountFactor =
                _fastTradingVolumeScalingFactor
                * (
                    (decimal)(
                        (
                            orderEvent.UtcTime
                            - (_previousOrderEvent?.UtcTime ?? orderEvent.UtcTime.AddDays(-1))
                        ).TotalMinutes
                    ) / 390m
                );
            _fastTradingVolumeDiscountFactor =
                _fastTradingVolumeDiscountFactor > 1
                    ? 1
                    : Math.Max(0.20m, _fastTradingVolumeDiscountFactor);

            if (_resetMarketCapacityDollarVolume)
            {
                _marketCapacityDollarVolume = 0;
                Trades = 0;
                _resetMarketCapacityDollarVolume = false;
            }

            Trades++;
            _previousOrderEvent = orderEvent;
        }

        /// <summary>
        /// Determines whether we should add the Market Volume to the <see cref="MarketCapacityDollarVolume"/>
        /// </summary>
        /// <returns></returns>
        private bool IncludeMarketVolume(Resolution resolution)
        {
            if (_previousOrderEvent == null)
            {
                return false;
            }

            var dollarVolumeScaleFactor = 6000000;
            DateTime timeout;
            decimal k;

            switch (resolution)
            {
                case Resolution.Tick:
                case Resolution.Second:
                    dollarVolumeScaleFactor = dollarVolumeScaleFactor / 60;
                    k =
                        _averageDollarVolume != 0
                            ? dollarVolumeScaleFactor / _averageDollarVolume
                            : 10;

                    var timeoutPeriod = k > 120 ? 120 : (int)Math.Max(5, (double)k);
                    timeout = _previousOrderEvent.UtcTime.AddMinutes(timeoutPeriod);
                    break;

                case Resolution.Minute:
                    k =
                        _averageDollarVolume != 0
                            ? dollarVolumeScaleFactor / _averageDollarVolume
                            : 10;

                    var timeoutMinutes = k > 120 ? 120 : (int)Math.Max(1, (double)k);
                    timeout = _previousOrderEvent.UtcTime.AddMinutes(timeoutMinutes);
                    break;

                case Resolution.Hour:
                    return _algorithm.UtcTime
                        == _previousOrderEvent.UtcTime.RoundUp(resolution.ToTimeSpan());

                case Resolution.Daily:
                    // At the end of a daily bar, the EndTime is the next day.
                    // Increment the order by one day to match it
                    return _algorithm.UtcTime == _previousOrderEvent.UtcTime
                        || _algorithm.UtcTime.Date
                            == _previousOrderEvent.UtcTime.RoundUp(resolution.ToTimeSpan());

                default:
                    timeout = _previousOrderEvent.UtcTime.AddHours(1);
                    break;
            }

            return _algorithm.UtcTime <= timeout;
        }

        /// <summary>
        /// Updates the market capacity of the Symbol. Called on each time step of the algorithm
        /// </summary>
        /// <returns>False if we're currently within the timeout period, True if the Symbol has went past the timeout</returns>
        public bool UpdateMarketCapacity()
        {
            var bar = GetBar();
            if (bar == null || bar.Volume == 0)
            {
                return false;
            }

            var utcTime = _algorithm.UtcTime;
            var resolution = bar.Period.ToHigherResolutionEquivalent(false);
            var conversionRate = Security.QuoteCurrency.ConversionRate;
            var timeBetweenBars = (decimal)(utcTime - (_previousTime ?? utcTime)).TotalMinutes;

            if (_previousTime == null || timeBetweenBars == 0)
            {
                _averageDollarVolume = conversionRate * bar.Close * bar.Volume;
            }
            else
            {
                _averageDollarVolume =
                    ((bar.Close * conversionRate) * (bar.Volume + _previousVolume))
                    / timeBetweenBars;
            }

            _previousTime = utcTime;
            _previousVolume = bar.Volume;

            var includeMarketVolume = IncludeMarketVolume(resolution);
            if (includeMarketVolume)
            {
                _resolutionScaleFactor = ResolutionScaleFactor(resolution);
                _marketCapacityDollarVolume +=
                    bar.Close
                    * _fastTradingVolumeDiscountFactor
                    * bar.Volume
                    * conversionRate
                    * Security.SymbolProperties.ContractMultiplier;
            }

            // When we've finished including market volume, signal completed
            return !includeMarketVolume;
        }

        /// <summary>
        /// Gets the TradeBar for the given time step. For Quotes, we convert
        /// it into a TradeBar using market depth as a proxy for volume.
        /// </summary>
        /// <returns>TradeBar</returns>
        private TradeBar GetBar()
        {
            TradeBar bar;
            if (_algorithm.CurrentSlice.Bars.TryGetValue(_symbol, out bar))
            {
                return bar;
            }

            QuoteBar quote;
            if (_algorithm.CurrentSlice.QuoteBars.TryGetValue(_symbol, out quote))
            {
                // Fake a tradebar for quote data using market depth as a proxy for volume
                var volume = (quote.LastBidSize + quote.LastAskSize) / 2;

                // Handle volume estimation for security types that don't have volume values
                switch (_symbol.SecurityType)
                {
                    case SecurityType.Forex:
                        volume = _forexMinuteVolume;
                        break;
                    case SecurityType.Cfd:
                        volume = _cfdMinuteVolume;
                        break;
                }

                return new TradeBar(
                    quote.Time,
                    quote.Symbol,
                    quote.Open,
                    quote.High,
                    quote.Low,
                    quote.Close,
                    volume,
                    quote.Period
                );
            }

            if (!_isInternal)
            {
                return null;
            }
            // internal subscriptions, like mapped continuous future contract won't be sent through the slice
            // but will be available in the security cache, if not present will return null
            var result = Security.Cache.GetData<TradeBar>();
            if (
                result != null
                && _algorithm.UtcTime
                    == result.EndTime.ConvertToUtc(Security.Exchange.Hours.TimeZone)
            )
            {
                return result;
            }

            return null;
        }

        private static decimal ResolutionScaleFactor(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Daily:
                    return 0.02m;

                case Resolution.Hour:
                    return 0.05m;

                case Resolution.Minute:
                    return 0.20m;

                case Resolution.Tick:
                case Resolution.Second:
                    return 0.50m;

                default:
                    return 1m;
            }
        }

        /// <summary>
        /// Signals a reset for the <see cref="MarketCapacityDollarVolume"/> and <see cref="SaleVolume"/>
        /// </summary>
        public void Reset()
        {
            _resetMarketCapacityDollarVolume = true;
            SaleVolume = 0;
        }

        /// <summary>
        /// Determines if we should remove a symbol from capacity estimation
        /// </summary>
        public bool ShouldRemove()
        {
            if (
                Security.Invested
                || _algorithm.UtcTime < _previousOrderEvent.UtcTime + CapacityEffectPeriod
            )
            {
                return false;
            }

            return true;
        }
    }
}
