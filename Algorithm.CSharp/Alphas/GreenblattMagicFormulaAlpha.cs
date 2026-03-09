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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
    /// <summary>
    /// This alpha picks stocks according to Joel Greenblatt's Magic Formula.
    /// First, each stock is ranked depending on the relative value of the ratio EV/EBITDA. For example, a stock
    /// that has the lowest EV/EBITDA ratio in the security universe receives a score of one while a stock that has
    /// the tenth lowest EV/EBITDA score would be assigned 10 points.
    ///
    /// Then, each stock is ranked and given a score for the second valuation ratio, Return on Capital (ROC).
    /// Similarly, a stock that has the highest ROC value in the universe gets one score point.
    /// The stocks that receive the lowest combined score are chosen for insights.
    ///
    /// Source: Greenblatt, J. (2010) The Little Book That Beats the Market
    ///
    /// This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    /// sourced so the community and client funds can see an example of an alpha.
    ///</summary>
    public class GreenblattMagicFormulaAlpha : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetCash(100000);

            // Set zero transaction fees
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            // Select stocks using MagicFormulaUniverseSelectionModel
            SetUniverseSelection(new GreenBlattMagicFormulaUniverseSelectionModel());

            // Use RateOfChangeAlphaModel to establish insights
            SetAlpha(new RateOfChangeAlphaModel());

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Set Immediate Execution Model
            SetExecution(new ImmediateExecutionModel());

            // Set Null Risk Management Model
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// Uses Rate of Change (ROC) to create magnitude prediction for insights.
        /// </summary>
        private class RateOfChangeAlphaModel : AlphaModel
        {
            private readonly int _lookback;
            private readonly Resolution _resolution;
            private readonly TimeSpan _predictionInterval;
            private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol;

            public RateOfChangeAlphaModel(
                int lookback = 1,
                Resolution resolution = Resolution.Daily)
            {
                _lookback = lookback;
                _resolution = resolution;
                _predictionInterval = resolution.ToTimeSpan().Multiply(lookback);
                _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();
            }

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                var insights = new List<Insight>();

                foreach (var kvp in _symbolDataBySymbol)
                {
                    var symbolData = kvp.Value;
                    if (symbolData.CanEmit)
                    {
                        var magnitude = Convert.ToDouble(Math.Abs(symbolData.Return));
                        insights.Add(Insight.Price(kvp.Key, _predictionInterval, InsightDirection.Up, magnitude));
                    }
                }

                return insights;
            }

            public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
                // Clean up data for removed securities
                foreach (var removed in changes.RemovedSecurities)
                {
                    SymbolData symbolData;
                    if (_symbolDataBySymbol.TryGetValue(removed.Symbol, out symbolData))
                    {
                        symbolData.RemoveConsolidators(algorithm);
                        _symbolDataBySymbol.Remove(removed.Symbol);
                    }
                }

                // Initialize data for added securities
                var symbols = changes.AddedSecurities.Select(x => x.Symbol);
                var history = algorithm.History(symbols, _lookback, _resolution);
                if (symbols.Count() == 0 && history.Count() == 0)
                {
                    return;
                }

                history.PushThrough(bar =>
                {
                    SymbolData symbolData;
                    if (!_symbolDataBySymbol.TryGetValue(bar.Symbol, out symbolData))
                    {
                        symbolData = new SymbolData(algorithm, bar.Symbol, _lookback, _resolution);
                        _symbolDataBySymbol[bar.Symbol] = symbolData;
                    }
                    symbolData.WarmUpIndicators(bar);
                });
            }

            /// <summary>
            /// Contains data specific to a symbol required by this model
            /// </summary>
            private class SymbolData
            {
                private readonly Symbol _symbol;
                private readonly IDataConsolidator _consolidator;
                private long _previous = 0;

                public RateOfChange Return { get; }

                public bool CanEmit
                {
                    get
                    {
                        if (_previous == Return.Samples)
                        {
                            return false;
                        }
                        _previous = Return.Samples;
                        return Return.IsReady;
                    }
                }

                public SymbolData(QCAlgorithm algorithm, Symbol symbol, int lookback, Resolution resolution)
                {
                    _symbol = symbol;
                    Return = new RateOfChange($"{symbol}.ROC({lookback})", lookback);
                    _consolidator = algorithm.ResolveConsolidator(symbol, resolution);
                    algorithm.RegisterIndicator(symbol, Return, _consolidator);
                }

                internal void RemoveConsolidators(QCAlgorithm algorithm)
                {
                    algorithm.SubscriptionManager.RemoveConsolidator(_symbol, _consolidator);
                }

                internal void WarmUpIndicators(BaseData bar)
                {
                    Return.Update(bar.EndTime, bar.Value);
                }
            }
        }

        /// <summary>
        /// Defines a universe according to Joel Greenblatt's Magic Formula, as a universe selection model for the framework algorithm.
        /// From the universe QC500, stocks are ranked using the valuation ratios, Enterprise Value to EBITDA(EV/EBITDA) and Return on Assets(ROA).
        /// </summary>
        private class GreenBlattMagicFormulaUniverseSelectionModel : FundamentalUniverseSelectionModel
        {
            private const int _numberOfSymbolsCoarse = 500;
            private const int _numberOfSymbolsFine = 20;
            private const int _numberOfSymbolsInPortfolio = 10;
            private int _lastMonth = -1;
            private Dictionary<Symbol, double> _dollarVolumeBySymbol;

            public GreenBlattMagicFormulaUniverseSelectionModel() : base(true)
            {
                _dollarVolumeBySymbol = new ();
            }

            /// <summary>
            /// Performs coarse selection for constituents.
            /// The stocks must have fundamental data
            /// The stock must have positive previous-day close price
            /// The stock must have positive volume on the previous trading day
            /// </summary>
            public override IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm, IEnumerable<CoarseFundamental> coarse)
            {
                if (algorithm.Time.Month == _lastMonth)
                {
                    return algorithm.Universe.Unchanged;
                }
                _lastMonth = algorithm.Time.Month;

                _dollarVolumeBySymbol = (
                    from cf in coarse
                    where cf.HasFundamentalData
                    orderby cf.DollarVolume descending
                    select new { cf.Symbol, cf.DollarVolume }
                    )
                    .Take(_numberOfSymbolsCoarse)
                    .ToDictionary(x => x.Symbol, x => x.DollarVolume);

                return _dollarVolumeBySymbol.Keys;
            }

            /// <summary>
            /// QC500: Performs fine selection for the coarse selection constituents
            /// The company's headquarter must in the U.S.
            /// The stock must be traded on either the NYSE or NASDAQ
            /// At least half a year since its initial public offering
            /// The stock's market cap must be greater than 500 million
            ///
            /// Magic Formula: Rank stocks by Enterprise Value to EBITDA(EV/EBITDA)
            /// Rank subset of previously ranked stocks(EV/EBITDA), using the valuation ratio Return on Assets(ROA)
            /// </summary>
            public override IEnumerable<Symbol> SelectFine(QCAlgorithm algorithm, IEnumerable<FineFundamental> fine)
            {
                var filteredFine =
                    from x in fine
                    where x.CompanyReference.CountryId == "USA"
                    where x.CompanyReference.PrimaryExchangeID == "NYS" || x.CompanyReference.PrimaryExchangeID == "NAS"
                    where (algorithm.Time - x.SecurityReference.IPODate).TotalDays > 180
                    where x.EarningReports.BasicAverageShares.ThreeMonths * x.EarningReports.BasicEPS.TwelveMonths * x.ValuationRatios.PERatio > 5e8
                    select x;

                double count = filteredFine.Count();
                if (count == 0)
                {
                    return Enumerable.Empty<Symbol>();
                }

                var percent = _numberOfSymbolsFine / count;

                // Select stocks with top dollar volume in every single sector
                var myDict = (
                    from x in filteredFine
                    group x by x.CompanyReference.IndustryTemplateCode into g
                    let y = (
                        from item in g
                        orderby _dollarVolumeBySymbol[item.Symbol] descending
                        select item
                    )
                    let c = (int)Math.Ceiling(y.Count() * percent)
                    select new { g.Key, Value = y.Take(c) }
                    )
                    .ToDictionary(x => x.Key, x => x.Value);

                // Stocks in QC500 universe
                var topFine = myDict.Values.SelectMany(x => x);

                // Magic Formula:
                // Rank stocks by Enterprise Value to EBITDA (EV/EBITDA)
                // Rank subset of previously ranked stocks (EV/EBITDA), using the valuation ratio Return on Assets (ROA)
                return topFine
                    // Sort stocks in the security universe of QC500 based on Enterprise Value to EBITDA valuation ratio
                    .OrderByDescending(x => x.ValuationRatios.EVToEBITDA)
                    .Take(_numberOfSymbolsFine)
                    // sort subset of stocks that have been sorted by Enterprise Value to EBITDA, based on the valuation ratio Return on Assets (ROA)
                    .OrderByDescending(x => x.ValuationRatios.ForwardROA)
                    .Take(_numberOfSymbolsInPortfolio)
                    .Select(x => x.Symbol);
            }
        }
    }
}
