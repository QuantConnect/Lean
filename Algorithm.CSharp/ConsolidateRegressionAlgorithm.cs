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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing data type bugs in the Consolidate API. Related to GH 4205.
    /// </summary>
    public class ConsolidateRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<int> _consolidationCounts;
        private List<SimpleMovingAverage> _smas;
        private List<DateTime> _lastSmaUpdates;
        private int _expectedConsolidations;
        private int _customDataConsolidator;
        private Symbol _symbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 20);

            var SP500 = QuantConnect.Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);
            _symbol = FutureChainProvider.GetFutureContractList(SP500, StartDate).First();
            var security = AddFutureContract(_symbol);

            _consolidationCounts = Enumerable.Repeat(0, 9).ToList();
            _smas = _consolidationCounts.Select(_ => new SimpleMovingAverage(10)).ToList();
            _lastSmaUpdates = _consolidationCounts.Select(_ => DateTime.MinValue).ToList();

            Consolidate<QuoteBar>(_symbol, time => new CalendarInfo(time.RoundDown(TimeSpan.FromDays(1)), TimeSpan.FromDays(1)),
                bar => UpdateQuoteBar(bar, 0));

            Consolidate<QuoteBar>(_symbol, time => new CalendarInfo(time.RoundDown(TimeSpan.FromDays(1)), TimeSpan.FromDays(1)),
                TickType.Quote, bar => UpdateQuoteBar(bar, 1));

            Consolidate<QuoteBar>(_symbol, TimeSpan.FromDays(1), bar => UpdateQuoteBar(bar, 2));

            Consolidate(_symbol, Resolution.Daily, TickType.Quote, (Action<QuoteBar>)(bar => UpdateQuoteBar(bar, 3)));

            Consolidate(_symbol, TimeSpan.FromDays(1), bar => UpdateTradeBar(bar, 4));

            Consolidate<TradeBar>(_symbol, TimeSpan.FromDays(1), bar => UpdateTradeBar(bar, 5));

            // custom data
            var symbol = AddData<CustomDataRegressionAlgorithm.Bitcoin>("BTC", Resolution.Minute).Symbol;
            Consolidate<TradeBar>(symbol, TimeSpan.FromDays(1), bar => _customDataConsolidator++);

            try
            {
                Consolidate<QuoteBar>(symbol, TimeSpan.FromDays(1), bar => { UpdateQuoteBar(bar, -1); });
                throw new Exception($"Expected {nameof(ArgumentException)} to be thrown");
            }
            catch (ArgumentException)
            {
                // will try to use BaseDataConsolidator for which input is TradeBars not QuoteBars
            }

            // Test using abstract T types, through defining a 'BaseData' handler
            Consolidate(_symbol, Resolution.Daily, null, (Action<BaseData>)(bar => UpdateBar(bar, 6)));

            Consolidate(_symbol, TimeSpan.FromDays(1), null, (Action<BaseData>)(bar => UpdateBar(bar, 7)));

            Consolidate(_symbol, TimeSpan.FromDays(1), (Action<BaseData>)(bar => UpdateBar(bar, 8)));

            _expectedConsolidations = QuantConnect.Time.EachTradeableDayInTimeZone(security.Exchange.Hours,
                StartDate,
                EndDate,
                security.Exchange.TimeZone,
                false).Count();
        }
        private void UpdateBar(BaseData tradeBar, int position)
        {
            if (!(tradeBar is TradeBar))
            {
                throw new Exception("Expected a TradeBar");
            }
            _consolidationCounts[position]++;
            _smas[position].Update(tradeBar.EndTime, tradeBar.Value);
            _lastSmaUpdates[position] = tradeBar.EndTime;
        }
        private void UpdateTradeBar(TradeBar tradeBar, int position)
        {
            _consolidationCounts[position]++;
            _smas[position].Update(tradeBar.EndTime, tradeBar.High);
            _lastSmaUpdates[position] = tradeBar.EndTime;
        }
        private void UpdateQuoteBar(QuoteBar quoteBar, int position)
        {
            _consolidationCounts[position]++;
            _smas[position].Update(quoteBar.EndTime, quoteBar.High);
            _lastSmaUpdates[position] = quoteBar.EndTime;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_consolidationCounts.Any(i => i != _expectedConsolidations) || _customDataConsolidator == 0)
            {
                throw new Exception("Unexpected consolidation count");
            }

            for (var i = 0; i < _smas.Count; i++)
            {
                if (_smas[i].Samples != _expectedConsolidations)
                {
                    throw new Exception($"Expected {_expectedConsolidations} samples in each SMA but found {_smas[i].Samples} in SMA in index {i}");
                }

                if (_smas[i].Current.Time != _lastSmaUpdates[i])
                {
                    throw new Exception($"Expected SMA in index {i} to have been last updated at {_lastSmaUpdates[i]} but was {_smas[i].Current.Time}");
                }
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_symbol, 0.5);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 12244;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "6636.699%"},
            {"Drawdown", "15.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "116177.7"},
            {"Net Profit", "16.178%"},
            {"Sharpe Ratio", "640.313"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "99.824%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "636.164"},
            {"Beta", "5.924"},
            {"Annual Standard Deviation", "1.012"},
            {"Annual Variance", "1.024"},
            {"Information Ratio", "696.123"},
            {"Tracking Error", "0.928"},
            {"Treynor Ratio", "109.404"},
            {"Total Fees", "$23.65"},
            {"Estimated Strategy Capacity", "$210000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "81.19%"},
            {"OrderListHash", "dfd9a280d3c6470b305c03e0b72c234e"}
        };
    }
}
