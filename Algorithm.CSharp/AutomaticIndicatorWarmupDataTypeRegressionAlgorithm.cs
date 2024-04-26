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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm which tests indicator warm up using different data types, related to GH issue 4205
    /// </summary>
    public class AutomaticIndicatorWarmupDataTypeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        public override void Initialize()
        {
            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            EnableAutomaticIndicatorWarmUp = true;
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);

            var SP500 = QuantConnect.Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);
            _symbol = FutureChainProvider.GetFutureContractList(SP500, StartDate).First();

            // Test case: custom IndicatorBase<QuoteBar> indicator using Future unsubscribed symbol
            var indicator1 = new CustomIndicator();
            AssertIndicatorState(indicator1, isReady: false);
            WarmUpIndicator(_symbol, indicator1);
            AssertIndicatorState(indicator1, isReady: true);

            // Test case: SimpleMovingAverage<IndicatorDataPoint> using Future unsubscribed symbol (should use TradeBar)
            var sma1 = new SimpleMovingAverage(10);
            AssertIndicatorState(sma1, isReady: false);
            WarmUpIndicator(_symbol, sma1);
            AssertIndicatorState(sma1, isReady: true);

            // Test case: SimpleMovingAverage<IndicatorDataPoint> using Equity unsubscribed symbol
            var spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var sma = new SimpleMovingAverage(10);
            AssertIndicatorState(sma, isReady: false);
            WarmUpIndicator(spy, sma);
            AssertIndicatorState(sma, isReady: true);

            // We add the symbol
            AddFutureContract(_symbol);
            AddEquity("SPY");
            // force spy for use Raw data mode so that it matches the used when unsubscribed which uses the universe settings
            SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(spy).SetDataNormalizationMode(DataNormalizationMode.Raw);

            // Test case: custom IndicatorBase<QuoteBar> indicator using Future subscribed symbol
            var indicator = new CustomIndicator();
            var consolidator = CreateConsolidator(TimeSpan.FromMinutes(2), typeof(QuoteBar));
            RegisterIndicator(_symbol, indicator, consolidator);

            AssertIndicatorState(indicator, isReady: false);
            WarmUpIndicator(_symbol, indicator);
            AssertIndicatorState(indicator, isReady: true);

            // Test case: SimpleMovingAverage<IndicatorDataPoint> using Future Subscribed symbol (should use TradeBar)
            var sma11 = new SimpleMovingAverage(10);
            AssertIndicatorState(sma11, isReady: false);
            WarmUpIndicator(_symbol, sma11);
            AssertIndicatorState(sma11, isReady: true);

            if (!sma11.Current.Equals(sma1.Current))
            {
                throw new Exception("Expected SMAs warmed up before and after adding the Future to the algorithm to have the same current value. " +
                                    "The result of 'WarmUpIndicator' shouldn't change if the symbol is or isn't subscribed");
            }

            // Test case: SimpleMovingAverage<IndicatorDataPoint> using Equity unsubscribed symbol
            var smaSpy = new SimpleMovingAverage(10);
            AssertIndicatorState(smaSpy, isReady: false);
            WarmUpIndicator(spy, smaSpy);
            AssertIndicatorState(smaSpy, isReady: true);

            if (!smaSpy.Current.Equals(sma.Current))
            {
                throw new Exception("Expected SMAs warmed up before and after adding the Equity to the algorithm to have the same current value. " +
                                    "The result of 'WarmUpIndicator' shouldn't change if the symbol is or isn't subscribed");
            }
        }

        private void AssertIndicatorState(IIndicator indicator, bool isReady)
        {
            if (indicator.IsReady != isReady)
            {
                throw new Exception($"Expected indicator state, expected {isReady} but was {indicator.IsReady}");
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

        private class CustomIndicator : IndicatorBase<QuoteBar>, IIndicatorWarmUpPeriodProvider
        {
            private bool _isReady;
            public int WarmUpPeriod => 1;
            public override bool IsReady => _isReady;
            public CustomIndicator() : base("Pepe")
            { }
            protected override decimal ComputeNextValue(QuoteBar input)
            {
                _isReady = true;
                return input.Ask.High;
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
        public long DataPoints => 6426;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 84;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "733913.744%"},
            {"Drawdown", "15.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "106827.7"},
            {"Net Profit", "6.828%"},
            {"Sharpe Ratio", "203744786353.299"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "456382350698.622"},
            {"Beta", "9.229"},
            {"Annual Standard Deviation", "2.24"},
            {"Annual Variance", "5.017"},
            {"Information Ratio", "228504036840.953"},
            {"Tracking Error", "1.997"},
            {"Treynor Ratio", "49450701625.717"},
            {"Total Fees", "$23.65"},
            {"Estimated Strategy Capacity", "$200000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "351.80%"},
            {"OrderListHash", "dfd9a280d3c6470b305c03e0b72c234e"}
        };
    }
}
