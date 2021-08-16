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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm consuming an alpha streams portfolio state and trading based on it
    /// </summary>
    public class AlphaStreamsUniverseSelectionTemplateAlgorithm : AlphaStreamsBasicTemplateAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 04, 04);
            SetEndDate(2018, 04, 06);

            SetExecution(new ImmediateExecutionModel());
            Settings.MinimumOrderMarginPortfolioPercentage = 0.01m;
            SetPortfolioConstruction(new EqualWeightingAlphaStreamsPortfolioConstructionModel());

            SetUniverseSelection(new ScheduledUniverseSelectionModel(
                DateRules.EveryDay(),
                TimeRules.Midnight,
                SelectAlphas,
                new UniverseSettings(UniverseSettings)
                {
                    SubscriptionDataTypes = new List<Tuple<Type, TickType>>
                        {new(typeof(AlphaStreamsPortfolioState), TickType.Trade)},
                    FillForward = false,
                }
            ));
        }

        private IEnumerable<Symbol> SelectAlphas(DateTime dateTime)
        {
            Log($"SelectAlphas() {Time}");
            foreach (var alphaId in new[] {"623b06b231eb1cc1aa3643a46", "9fc8ef73792331b11dbd5429a"})
            {
                var alphaSymbol = new Symbol(SecurityIdentifier.GenerateBase(typeof(AlphaStreamsPortfolioState), alphaId, Market.USA),
                        alphaId);

                yield return alphaSymbol;
            }
        }


        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics
        {
            get
            {
                var result = base.ExpectedStatistics;
                result["Compounding Annual Return"] = "-13.200%";
                result["Information Ratio"] = "2.827";
                result["Tracking Error"] = "0.248";
                result["Fitness Score"] = "0.011";
                result["Return Over Maximum Drawdown"] = "-113.513";
                result["Portfolio Turnover"] = "0.023";
                return result;
            }
        }
    }
}
