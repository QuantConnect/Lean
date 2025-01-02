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
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing data type bugs in the Consolidate API. Related to GH 4205.
    /// </summary>
    public class ConsolidateRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<int> _consolidationCounts;
        private List<int> _expectedConsolidationCounts;
        private List<SimpleMovingAverage> _smas;
        private List<DateTime> _lastSmaUpdates;
        private int _customDataConsolidatorCount;
        private Future _future;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2020, 01, 05);
            SetEndDate(2020, 01, 20);

            var SP500 = QuantConnect.Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);
            var symbol = FuturesChain(SP500).First();
            _future = AddFutureContract(symbol);

            var tradableDatesCount = QuantConnect.Time.EachTradeableDayInTimeZone(_future.Exchange.Hours,
                StartDate,
                EndDate,
                _future.Exchange.TimeZone,
                false).Count();
            _expectedConsolidationCounts = new(10);

            Consolidate<QuoteBar>(symbol, time => new CalendarInfo(time.RoundDown(TimeSpan.FromDays(1)), TimeSpan.FromDays(1)),
                bar => UpdateQuoteBar(bar, 0));
            // The consolidator will respect the full 1 day bar span and will not consolidate the last tradable date,
            // since scan will not be called at 202/01/21 12am
            _expectedConsolidationCounts.Add(tradableDatesCount - 1);

            Consolidate<QuoteBar>(symbol, time => new CalendarInfo(time.RoundDown(TimeSpan.FromDays(1)), TimeSpan.FromDays(1)),
                TickType.Quote, bar => UpdateQuoteBar(bar, 1));
            _expectedConsolidationCounts.Add(tradableDatesCount - 1);

            Consolidate<QuoteBar>(symbol, TimeSpan.FromDays(1), bar => UpdateQuoteBar(bar, 2));
            _expectedConsolidationCounts.Add(tradableDatesCount - 1);

            Consolidate(symbol, Resolution.Daily, TickType.Quote, (Action<QuoteBar>)(bar => UpdateQuoteBar(bar, 3)));
            _expectedConsolidationCounts.Add(tradableDatesCount);

            Consolidate(symbol, TimeSpan.FromDays(1), bar => UpdateTradeBar(bar, 4));
            _expectedConsolidationCounts.Add(tradableDatesCount - 1);

            Consolidate<TradeBar>(symbol, TimeSpan.FromDays(1), bar => UpdateTradeBar(bar, 5));
            _expectedConsolidationCounts.Add(tradableDatesCount - 1);

            // Test using abstract T types, through defining a 'BaseData' handler

            Consolidate(symbol, Resolution.Daily, null, (Action<BaseData>)(bar => UpdateBar(bar, 6)));
            _expectedConsolidationCounts.Add(tradableDatesCount);

            Consolidate(symbol, TimeSpan.FromDays(1), null, (Action<BaseData>)(bar => UpdateBar(bar, 7)));
            _expectedConsolidationCounts.Add(tradableDatesCount - 1);

            Consolidate(symbol, TimeSpan.FromDays(1), (Action<BaseData>)(bar => UpdateBar(bar, 8)));
            _expectedConsolidationCounts.Add(tradableDatesCount - 1);

            _consolidationCounts = Enumerable.Repeat(0, _expectedConsolidationCounts.Count).ToList();
            _smas = _consolidationCounts.Select(_ => new SimpleMovingAverage(10)).ToList();
            _lastSmaUpdates = _consolidationCounts.Select(_ => DateTime.MinValue).ToList();

            // custom data
            var customSecurity = AddData<CustomDataRegressionAlgorithm.Bitcoin>("BTC", Resolution.Minute);
            Consolidate<TradeBar>(customSecurity.Symbol, TimeSpan.FromDays(1), bar => _customDataConsolidatorCount++);

            try
            {
                Consolidate<QuoteBar>(customSecurity.Symbol, TimeSpan.FromDays(1), bar => { UpdateQuoteBar(bar, -1); });
                throw new RegressionTestException($"Expected {nameof(ArgumentException)} to be thrown");
            }
            catch (ArgumentException)
            {
                // will try to use BaseDataConsolidator for which input is TradeBars not QuoteBars
            }
        }

        private void UpdateBar(BaseData tradeBar, int position)
        {
            if (!(tradeBar is TradeBar))
            {
                throw new RegressionTestException("Expected a TradeBar");
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
            for (var i = 0; i < _consolidationCounts.Count; i++)
            {
                var consolidationCount = _consolidationCounts[i];
                var expectedConsolidationCount = _expectedConsolidationCounts[i];

                if (consolidationCount != expectedConsolidationCount)
                {
                    throw new RegressionTestException($"Expected {expectedConsolidationCount} consolidations for consolidator {i} but received {consolidationCount}");
                }
            }

            if (_customDataConsolidatorCount == 0)
            {
                throw new RegressionTestException($"Unexpected custom data consolidation count: {_customDataConsolidatorCount}");
            }

            for (var i = 0; i < _smas.Count; i++)
            {
                if (_smas[i].Samples != _expectedConsolidationCounts[i])
                {
                    throw new RegressionTestException($"Expected {_expectedConsolidationCounts} samples in each SMA but found {_smas[i].Samples} in SMA in index {i}");
                }

                if (_smas[i].Current.Time != _lastSmaUpdates[i])
                {
                    throw new RegressionTestException($"Expected SMA in index {i} to have been last updated at {_lastSmaUpdates[i]} but was {_smas[i].Current.Time}");
                }
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && _future.HasData)
            {
                SetHoldings(_future.Symbol, 0.5);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 14228;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "665.524%"},
            {"Drawdown", "1.500%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "109332.4"},
            {"Net Profit", "9.332%"},
            {"Sharpe Ratio", "9.805"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "93.474%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "3.164"},
            {"Beta", "0.957"},
            {"Annual Standard Deviation", "0.383"},
            {"Annual Variance", "0.146"},
            {"Information Ratio", "8.29"},
            {"Tracking Error", "0.379"},
            {"Treynor Ratio", "3.917"},
            {"Total Fees", "$15.05"},
            {"Estimated Strategy Capacity", "$2100000000.00"},
            {"Lowest Capacity Asset", "ES XCZJLC9NOB29"},
            {"Portfolio Turnover", "64.34%"},
            {"OrderListHash", "d814db6d5a9c97ee6de477ea06cd3834"}
        };
    }
}
