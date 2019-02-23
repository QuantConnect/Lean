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

using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders.Fees;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
    /// <summary>
    /// Identify "pumped" penny stocks and predict that the price of a "Pumped" penny stock reverts to mean

    /// <br><br>This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open sourced so the community and client funds can see an example of an alpha. 
    /// You can read the source code for this alpha on Github in <a href="https://github.com/QuantConnect/Lean/blob/master/Algorithm.CSharp/PumpAndDumpAlpha.cs">C#</a>
    /// or <a href="https://github.com/QuantConnect/Lean/blob/master/Algorithm.Python/Alphas/PumpAndDumpAlpha.py">Python</a>.
    /// </summary>
    public class PumpAndDumpAlpha : QCAlgorithmFramework
    {
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetCash(100000);

            // Set zero transaction fees
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            // Select stocks using PennyStockUniverseSelectionModel
            UniverseSettings.Resolution = Resolution.Daily;
            SetUniverseSelection(new PennyStockUniverseSelectionModel());

            // Use PumpAndDumpAlphaModel to establish insights
            SetAlpha(new PumpAndDumpAlphaModel());

            // Equally weigh securities in portfolio, based on insights
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
        }

        /// <summary>
        /// Performs coarse selection for constituents.
        /// The stocks must have fundamental data
        /// The stock must have positive previous-day close price
        /// The stock must have volume between $1000000 and $10000 on the previous trading day
        /// The stock must cost less than $5'''
        /// </summary>
        private class PennyStockUniverseSelectionModel : FundamentalUniverseSelectionModel
        {
            private const int _numberOfSymbolsCoarse = 500;
            private int _lastMonth = -1;

            public PennyStockUniverseSelectionModel() : base(true, null, null)
            {
            }

            public override IEnumerable<Symbol> SelectCoarse(QCAlgorithmFramework algorithm, IEnumerable<CoarseFundamental> coarse)
            {
                var month = algorithm.Time.Month;
                if (month == _lastMonth)
                {
                    return algorithm.Universe.Unchanged;
                }
                _lastMonth = month;

                return (from cf in coarse
                        where cf.HasFundamentalData
                        where cf.Volume < 1000000
                        where cf.Volume > 10000
                        where cf.Price < 5
                        orderby cf.DollarVolume descending
                        select cf.Symbol).Take(_numberOfSymbolsCoarse);
            }
        }

        /// <summary>
        /// Uses ranking of intraday percentage difference between open price and close price to create magnitude and direction prediction for insights
        /// </summary>
        private class PumpAndDumpAlphaModel : AlphaModel
        {
            private readonly int _numberOfStocks;
            private readonly TimeSpan _predictionInterval;

            public PumpAndDumpAlphaModel(
                int lookback = 1,
                int numberOfStocks = 10,
                Resolution resolution = Resolution.Daily)
            {
                _numberOfStocks = numberOfStocks;
                _predictionInterval = resolution.ToTimeSpan().Multiply(lookback);
            }

            public override IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
            {
                return (
                    from entry in algorithm.ActiveSecurities
                    let security = entry.Value
                    where security.HasData && security.Open > 0
                    // Rank penny stocks on one day price change
                    let Magnitude = security.Close / security.Open - 1
                    orderby -Math.Round(Magnitude, 6), security.Symbol
                    select CreateInsight(security.Symbol, Magnitude))
                    // Retrieve list of _numberOfStocks "pumped" penny stocks
                    .Take(_numberOfStocks);
            }

            private Insight CreateInsight(Symbol symbol, decimal magnitude)
            {
                return Insight.Price(symbol, _predictionInterval, InsightDirection.Down, (double)magnitude);
            }
        }
    }
}