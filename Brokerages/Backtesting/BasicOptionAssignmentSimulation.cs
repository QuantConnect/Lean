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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Backtesting
{
    /// <summary>
    /// This market conditions simulator emulates exercising of short option positions in the portfolio.
    /// Simulator implements basic no-arb argument: when time value of the option contract is close to zero
    /// it assigns short legs getting profit close to expiration dates in deep ITM positions. User algorithm then receives
    /// assignment event from LEAN. Simulator randomly scans for arbitrage opportunities every two hours or so.
    /// </summary>
    public class BasicOptionAssignmentSimulation : IBacktestingMarketSimulation
    {
        // we start simulating assignments 4 days prior to expiration
        private readonly TimeSpan _priorExpiration = new TimeSpan(4,0,0,0);

        // we focus only on deep ITM calls and puts (at least 5% away from price)
        private const decimal _deepITM = 0.05m;

        // we rescan portfolio for new contracts and expirations every month
        private readonly TimeSpan _securitiesRescanPeriod = new TimeSpan(30, 0, 0, 0);

        // we try to generate new assignments every 2 hours
        private readonly TimeSpan _assignmentScanPeriod = new TimeSpan(0, 2, 0, 0);

        // last update time
        private DateTime _lastUpdate = DateTime.MinValue;
        private Queue<DateTime> _assignmentScans;
        private readonly Random _rand = new Random(12345);

        /// <summary>
        /// We generate a list of time points when we would like to run our simulation. we then return true if the time is in the list.
        /// </summary>
        /// <returns></returns>
        public bool IsReadyToSimulate(IAlgorithm algorithm)
        {
            if (_lastUpdate == DateTime.MinValue ||
                algorithm.UtcTime - _lastUpdate > _securitiesRescanPeriod)
            {
                var expirations = algorithm.Securities.Select(x => x.Key)
                            .Where(x => (x.ID.SecurityType == SecurityType.Option || x.ID.SecurityType == SecurityType.FutureOption) &&
                                        x.ID.Date > algorithm.Time &&
                                        x.ID.Date - algorithm.Time <= _securitiesRescanPeriod)
                            .Select(x => x.ID.Date)
                            .OrderBy(x => x)
                            .ToList();

                var scansCount = _priorExpiration.TotalMinutes / _assignmentScanPeriod.TotalMinutes;

                // we generate a list of random dates when we plan to search for opportunities to assign short positions.
                var scans = new List<DateTime>();

                foreach (var expirationDate in expirations)
                {
                    var startDate = expirationDate - _priorExpiration;

                    foreach (var count in Enumerable.Range(0, (int)scansCount))
                    {
                        scans.Add(startDate.AddMinutes(count * _assignmentScanPeriod.TotalMinutes));
                    }
                }
                var randomizedScans = scans
                                    .DistinctBy(x => new DateTime(x.Year, x.Month, x.Day, x.Hour, 0, 0)) // DistinctBy hour
                                    .OrderBy(x => x)
                                    .Select(x => x.AddMinutes(_rand.NextDouble() * _assignmentScanPeriod.TotalMinutes));

                _assignmentScans = new Queue<DateTime>(randomizedScans);

                _lastUpdate = algorithm.UtcTime;
            }

            if (_assignmentScans.Count > 0)
            {
                // we check if new simulation date has arrived. It may happen that several of them had.. due to exchange hours, weekends, etc.
                // we fast forward through unused items
                if (algorithm.UtcTime >= _assignmentScans.Peek())
                {
                    while (_assignmentScans.Count > 0 &&
                            algorithm.UtcTime >= _assignmentScans.Peek())
                    {
                        _assignmentScans.Dequeue();
                    }

                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// We simulate activity of market makers on expiration. Trying to get profit close to expiration dates in deep ITM positions.
        /// This version of the simulator exercises short positions in full.
        /// </summary>
        public void SimulateMarketConditions(IBrokerage brokerage, IAlgorithm algorithm)
        {
            if (!IsReadyToSimulate(algorithm)) return;

            var backtestingBrokerage = (BacktestingBrokerage)brokerage;

            Func<Symbol, bool> deepITM = symbol =>
            {
                var underlyingPrice = algorithm.Securities[symbol.Underlying].Close;

                var result =
                    symbol.ID.OptionRight == OptionRight.Call
                        ? (underlyingPrice - symbol.ID.StrikePrice) / underlyingPrice > _deepITM
                        : (symbol.ID.StrikePrice - underlyingPrice) / underlyingPrice > _deepITM;

                return result;
            };

            algorithm.Securities
                // we take only options that expire soon
                .Where(x => (x.Key.ID.SecurityType == SecurityType.Option || x.Key.ID.SecurityType == SecurityType.FutureOption) &&
                            x.Key.ID.Date - algorithm.UtcTime <= _priorExpiration)
                // we look into short positions only (short for user means long for us)
                .Where(x => x.Value.Holdings.IsShort)
                // we take only deep ITM strikes
                .Where(x => deepITM(x.Key))
                // we estimate P/L
                .Where(x => EstimateArbitragePnL((Option)x.Value,
                        (OptionHolding)x.Value.Holdings,
                        algorithm.Securities[x.Value.Symbol.Underlying],
                        algorithm.Portfolio.CashBook) > 0.0m)
                .ToList()
                // we exercise options with positive expected P/L (over basic sale of option)
                .ForEach(x => backtestingBrokerage.ActivateOptionAssignment((Option)x.Value, (int)((OptionHolding)x.Value.Holdings).AbsoluteQuantity));

        }

        private decimal EstimateArbitragePnL(Option option,
            OptionHolding holding,
            Security underlying,
            ICurrencyConverter currencyConverter)
        {
            // no-arb argument:
            // if our long deep ITM position has a large B/A spread and almost no time value, it may be interesting for us
            // to exercise the option and close the resulting position in underlying instrument, if we want to exit now.

            // User's short option position is our long one.
            // In order to sell ITM position we take option bid price as an input
            var optionPrice = option.BidPrice;

            // we are interested in underlying bid price if we exercise calls and want to sell the underlying immediately.
            // we are interested in underlying ask price if we exercise puts
            var underlyingPrice = option.Symbol.ID.OptionRight == OptionRight.Call
                ? underlying.BidPrice
                : underlying.AskPrice;

            // quantity is normally negative algo's holdings, but since we're modeling the contract holder (counter-party)
            // it's negative THEIR holdings. holding.Quantity is negative, so if counter-party exercises, they would reduce holdings
            var underlyingQuantity = option.GetExerciseQuantity(holding.Quantity);

            // Scenario 1 (base): we just close option position
            var marketOrder1 = new MarketOrder(option.Symbol, -holding.Quantity, option.LocalTime.ConvertToUtc(option.Exchange.TimeZone));
            var orderFee1 = currencyConverter.ConvertToAccountCurrency(option.FeeModel.GetOrderFee(
                new OrderFeeParameters(option, marketOrder1)).Value);

            var basePnL = (optionPrice - holding.AveragePrice) * -holding.Quantity
                * option.QuoteCurrency.ConversionRate
                * option.SymbolProperties.ContractMultiplier
                - orderFee1.Amount;

            // Scenario 2 (alternative): we exercise option and then close underlying position
            var optionExerciseOrder2 = new OptionExerciseOrder(option.Symbol, (int)holding.AbsoluteQuantity, option.LocalTime.ConvertToUtc(option.Exchange.TimeZone));
            var optionOrderFee2 = currencyConverter.ConvertToAccountCurrency(option.FeeModel.GetOrderFee(
                new OrderFeeParameters(option, optionExerciseOrder2)).Value);

            var undelyingMarketOrder2 = new MarketOrder(underlying.Symbol, -underlyingQuantity, underlying.LocalTime.ConvertToUtc(underlying.Exchange.TimeZone));
            var undelyingOrderFee2 = currencyConverter.ConvertToAccountCurrency(underlying.FeeModel.GetOrderFee(
                new OrderFeeParameters(underlying, undelyingMarketOrder2)).Value);

            // calculating P/L of the two transactions (exercise option and then close underlying position)
            var altPnL = (underlyingPrice - option.StrikePrice) * underlyingQuantity * underlying.QuoteCurrency.ConversionRate * option.ContractUnitOfTrade
                        - undelyingOrderFee2.Amount
                        - holding.AveragePrice * holding.AbsoluteQuantity * option.SymbolProperties.ContractMultiplier * option.QuoteCurrency.ConversionRate
                        - optionOrderFee2.Amount;

            return altPnL - basePnL;
        }
    }
}
