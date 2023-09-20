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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template framework algorithm uses framework components to define the algorithm.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class DominantCyclePeriodAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private DominantCyclePeriodIndicator _htdcperiod;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            Console.WriteLine("Dominant Cycle Period Algorithm - Initialize");

            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 01, 01); //Set Start Date
            SetEndDate(2013, 12, 01); //Set End Date
            SetCash(100000); //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.

            // // set algorithm framework models
            // SetUniverseSelection(
            //     new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            // SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025,
            //     null));
            //
            // // We can define who often the EWPCM will rebalance if no new insight is submitted using:
            // // Resolution Enum:
            // SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(Resolution.Daily));
            // // TimeSpan
            // // SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(TimeSpan.FromDays(2)));
            // // A Func<DateTime, DateTime>. In this case, we can use the pre-defined func at Expiry helper class
            // // SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(Expiry.EndOfWeek));
            //
            // SetExecution(new ImmediateExecutionModel());
            // SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.01m));

            _symbol = AddEquity("SPY", Resolution.Daily).Symbol;

            _htdcperiod = new DominantCyclePeriodIndicator("SPYIND", 30);

            //IndicatorDataPoint in1 = new IndicatorDataPoint(();

            var consolidator = new TradeBarConsolidator(TimeSpan.FromHours(1));
            //Update the AD indicator with the consolidated bar
            consolidator.DataConsolidated += (_, bar) => _htdcperiod.Update(bar);
            SubscriptionManager.AddConsolidator(_symbol, consolidator);
        }


        static int c = 0;

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            Console.WriteLine("Dominant Cycle Period Algorithm - OnData");

            string strDebugLine = "OnData Slice Data:: c: " + c++ +
                " Data.Count: " + data.Count +
                " Values.Count: " + data.Values.Count +
                " AllData.Count: " + data.AllData.Count +
                " Bars.Count: " + data.Bars.Count +
                " Bars['SPY'].Close: " + data.Bars["SPY"].Close;

            Console.WriteLine(strDebugLine);
            System.Diagnostics.Debug.WriteLine(strDebugLine);

            // if (!Portfolio.Invested)
            // {
            //     Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            //     SetHoldings(_spy, 1);
            //     Debug("Purchased Stock");
            // }

            //Update the RSI indicator value with the new input close price every day
            if (data.Bars.ContainsKey(_symbol))
            {
                var bar = data.Bars[_symbol];
                _htdcperiod.Update(bar);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Console.WriteLine("Dominant Cycle Period Algorithm - OnOrderEvent");

            if (orderEvent.Status.IsFill())
            {
                Debug($"Purchased Stock: {orderEvent.Symbol}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        //public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };
        public virtual Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            { "Total Trades", "3" },
            { "Average Win", "0%" },
            { "Average Loss", "-1.01%" },
            { "Compounding Annual Return", "261.134%" },
            { "Drawdown", "2.200%" },
            { "Expectancy", "-1" },
            { "Net Profit", "1.655%" },
            { "Sharpe Ratio", "8.505" },
            { "Probabilistic Sharpe Ratio", "66.840%" },
            { "Loss Rate", "100%" },
            { "Win Rate", "0%" },
            { "Profit-Loss Ratio", "0" },
            { "Alpha", "-0.091" },
            { "Beta", "1.006" },
            { "Annual Standard Deviation", "0.224" },
            { "Annual Variance", "0.05" },
            { "Information Ratio", "-33.445" },
            { "Tracking Error", "0.002" },
            { "Treynor Ratio", "1.893" },
            { "Total Fees", "$10.32" },
            { "Estimated Strategy Capacity", "$27000000.00" },
            { "Lowest Capacity Asset", "SPY R735QTJ8XC9X" },
            { "Portfolio Turnover", "59.86%" },
            { "OrderListHash", "ad2216297c759d8e5aef48ff065f8919" }
        };
    }
}
