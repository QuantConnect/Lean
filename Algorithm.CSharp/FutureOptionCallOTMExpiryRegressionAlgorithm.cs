using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests Out of The Money (OTM) future option expiry for puts.
    /// We expect 1 order from the algorithm, which are:
    ///
    ///   * Initial entry, buy ES Call Option (expiring OTM)
    ///     - contract expires worthless, not exercised, so never opened a position in the underlying
    ///
    /// Additionally, we test delistings for future options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    /// </summary>
    /// <remarks>
    /// Total Trades in regression algorithm should be 1, but expiration is counted as a trade.
    /// </remarks>
    public class FutureOptionCallOTMExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _es19h21;
        private Symbol _esOption;
        private Symbol _expectedContract;

        public override void Initialize()
        {
            SetStartDate(2020, 9, 22);
            typeof(QCAlgorithm)
                .GetField("_endDate", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, new DateTime(2021, 3, 30));

            // We add AAPL as a temporary workaround for https://github.com/QuantConnect/Lean/issues/4872
            // which causes delisting events to never be processed, thus leading to options that might never
            // be exercised until the next data point arrives.
            AddEquity("AAPL", Resolution.Daily);

            _es19h21 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2021, 3, 19)),
                Resolution.Minute).Symbol;

            // Select a future option expiring ITM, and adds it to the algorithm.
            _esOption = AddFutureOptionContract(OptionChainProvider.GetOptionContractList(_es19h21, Time)
                .Where(x => x.ID.StrikePrice <= 3400m)
                .OrderByDescending(x => x.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution.Minute).Symbol;

            _expectedContract = QuantConnect.Symbol.CreateOption(_es19h21, Market.CME, OptionStyle.American, OptionRight.Call, 3400m, new DateTime(2021, 3, 19));
            if (_esOption != _expectedContract)
            {
                throw new Exception($"Contract {_expectedContract} was not found in the chain");
            }

            Schedule.On(DateRules.Today, TimeRules.AfterMarketOpen(_es19h21, 1), () =>
            {
                MarketOrder(_esOption, 1);
            });
        }

        public override void OnData(Slice data)
        {
            // Assert delistings, so that we can make sure that we receive the delisting warnings at
            // the expected time. These assertions detect bug #4872
            foreach (var delisting in data.Delistings.Values)
            {
                if (delisting.Type == DelistingType.Warning)
                {
                    if (delisting.Time != new DateTime(2021, 3, 19))
                    {
                        throw new Exception($"Delisting warning issued at unexpected date: {delisting.Time}");
                    }
                }
                if (delisting.Type == DelistingType.Delisted)
                {
                    if (delisting.Time != new DateTime(2021, 3, 20))
                    {
                        throw new Exception($"Delisting happened at unexpected date: {delisting.Time}");
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                // There's lots of noise with OnOrderEvent, but we're only interested in fills.
                return;
            }

            if (!Securities.ContainsKey(orderEvent.Symbol))
            {
                throw new Exception($"Order event Symbol not found in Securities collection: {orderEvent.Symbol}");
            }

            var security = Securities[orderEvent.Symbol];
            if (security.Symbol == _es19h21)
            {
                throw new Exception("Invalid state: did not expect a position for the underlying to be opened, since this contract expires OTM");
            }
            // Expected contract is ES19H21 Call Option expiring OTM @ 3300
            if (security.Symbol == _expectedContract)
            {
                AssertFutureOptionContractOrder(orderEvent, security);
            }
            else
            {
                throw new Exception($"Received order event for unknown Symbol: {orderEvent.Symbol}");
            }

            Log($"{orderEvent}");
        }

        private void AssertFutureOptionContractOrder(OrderEvent orderEvent, Security option)
        {
            if (orderEvent.Direction == OrderDirection.Buy && option.Holdings.Quantity != 1)
            {
                throw new Exception($"No holdings were created for option contract {option.Symbol}");
            }
            if (orderEvent.Direction == OrderDirection.Sell && option.Holdings.Quantity != 0)
            {
                throw new Exception("Holdings were found after a filled option exercise");
            }
            if (orderEvent.Direction == OrderDirection.Sell && !orderEvent.Message.Contains("OTM"))
            {
                throw new Exception("Contract did not expire OTM");
            }
            if (orderEvent.Message.Contains("Exercise"))
            {
                throw new Exception("Exercised option, even though it expires OTM");
            }
        }

        public bool CanRunLocally { get; } = true;
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            { "Total Trades", "2" },
            { "Average Win", "0%" },
            { "Average Loss", "-0.18%" },
            { "Compounding Annual Return", "-0.353%" },
            { "Drawdown", "0.200%" },
            { "Expectancy", "-1" },
            { "Net Profit", "-0.183%" },
            { "Sharpe Ratio", "-1.284" },
            { "Probabilistic Sharpe Ratio", "0.011%" },
            { "Loss Rate", "100%" },
            { "Win Rate", "0%" },
            { "Profit-Loss Ratio", "0" },
            { "Alpha", "0" },
            { "Beta", "0" },
            { "Annual Standard Deviation", "0.002" },
            { "Annual Variance", "0" },
            { "Information Ratio", "-1.284" },
            { "Tracking Error", "0.002" },
            { "Treynor Ratio", "0" },
            { "Total Fees", "$3.70" },
            { "Fitness Score", "0" },
            { "Kelly Criterion Estimate", "0" },
            { "Kelly Criterion Probability Value", "0" },
            { "Sortino Ratio", "-0.178" },
            { "Return Over Maximum Drawdown", "-1.929" },
            { "Portfolio Turnover", "0" },
            { "Total Insights Generated", "0" },
            { "Total Insights Closed", "0" },
            { "Total Insights Analysis Completed", "0" },
            { "Long Insight Count", "0" },
            { "Short Insight Count", "0" },
            { "Long/Short Ratio", "100%" },
            { "Estimated Monthly Alpha Value", "$0" },
            { "Total Accumulated Estimated Alpha Value", "$0" },
            { "Mean Population Estimated Insight Value", "$0" },
            { "Mean Population Direction", "0%" },
            { "Mean Population Magnitude", "0%" },
            { "Rolling Averaged Population Direction", "0%" },
            { "Rolling Averaged Population Magnitude", "0%" },
            { "OrderListHash", "2051471982" }
        };
    }
}

