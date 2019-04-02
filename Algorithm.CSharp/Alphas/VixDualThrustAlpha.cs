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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
    /// <summary>
    /// This is a demonstration algorithm. It trades UVXY.
    /// Dual Thrust alpha model is used to produce insights.
    /// Those input parameters have been chosen that gave acceptable results on a series
    /// of random backtests run for the period from Oct, 2016 till Feb, 2019.
    /// </summary>
    class VIXDualThrustAlpha : QCAlgorithm
    {
        // -- STRATEGY INPUT PARAMETERS --
        private decimal _k1 = 0.63m;
        private decimal _k2 = 0.63m;
        private int _rangePeriod = 20;
        private int _consolidatorBars = 30;

        // -- INITIALIZE --
        public override void Initialize()
        {
            // Settings
            SetStartDate(2016, 10, 01);
            SetSecurityInitializer(s => s.SetFeeModel(new ConstantFeeModel(0m)));
            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

            // Universe Selection
            UniverseSettings.Resolution = Resolution.Minute;   // it's minute by default, but lets leave this param here
            var symbols = new[] { QuantConnect.Symbol.Create("UVXY", SecurityType.Equity, Market.USA) };
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));

            // Warming up
            var resolutionInTimeSpan = UniverseSettings.Resolution.ToTimeSpan();
            var warmUpTimeSpan = resolutionInTimeSpan.Multiply(_consolidatorBars).Multiply(_rangePeriod);
            SetWarmUp(warmUpTimeSpan);

            // Alpha Model
            SetAlpha(new DualThrustAlphaModel(_k1, _k2, _rangePeriod, UniverseSettings.Resolution, _consolidatorBars));

            // Portfolio Construction
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Execution
            SetExecution(new ImmediateExecutionModel());

            // Risk Management
            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.03m));
        }
    }

    /// <summary>
    /// Alpha model that uses dual-thrust strategy to create insights
    /// https://medium.com/@FMZ_Quant/dual-thrust-trading-strategy-2cc74101a626
    /// or here:
    /// https://www.quantconnect.com/tutorials/strategy-library/dual-thrust-trading-algorithm
    /// </summary>
    public class DualThrustAlphaModel : AlphaModel
    {
        private readonly decimal _k1;
        private readonly decimal _k2;
        private readonly TimeSpan _consolidatorTimeSpan;
        private readonly int _rangePeriod;
        private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol;

        /// <summary>
        /// Initializes a new instance of the  class
        /// </summary>
        /// <param name="k1">Coefficient for upper band</param>
        /// <param name="k2">Coefficient for lower band</param>
        /// <param name="rangePeriod">Amount of last bars to calculate the range</param>
        /// <param name="resolution">The resolution of data sent into the EMA indicators</param>
        /// <param name="barsToConsolidate">If we want alpha o work on trade bars whose length is
        /// different from the standard resolution - 1m 1h etc. - we need to pass this parameters along
        /// with proper data resolution</param>
        public DualThrustAlphaModel(
            decimal k1,
            decimal k2,
            int rangePeriod,
            Resolution resolution = Resolution.Daily,
            int barsToConsolidate = 1
            )
        {
            // coefficient that used to determinte upper and lower borders of a breakout channel
            _k1 = k1;
            _k2 = k2;

            // period the range is calculated over
            _rangePeriod = rangePeriod;

            // initialize with empty dict.
            _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();

            // time for bars we make the calculations on
            _consolidatorTimeSpan = resolution.ToTimeSpan().Multiply(barsToConsolidate);
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();

            // in 5 days after emission an insight is to be considered expired
            int insightCloseAddDays = 5;

            foreach (var symbolData in _symbolDataBySymbol.Values)
            {
                var range = symbolData.Range;
                var symbol = symbolData.Symbol;
                var security = algorithm.Securities[symbol];

                if (symbolData.IsReady)
                {
                    // buying condition
                    // - (1) price is above upper line
                    // - (2) and we are not long. this is a first time we crossed the line lately
                    if (security.Price > symbolData.UpperLine && !algorithm.Portfolio[symbol].IsLong)
                    {
                        DateTime insightCloseTimeUtc = algorithm.UtcTime.AddDays(insightCloseAddDays);
                        insights.Add(Insight.Price(symbolData.Symbol, insightCloseTimeUtc, InsightDirection.Up));
                    }

                    // selling condition
                    // - (1) price is lower that lower line
                    // - (2) and we are not short. this is a first time we crossed the line lately
                    if (security.Price < symbolData.LowerLine && !algorithm.Portfolio[symbol].IsShort)
                    {
                        DateTime insightCloseTimeUtc = algorithm.UtcTime.AddDays(insightCloseAddDays);
                        insights.Add(Insight.Price(symbolData.Symbol, insightCloseTimeUtc, InsightDirection.Down));
                    }
                }
            }

            return insights;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            // added
            foreach (var added in changes.AddedSecurities)
            {
                SymbolData symbolData;
                if (!_symbolDataBySymbol.TryGetValue(added.Symbol, out symbolData))
                {
                    // add symbol/symbolData pair to collection
                    symbolData = new SymbolData(_rangePeriod, _consolidatorTimeSpan)
                    {
                        Symbol = added.Symbol,
                        K1 = _k1,
                        K2 = _k2
                    };

                    _symbolDataBySymbol[added.Symbol] = symbolData;

                    //register consolidator
                    algorithm.SubscriptionManager.AddConsolidator(added.Symbol, symbolData.GetConsolidator());
                }
            }

            // removed
            foreach (var removed in changes.RemovedSecurities)
            {
                SymbolData symbolData;
                if (_symbolDataBySymbol.TryGetValue(removed.Symbol, out symbolData))
                {
                    // unsibscribe consolidator from data updates
                    algorithm.SubscriptionManager.RemoveConsolidator(removed.Symbol, symbolData.GetConsolidator());

                    // remove item from dictionary collection
                    if (!_symbolDataBySymbol.Remove(removed.Symbol))
                    {
                        algorithm.Error("Unable to remove data from collection: DualThrustAlphaModel");
                    }
                }
            }
        }

        /// <summary>
        /// Contains data specific to a symbol required by this model
        /// </summary>
        private class SymbolData
        {
            // rolling to contain items over the looking back period
            private readonly RollingWindow<TradeBar> _rangeWindow;

            // we calculate our logic on bars
            private readonly TradeBarConsolidator _consolidator;

            // current range value
            public decimal Range { get; private set; }

            // upper Line
            public decimal UpperLine { get; private set; }

            // lower Line
            public decimal LowerLine { get; private set; }

            // symbol value
            public Symbol Symbol { get; set; }

            // k1
            public decimal K1 { private get; set; }

            // k2
            public decimal K2 { private get; set; }

            // data is ready when rolling window is ready
            public bool IsReady => _rangeWindow.IsReady;

            /// <summary>
            /// Main constructor for the class
            /// </summary>
            /// <param name="rangePeriod">Range period</param>
            /// <param name="consolidatorResolution">Time length of consolidator</param>
            public SymbolData(int rangePeriod, TimeSpan consolidatorResolution)
            {
                _rangeWindow = new RollingWindow<TradeBar>(rangePeriod);
                _consolidator = new TradeBarConsolidator(consolidatorResolution);

                // event fired at new consolidated trade bar
                _consolidator.DataConsolidated += (sender, consolidated) =>
                {
                    // add new tradebar to
                    _rangeWindow.Add(consolidated);

                    if (IsReady)
                    {
                        var hh = _rangeWindow.Select(x => x.High).Max();
                        var hc = _rangeWindow.Select(x => x.Close).Max();
                        var lc = _rangeWindow.Select(x => x.Close).Min();
                        var ll = _rangeWindow.Select(x => x.Low).Min();

                        Range = Math.Max(hh - lc, hc - ll);

                        UpperLine = consolidated.Close + K1 * Range;
                        LowerLine = consolidated.Close - K2 * Range;
                    }
                };
            }

            /// <summary>
            /// Returns the interior consolidator
            /// </summary>
            public TradeBarConsolidator GetConsolidator()
            {
                return _consolidator;
            }
        }
    }
}
