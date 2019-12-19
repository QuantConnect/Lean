using Deedle;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Report
{
    /// <summary>
    /// Strategy metrics collection such as usage of funds and asset allocations
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Calculates the leverage used from trades. The series used to call this extension function should
        /// be the equity curve with the associated <see cref="Order"/> objects that go along with it.
        /// </summary>
        /// <param name="equityCurve">Equity curve series</param>
        /// <param name="orders">Orders associated with the equity curve</param>
        /// <returns>Leverage utilization over time</returns>
        public static Series<DateTime, double> LeverageUtilization(Series<DateTime, double> equityCurve, List<Order> orders)
        {
            if (equityCurve.IsEmpty || orders.Count == 0)
            {
                return new Series<DateTime, double>(new DateTime[] { }, new double[] { });
            }

            var leverage = PortfolioLooper.FromOrders(equityCurve, orders)
                .ToList() // Required because for some reason our AbsoluteHoldingsValue is multiplied by two whenever we GroupBy on the raw IEnumerable
                .GroupBy(portfolio => portfolio.Time)
                .Select(group => new KeyValuePair<DateTime, double>(
                    group.Key,
                    (double)group.Last().Leverage
                ));

            // Drop missing because we don't care about the missing values
            return new Series<DateTime, double>(leverage).DropMissing();
        }

        /// <summary>
        /// Calculates the portfolio's asset allocation percentage over time. The series used to call this extension function should
        /// be the equity curve with the associated <see cref="Order"/> objects that go along with it.
        /// </summary>
        /// <param name="equityCurve">Equity curve series</param>
        /// <param name="orders">Orders associated with the equity curve</param>
        /// <returns></returns>
        public static Series<Symbol, double> AssetAllocations(Series<DateTime, double> equityCurve, List<Order> orders)
        {
            if (equityCurve.IsEmpty || orders.Count == 0)
            {
                return new Series<Symbol, double>(new Symbol[] { }, new double[] { });
            }

            var portfolioHoldings = PortfolioLooper.FromOrders(equityCurve, orders)
                .ToList() // Required because for some reason our AbsoluteHoldingsValue is multiplied by two whenever we GroupBy on the raw IEnumerable
                .GroupBy(x => x.Time)
                .Select(kvp => kvp.Last())
                .ToList();

            var totalPortfolioValueOverTime = (double)portfolioHoldings.Sum(x => x.Holdings.Sum(y => y.AbsoluteHoldingsValue));
            var holdingsBySymbolOverTime = new Dictionary<Symbol, double>();

            foreach (var portfolio in portfolioHoldings)
            {
                foreach (var holding in portfolio.Holdings)
                {
                    if (!holdingsBySymbolOverTime.ContainsKey(holding.Symbol))
                    {
                        holdingsBySymbolOverTime[holding.Symbol] = (double)holding.AbsoluteHoldingsValue;
                        continue;
                    }

                    holdingsBySymbolOverTime[holding.Symbol] = holdingsBySymbolOverTime[holding.Symbol] + (double)holding.AbsoluteHoldingsValue;
                }
            }

            return new Series<Symbol, double>(
                holdingsBySymbolOverTime.Keys,
                holdingsBySymbolOverTime.Values.Select(x => x / totalPortfolioValueOverTime).ToList()
            ).DropMissing();
        }

        /// <summary>
        /// Strategy long/short exposure by asset class
        /// </summary>
        /// <param name="equityCurve">Equity curve</param>
        /// <param name="orders">Orders of the strategy</param>
        /// <param name="direction">Long/Short</param>
        /// <returns>
        /// DataFrame keyed by <see cref="SecurityType"/> and <see cref="OrderDirection"/>.
        /// Returns a DataFrame of exposure per asset per direction over time
        /// </returns>
        public static Frame<DateTime, Tuple<SecurityType, OrderDirection>> Exposure(Series<DateTime, double> equityCurve, List<Order> orders, OrderDirection direction)
        {
            if (equityCurve.IsEmpty || orders.Count == 0)
            {
                return Frame.CreateEmpty<DateTime, Tuple<SecurityType, OrderDirection>>();
            }

            // We want to add all of the holdings by asset class to a mock dataframe that is column keyed by SecurityType with
            // rows being DateTime and values being the exposure at that given time (as double)
            var holdingsByAssetClass = new Dictionary<SecurityType, List<KeyValuePair<DateTime, double>>>();
            var multiplier = direction == OrderDirection.Sell ? -1 : 1;
            var portfolioLooper = PortfolioLooper.FromOrders(equityCurve, orders);

            foreach (var portfolio in portfolioLooper)
            {
                List<KeyValuePair<DateTime, double>> holdings;
                if (!holdingsByAssetClass.TryGetValue(portfolio.Order.SecurityType, out holdings))
                {
                    holdings = new List<KeyValuePair<DateTime, double>>();
                    holdingsByAssetClass[portfolio.Order.SecurityType] = holdings;
                }

                var assets = portfolio.Holdings
                   .Where(pointInTimeHoldings => pointInTimeHoldings.Symbol.SecurityType == portfolio.Order.SecurityType)
                   .ToList();

                if (assets.Count > 0)
                {
                    // Use the multiplier to flip the holdings value around
                    var sum = (double)assets.Where(pointInTimeHoldings => multiplier * pointInTimeHoldings.HoldingsValue > 0)
                        .Select(pointInTimeHoldings => pointInTimeHoldings.AbsoluteHoldingsValue)
                        .Sum();

                    holdings.Add(new KeyValuePair<DateTime, double>(portfolio.Time, sum / (double)portfolio.TotalPortfolioValue));
                }
            }

            var frame = Frame.CreateEmpty<DateTime, Tuple<SecurityType, OrderDirection>>();

            foreach (var kvp in holdingsByAssetClass)
            {
                // Skip Base asset class since we need it as a special value
                // (and it can't be traded on either way)
                if (kvp.Key == SecurityType.Base)
                {
                    continue;
                }

                // Select the last entry of a given time to get accurate results of the portfolio's actual value.
                // Then, select only the long or short holdings.
                frame = frame.Join(
                    new Tuple<SecurityType, OrderDirection>(kvp.Key, direction),
                    new Series<DateTime, double>(kvp.Value.GroupBy(x => x.Key).Select(x => x.Last())) * multiplier
                );
            }

            // Equivalent to `pd.fillna(method='ffill').dropna(axis=1, how='all').dropna(how='all')`
            // First drops any missing SecurityTypes, then drops the rows with missing values
            // to get rid of any empty data prior to the first value.
            return frame.FillMissing(Direction.Forward)
                .DropSparseColumnsAll()
                .DropSparseRowsAll();
        }
    }
}
