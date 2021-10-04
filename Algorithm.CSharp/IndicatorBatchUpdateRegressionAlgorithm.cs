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
using QuantConnect;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Indicators;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;


namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm which tests indicator warm up using IndicatorBatchUpdate and ConsolidatorBatchUpdate, related to GH issue #5634
    /// </summary>
    public class IndicatorBatchUpdateRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private SimpleMovingAverage _sma;
        private RollingWindow<IBaseDataBar> _rollingWindow;
        private CustomQuoteBarIndicator _customQuoteBarIndicator;
        private int _period;
        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 10, 11);
            SetCash(1000000);
            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            var spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            _period = 10;

            // Test case: IndicatorBatchUpdate for indicators with different data types and Equity unsubscribed symbol
            var atr = new AverageTrueRange(_period);
            var rollingAtrWindow = new RollingWindow<decimal>(_period);
            atr.Updated += (sender, updated) => rollingAtrWindow.Add(updated);
            var rsi = new RelativeStrengthIndex(_period);
            var rsiDoubled = rsi.Times(2);
            var indicators1 = new List<IIndicator>(){atr, rsi, new SimpleMovingAverage(_period), new CustomTradeBarIndicator(_period), new CustomQuoteBarIndicator(_period)};

            AssertIndicatorStates(indicators1, isReady: false);
            AssertRollingWindowBarCount(rollingAtrWindow, count:0);
            IndicatorBatchUpdate(spy, indicators1, 11, Resolution.Minute);       // warm up period for RSI(n) is n+1
            AssertIndicatorStates(indicators1, isReady: true);
            AssertRollingWindowBarCount(rollingAtrWindow, count: rollingAtrWindow.Size);
            if (rsiDoubled.Current.Value != 2*rsi.Current.Value)
            {
                throw new Exception($"Indicator extension rsiDoubled not correct!");
            }
      
            var SP500 = QuantConnect.Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);
            _symbol  = FutureChainProvider.GetFutureContractList(SP500, StartDate).First();
            
            // Test case: ConsolidatorBatchUpdate for an user-defined QuoteBarConsolidator using Future unsubscribed symbol
            _sma = new SimpleMovingAverage(_period);
            _customQuoteBarIndicator = new CustomQuoteBarIndicator(_period);
            _rollingWindow = new RollingWindow<IBaseDataBar>(_period);
            var consolidator = new QuoteBarConsolidator(TimeSpan.FromMinutes(1));
            consolidator.DataConsolidated += OnDataConsolidated;

            var indicators2 = new List<IIndicator>(){_sma, _customQuoteBarIndicator};
            AssertIndicatorStates(indicators2, isReady: false);
            AssertRollingWindowBarCount(_rollingWindow, count: 0);
            ConsolidatorBatchUpdate(_symbol, consolidator, _period + 1, Resolution.Minute);
            AssertIndicatorStates(indicators2, isReady: true);
            AssertRollingWindowBarCount(_rollingWindow, count: _rollingWindow.Size);

            // Reset indicators and rolling window for next test case
            _sma.Reset();
            _customQuoteBarIndicator.Reset();
            _rollingWindow.Reset();

            // Test case: ConsolidatorBatchUpdate for an user-defined QuoteBarConsolidator using Future subscribed symbol
            AddFutureContract(_symbol, Resolution.Minute);
            var consolidator2 = new QuoteBarConsolidator(TimeSpan.FromMinutes(2));
            consolidator2.DataConsolidated += OnDataConsolidated;
            SubscriptionManager.AddConsolidator(_symbol, consolidator2);

            AssertIndicatorStates(indicators2, isReady: false);
            AssertRollingWindowBarCount(_rollingWindow, count: 0);
            ConsolidatorBatchUpdate(_symbol, consolidator2, _period*2 + 1, Resolution.Minute);    // larger period required here since the consolidator will aggregate the bars provided by the history request
            AssertIndicatorStates(indicators2, isReady: true);
            AssertRollingWindowBarCount(_rollingWindow, count: _rollingWindow.Size);
        }
        
        private void OnDataConsolidated(object sender, QuoteBar quoteBar)
        {
            _rollingWindow.Add(quoteBar);
            _sma.Update(new IndicatorDataPoint(quoteBar.EndTime, quoteBar.Ask.Close));
            _customQuoteBarIndicator.Update(quoteBar);
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested && data.ContainsKey(_symbol))
            {
                MarketOrder(_symbol, 1);
            }
        }

        private void AssertIndicatorStates(IEnumerable<IIndicator> indicators, bool isReady)
        {
            if (indicators.Any(indicator => indicator.IsReady != isReady))
                {
                    var indicator = indicators.Where(x => x.IsReady != isReady).First();
                    throw new Exception($"Unexpected indicator state, expected {isReady} but was {indicator.IsReady} for indicator {indicator.Name}.");
                }
        }

        private void AssertRollingWindowBarCount<T>(RollingWindow<T> rollingWindow, int count) // where T : IBaseData
        {
            if (rollingWindow.Count < count)
            {
                throw new Exception($"Expected rolling window count = {count}, but was {rollingWindow.Count}.");
            }
        }

        private class CustomTradeBarIndicator : IndicatorBase<TradeBar>, IIndicatorWarmUpPeriodProvider
        {
            private bool _isReady;
            public int WarmUpPeriod { get; }
            public override bool IsReady => _isReady;
            public CustomTradeBarIndicator(int period) : base("Lola")
            {
                WarmUpPeriod = period;
            }
            protected override decimal ComputeNextValue(TradeBar input)
            {
                _isReady = Samples >= WarmUpPeriod;
                return input.High;
            }
        }

        private class CustomQuoteBarIndicator : IndicatorBase<QuoteBar>, IIndicatorWarmUpPeriodProvider
        {
            private bool _isReady;
            public int WarmUpPeriod { get; }
            public override bool IsReady => _isReady;
            public CustomQuoteBarIndicator(int period): base("Lina")
            {
                WarmUpPeriod = period;
            }
            protected override decimal ComputeNextValue(QuoteBar input)
            {
                _isReady = Samples >= WarmUpPeriod;
                return input.Ask.High;
            }

            public override void Reset()
            {
                _isReady = false;
                base.Reset();
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "17.431%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.163%"},
            {"Sharpe Ratio", "15.604"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.494"},
            {"Beta", "0.068"},
            {"Annual Standard Deviation", "0.013"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-60.69"},
            {"Tracking Error", "0.166"},
            {"Treynor Ratio", "3.029"},
            {"Total Fees", "$1.85"},
            {"Estimated Strategy Capacity", "$1500000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Fitness Score", "0.027"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "19.106"},
            {"Return Over Maximum Drawdown", "136.191"},
            {"Portfolio Turnover", "0.027"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "6ed2d03562bc206cb0dffe3a410540b1"}
        };
    }
}
