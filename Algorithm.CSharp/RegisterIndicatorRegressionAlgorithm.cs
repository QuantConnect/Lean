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
    /// Regression algorithm reproducing data type bugs in the RegisterIndicator API. Related to GH 4205.
    /// </summary>
    public class RegisterIndicatorRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<IIndicator> _indicators;
        private Symbol _symbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 09);

            var SP500 = QuantConnect.Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);
            _symbol = FutureChainProvider.GetFutureContractList(SP500, StartDate.AddDays(1)).First();
            AddFutureContract(_symbol);

            // this collection will hold all indicators and at the end of the algorithm we will assert that all of them are ready
            _indicators = new List<IIndicator>();

            // Test the different APIs for IndicatorBase<QuoteBar> works correctly.
            // Should be able to determine the correct consolidator and not throw an exception
            var indicator = new CustomIndicator();
            RegisterIndicator(_symbol, indicator, Resolution.Minute);
            _indicators.Add(indicator);

            // specifying a selector and using resolution
            var indicator2 = new CustomIndicator();
            RegisterIndicator(_symbol, indicator2, Resolution.Minute, data => (QuoteBar) data);
            _indicators.Add(indicator2);

            // specifying a selector and using timeSpan
            var indicator3 = new CustomIndicator();
            RegisterIndicator(_symbol, indicator3, TimeSpan.FromMinutes(1), data => (QuoteBar)data);
            _indicators.Add(indicator3);

            // directly sending in the desired consolidator
            var indicator4 = new SimpleMovingAverage(10);
            var consolidator = ResolveConsolidator(_symbol, Resolution.Minute, typeof(QuoteBar));
            RegisterIndicator(_symbol, indicator4, consolidator);
            _indicators.Add(indicator4);

            // directly sending in the desired consolidator and specifying a selector
            var indicator5 = new SimpleMovingAverage(10);
            var consolidator2 = ResolveConsolidator(_symbol, Resolution.Minute, typeof(QuoteBar));
            RegisterIndicator(_symbol, indicator5, consolidator2,
                data =>
                {
                    var quoteBar = data as QuoteBar;
                    return quoteBar.High - quoteBar.Low;
                });
            _indicators.Add(indicator5);

            // Now make sure default data type TradeBar works correctly and does not throw an exception
            // Specifying resolution and selector
            var movingAverage = new SimpleMovingAverage(10);
            RegisterIndicator(_symbol, movingAverage, Resolution.Minute, data => data.Value);
            _indicators.Add(movingAverage);

            // Specifying resolution
            var movingAverage2 = new SimpleMovingAverage(10);
            RegisterIndicator(_symbol, movingAverage2, Resolution.Minute);
            _indicators.Add(movingAverage2);

            // Specifying TimeSpan
            var movingAverage3 = new SimpleMovingAverage(10);
            RegisterIndicator(_symbol, movingAverage3, TimeSpan.FromMinutes(1));
            _indicators.Add(movingAverage3);

            // Specifying TimeSpan and selector
            var movingAverage4 = new SimpleMovingAverage(10);
            RegisterIndicator(_symbol, movingAverage4, TimeSpan.FromMinutes(1), data => data.Value);
            _indicators.Add(movingAverage4);

            // Test custom data is able to register correctly and indicators updated
            var smaCustomData = new SimpleMovingAverage(1);
            var symbol = AddData<CustomDataRegressionAlgorithm.Bitcoin>("BTC", Resolution.Minute).Symbol;
            RegisterIndicator(symbol, smaCustomData, TimeSpan.FromMinutes(1), data => data.Value);
            _indicators.Add(smaCustomData);

            var smaCustomData2 = new SimpleMovingAverage(1);
            RegisterIndicator(symbol, smaCustomData2, Resolution.Minute);
            _indicators.Add(smaCustomData2);
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

        public override void OnEndOfAlgorithm()
        {
            if (_indicators.Any(indicator => !indicator.IsReady))
            {
                throw new Exception("All indicators should be ready");
            }
            Log($"Total of {_indicators.Count} are ready");
        }

        private class CustomIndicator : IndicatorBase<QuoteBar>
        {
            private bool _isReady;
            public override bool IsReady => _isReady;
            public CustomIndicator() : base("Jose")
            {
            }
            protected override decimal ComputeNextValue(QuoteBar input)
            {
                _isReady = true;
                return input.Close;
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
        public long DataPoints => 4090;

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
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "19.500%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "88402.7"},
            {"Net Profit", "-11.597%"},
            {"Sharpe Ratio", "-0.687"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "7.176"},
            {"Beta", "10.771"},
            {"Annual Standard Deviation", "1.467"},
            {"Annual Variance", "2.151"},
            {"Information Ratio", "-0.186"},
            {"Tracking Error", "1.331"},
            {"Treynor Ratio", "-0.094"},
            {"Total Fees", "$23.65"},
            {"Estimated Strategy Capacity", "$290000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "309.20%"},
            {"OrderListHash", "448896f610140b728419d56313cc61bc"}
        };
    }
}
