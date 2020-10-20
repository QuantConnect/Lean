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
    /// This regression algorithm tests In The Money (ITM) future option expiry for short calls.
    /// We expect 3 orders from the algorithm, which are:
    ///
    ///   * Initial entry, sell ES Put Option (expiring ITM)
    ///   * Option assignment, buy 1 contract of the underlying (ES)
    ///   * Future contract expiry, liquidation (sell 1 ES future)
    ///
    /// Additionally, we test delistings for future options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    /// </summary>
    public class FutureOptionShortPutITMExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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
                .Where(x => x.ID.StrikePrice >= 3300m && x.ID.OptionRight == OptionRight.Put)
                .OrderBy(x => x.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution.Minute).Symbol;

            _expectedContract = QuantConnect.Symbol.CreateOption(_es19h21, Market.CME, OptionStyle.American, OptionRight.Put, 3300m, new DateTime(2021, 3, 19));
            if (_esOption != _expectedContract)
            {
                throw new Exception($"Contract {_expectedContract} was not found in the chain");
            }

            Schedule.On(DateRules.Today, TimeRules.AfterMarketOpen(_es19h21, 1), () =>
            {
                MarketOrder(_esOption, -1);
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
                AssertFutureOptionOrderExercise(orderEvent, security, Securities[_expectedContract]);
            }
            // Expected contract is ES19H21 Call Option expiring ITM @ 3250
            else if (security.Symbol == _expectedContract)
            {
                AssertFutureOptionContractOrder(orderEvent, security);
            }
            else
            {
                throw new Exception($"Received order event for unknown Symbol: {orderEvent.Symbol}");
            }

            Log($"{orderEvent}");
        }

        private void AssertFutureOptionOrderExercise(OrderEvent orderEvent, Security future, Security optionContract)
        {
            if (orderEvent.Message.Contains("Assignment") && orderEvent.Direction == OrderDirection.Buy && future.Holdings.Quantity != 1)
            {
                throw new Exception($"Expected Qty: 1 futures holdings for assigned future {future.Symbol}, found {future.Holdings.Quantity}");
            }
            if (!orderEvent.Message.Contains("Assignment") && orderEvent.Direction == OrderDirection.Sell && future.Holdings.Quantity != 0)
            {
                // We buy back the underlying at expiration, so we expect a neutral position then
                throw new Exception($"Expected no holdings when liquidating future contract {future.Symbol}");
            }
        }

        private void AssertFutureOptionContractOrder(OrderEvent orderEvent, Security option)
        {
            if (orderEvent.Direction == OrderDirection.Sell && option.Holdings.Quantity != -1)
            {
                throw new Exception($"No holdings were created for option contract {option.Symbol}");
            }
            if (orderEvent.Direction == OrderDirection.Buy && option.Holdings.Quantity != 0)
            {
                throw new Exception("Holdings were found after a filled option exercise");
            }
            if (orderEvent.IsAssignment && option.Holdings.Quantity != 0)
            {
                throw new Exception($"Holdings were found after option contract was assigned: {option.Symbol}");
            }
        }

        public bool CanRunLocally { get; } = true;
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            { "Total Trades", "3" },
            { "Average Win", "2.21%" },
            { "Average Loss", "-0.27%" },
            { "Compounding Annual Return", "3.774%" },
            { "Drawdown", "0.000%" },
            { "Expectancy", "3.572" },
            { "Net Profit", "1.937%" },
            { "Sharpe Ratio", "1.28" },
            { "Probabilistic Sharpe Ratio", "65.965%" },
            { "Loss Rate", "50%" },
            { "Win Rate", "50%" },
            { "Profit-Loss Ratio", "8.14" },
            { "Alpha", "0" },
            { "Beta", "0" },
            { "Annual Standard Deviation", "0.025" },
            { "Annual Variance", "0.001" },
            { "Information Ratio", "1.28" },
            { "Tracking Error", "0.025" },
            { "Treynor Ratio", "0" },
            { "Total Fees", "$7.40" },
            { "Fitness Score", "0.02" },
            { "Kelly Criterion Estimate", "0" },
            { "Kelly Criterion Probability Value", "0" },
            { "Sortino Ratio", "79228162514264337593543950335" },
            { "Return Over Maximum Drawdown", "980.31" },
            { "Portfolio Turnover", "0.02" },
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
            { "OrderListHash", "610527226" }
        };
    }
}

