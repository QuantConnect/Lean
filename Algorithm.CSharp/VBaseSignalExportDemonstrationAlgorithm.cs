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

using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using System.Collections.Generic;
using PortfolioTarget = QuantConnect.Algorithm.Framework.Portfolio.PortfolioTarget;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends a list of portfolio targets to vBsase API
    /// </summary>
    public class VBaseSignalExportDemonstrationAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// vBase API KEY: This value is provided by vBase in your profile section
        /// See API documentation at https://docs.vbase.com/getting-started/rest-api-user-guide
        /// </summary>
        private const string _vbaseApiKey = "YOUR API KEY";
        private const string _vbaseCollectionName = "YOUR COLLECTION";

        private PortfolioTarget[] _targets = new PortfolioTarget[4];

        private List<Symbol> _symbols = new()
        {
            QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA),
            QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
            QuantConnect.Symbol.Create("MSFT", SecurityType.Equity, Market.USA),
            QuantConnect.Symbol.Create("NVDA", SecurityType.Equity, Market.USA),
        };


        /// <summary>
        /// Stamping of predefined portfolio targets
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 06, 09);
            SetEndDate(2014, 06, 09);
            SetCash(100000);

            for (var index = 0; index < _symbols.Count; index++)
            {
                var symbol = _symbols[index];
                _targets[index] = new PortfolioTarget(symbol, (decimal)0.25);
            }

            // Add vBase signal export provider
            SignalExport.AddSignalExportProvider(new VBaseSignalExport(_vbaseApiKey, _vbaseCollectionName));
        }

        public override void OnData(Slice slice)
        {
            SignalExport.SetTargetPortfolio(_targets);
        }

        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

    }
}
