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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template framework algorithm uses framework components to define the algorithm.
    /// Liquid ETF Competition template
    /// </summary>
    /// <meta name="tag" content="competition" />
    /// <meta name="tag" content="alpha stream" />
    /// <meta name="tag" content="using quantconnect" />
    public class LiquidETFUniverseFrameworkAlgorithm : QCAlgorithm
    {
        // List of symbols we want to trade. Set it in OnSecuritiesChanged
        private readonly List<Symbol> _symbols = new List<Symbol>();

        public override void Initialize()
        {
            // Set Start Date so that backtest has 5+ years of data
            SetStartDate(2014, 11, 1);
            // No need to set End Date as the final submission will be tested
            // up until the review date

            // Set $1m Strategy Cash to trade significant AUM
            SetCash(1000000);

            // Add a relevant benchmark, with the default being SPY
            SetBenchmark("SPY");

            // Use the Alpha Streams Brokerage Model, developed in conjunction with
            // funds to model their actual fees, costs, etc.
            // Please do not add any additional reality modelling, such as Slippage, Fees, Buying Power, etc.
            SetBrokerageModel(new AlphaStreamsBrokerageModel());

            // Use the LiquidETFUniverse with minute-resolution data
            UniverseSettings.Resolution = Resolution.Minute;
            SetUniverseSelection(new LiquidETFUniverse());

            // Optional
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
        }

        public override void OnData(Slice slice)
        {
            if (_symbols.All(x => Portfolio[x].Invested))
            {
                return;
            }

            var insights = _symbols.Where(x => Securities[x].Price > 0)
                .Select(x => Insight.Price(x, TimeSpan.FromDays(1), InsightDirection.Up))
                .ToArray();

            if (insights.Length > 0)
            {
                EmitInsights(insights);
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            // Set symbols as the Inverse Energy ETFs
            foreach (var security in changes.AddedSecurities)
            {
                if (LiquidETFUniverse.Energy.Inverse.Contains(security.Symbol))
                {
                    _symbols.Add(security.Symbol);
                }
            }

            // Print out the information about the groups
            Log($"Energy: {LiquidETFUniverse.Energy}");
            Log($"Metals: {LiquidETFUniverse.Metals}");
            Log($"Technology: {LiquidETFUniverse.Technology}");
            Log($"Treasuries: {LiquidETFUniverse.Treasuries}");
            Log($"Volatility: {LiquidETFUniverse.Volatility}");
            Log($"SP500Sectors: {LiquidETFUniverse.SP500Sectors}");
        }
    }
}