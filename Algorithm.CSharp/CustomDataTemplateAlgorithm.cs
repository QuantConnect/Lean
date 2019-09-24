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
 *
*/

using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Template algorithm combining coarse selection with custom data
    /// </summary>
    public class CustomDataTemplateAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int NumberOfSymbols = 3;

        public override void Initialize()
        {
            SetStartDate(2014, 03, 25);  //Set Start Date
            SetEndDate(2014, 04, 07);    //Set End Date
            SetCash(50000);             //Set Strategy Cash

            // what resolution should the data *added* to the universe be?
            UniverseSettings.Resolution = Resolution.Daily;
            // this add universe method accepts a single parameter that is a function that
            // accepts an IEnumerable<CoarseFundamental> and returns IEnumerable<Symbol>
            AddUniverse(CoarseSelectionFunction);
        }

        // sort the data by daily dollar volume and take the top 'NumberOfSymbols'
        public static IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            // sort descending by daily dollar volume
            var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume);

            // take the top entries from our sorted collection
            var top = sortedByDollarVolume.Take(NumberOfSymbols);

            // we need to return only the symbol objects
            return top.Select(x => x.Symbol);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            // Trading logic could be place here
        }

        /// <summary>
        /// Here we will add and remove our custom data types based on the securities being added by
        /// the coarse selection method
        /// </summary>
        /// <remarks>We want to make sure we remove our custom data types to avoid unnecessary
        /// resource consumption</remarks>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.Equity))
            {
                AddData<SECReport8K>(added.Symbol, Resolution.Daily);
                AddData<SECReport10K>(added.Symbol, Resolution.Daily);
            }

            foreach (var removed in changes.RemovedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.Equity))
            {
                RemoveCustomSecurities(removed.Symbol, typeof(SECReport8K), typeof(SECReport10K));
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"}
        };
    }
}
