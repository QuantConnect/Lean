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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    public class SECReport8KAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 8, 21);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Minute;
            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseSelector));

            // Request underlying equity data.
            var ibm = AddEquity("IBM", Resolution.Minute).Symbol;
            // Add SEC report 10-Q data for the underlying IBM asset
            var earningsFiling = AddData<SECReport10Q>(ibm).Symbol;
            // Request 120 days of history with the SECReport10Q IBM custom data Symbol.
            var history = History<SECReport10Q>(earningsFiling, 120, Resolution.Daily);

            // Count the number of items we get from our history request
            Debug($"We got {history.Count()} items from our history request");
        }

        public IEnumerable<Symbol> CoarseSelector(IEnumerable<CoarseFundamental> coarse)
        {
            // Add SEC data from the filtered coarse selection
            var symbols = coarse.Where(x => x.HasFundamentalData && x.DollarVolume > 50000000)
                .Select(x => x.Symbol)
                .Take(10);

            foreach (var symbol in symbols)
            {
                AddData<SECReport8K>(symbol);
            }

            return symbols;
        }

        public override void OnData(Slice data)
        {
            // Store the symbols we want to long in a list
            // so that we can have an equal-weighted portfolio
            var longEquitySymbols = new List<Symbol>();

            // Get all SEC data and loop over it
            foreach (var report in data.Get<SECReport8K>().Values)
            {
                // Get the length of all contents contained within the report
                var reportTextLength = report.Report.Documents.Select(x => x.Text.Length).Sum();

                if (reportTextLength > 20000)
                {
                    longEquitySymbols.Add(report.Symbol.Underlying);
                }
            }

            foreach (var equitySymbol in longEquitySymbols)
            {
                SetHoldings(equitySymbol, 1m / longEquitySymbols.Count);
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var r in changes.RemovedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.Equity))
            {
                // If removed from the universe, liquidate and remove the custom data from the algorithm
                Liquidate(r.Symbol);
                RemoveSecurity(QuantConnect.Symbol.CreateBase(typeof(SECReport8K), r.Symbol, Market.USA));
            }
        }
    }
}
