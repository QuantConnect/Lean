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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm that test if the fill prices are the correct quote side.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class EquityTradeAndQuotesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private bool _canTrade;
        private int _quoteCounter;
        private int _tradeCounter;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash


            SetSecurityInitializer(x => x.SetDataNormalizationMode(DataNormalizationMode.Raw));

            _symbol = AddEquity("IBM", Resolution.Minute).Symbol;
            AddEquity("AAPL", Resolution.Daily);

            // 2013-10-07 was Monday, that's why we ask 3 days history to get  data from previous Friday.
            var history = History(new[] { _symbol }, TimeSpan.FromDays(3), Resolution.Minute).ToList();
            Log($"{Time} - history.Count: {history.Count}");

            const int expectedSliceCount = 390;
            if (history.Count != expectedSliceCount)
            {
                throw new Exception($"History slices - expected: {expectedSliceCount}, actual: {history.Count}");
            }


            if (history.Any(s => s.Bars.Count != 1 && s.QuoteBars.Count != 1))
            {
                throw new Exception($"History not all slices have trades and quotes.");
            }

            Schedule.On(DateRules.EveryDay(_symbol), TimeRules.AfterMarketOpen(_symbol, 0), () => { _canTrade = true; });

            Schedule.On(DateRules.EveryDay(_symbol), TimeRules.BeforeMarketClose(_symbol, 16), () => { _canTrade = false; });

        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            _quoteCounter += data.QuoteBars.Count;
            _tradeCounter += data.Bars.Count;

            if (!Portfolio.Invested && _canTrade)
            {
                SetHoldings(_symbol, 1);
                Log($"Purchased Security {_symbol.ID}");
            }

            if (Time.Minute % 15 == 0)
            {
                Liquidate();
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var addedSecurity in changes.AddedSecurities)
            {
                var subscriptions = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(addedSecurity.Symbol);
                if (addedSecurity.Symbol == _symbol)
                {
                    if (!(subscriptions.Count == 2 &&
                          subscriptions.Any(s => s.TickType == TickType.Trade) &&
                          subscriptions.Any(s => s.TickType == TickType.Quote)))
                    {
                        throw new Exception($"Subscriptions were not correctly added for high resolution.");
                    }
                }
                else
                {
                    if (subscriptions.Single().TickType != TickType.Trade)
                    {
                        throw new Exception($"Subscriptions were not correctly added for low resolution.");
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Log($"{Time:s} {orderEvent.Direction}");
                var expectedFillPrice = orderEvent.Direction == OrderDirection.Buy ? Securities[_symbol].AskPrice : Securities[_symbol].BidPrice;
                if (orderEvent.FillPrice != expectedFillPrice)
                {
                    throw new Exception($"Fill price is not the expected for OrderId {orderEvent.OrderId} at Algorithm Time {Time:s}." +
                                        $"\n\tExpected fill price: {expectedFillPrice}, Actual fill price: {orderEvent.FillPrice}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // We expect at least 390 * 5 = 1950 minute bar
            // + 5 daily bars, but those are pumped into OnData every minute 
            if (_tradeCounter <= 1955)
            {
                throw new Exception($"Fail at trade bars count expected >= 1955, actual: {_tradeCounter}.");
            }
            // We expect 390 * 5 = 1950 quote bars. 
            if (_quoteCounter != 1950)
            {
                throw new Exception($"Fail at trade bars count expected: 1950, actual: {_quoteCounter}.");
            }

        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5508;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 780;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "250"},
            {"Average Win", "0.12%"},
            {"Average Loss", "-0.10%"},
            {"Compounding Annual Return", "-86.492%"},
            {"Drawdown", "3.300%"},
            {"Expectancy", "-0.225"},
            {"Start Equity", "100000"},
            {"End Equity", "97294.97"},
            {"Net Profit", "-2.705%"},
            {"Sharpe Ratio", "-5.072"},
            {"Sortino Ratio", "-5.033"},
            {"Probabilistic Sharpe Ratio", "1.585%"},
            {"Loss Rate", "65%"},
            {"Win Rate", "35%"},
            {"Profit-Loss Ratio", "1.20"},
            {"Alpha", "-1.882"},
            {"Beta", "0.571"},
            {"Annual Standard Deviation", "0.149"},
            {"Annual Variance", "0.022"},
            {"Information Ratio", "-22.183"},
            {"Tracking Error", "0.123"},
            {"Treynor Ratio", "-1.323"},
            {"Total Fees", "$670.74"},
            {"Estimated Strategy Capacity", "$190000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "4996.13%"},
            {"OrderListHash", "c65a9aa12b55e53a49a29cd28a358fcd"}
        };
    }
}
