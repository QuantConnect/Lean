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

using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends an array of current portfolio targets to Numerai API
    /// every time the ema indicators crosses between themselves. 
    /// See (https://docs.numer.ai/numerai-signals/signals-overview) for more information
    /// about accepted symbols, signals, etc.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class NumeraiSignalExportDemonstrationAlgorithm : QCAlgorithm
    {
        private readonly List<Security> _securities = new();
        private Symbol _etfSymbol;
        public override void Initialize()
        {
            SetStartDate(2020, 10, 7);   // Set Start Date
            SetEndDate(2020, 10, 12);    // Set End Date
            SetCash(100000);             // Set Strategy Cash

            SetSecurityInitializer(new BrokerageModelSecurityInitializer(BrokerageModel, new FuncSecuritySeeder(GetLastKnownPrices)));

            // Add the CRSP US Total Market Index constituents, which represents approximately 100% of the investable US Equity market
            _etfSymbol = AddEquity("VTI").Symbol;
            AddUniverse(Universe.ETF(_etfSymbol));

            // Create a Scheduled Event to submit signals every trading day at 13:00 UTC
            Schedule.On(DateRules.EveryDay(_etfSymbol), TimeRules.At(13, 0, TimeZones.Utc), SubmitSignals);

            // Add the Numerai signal export provider
            // Numerai Public ID: This value is provided by Numerai Signals in their main webpage once you've logged in
            // and created a API key. See (https://signals.numer.ai/account)
            var numeraiPublicId = "";

            // Numerai Secret ID: This value is provided by Numerai Signals in their main webpage once you've logged in
            // and created a API key. See (https://signals.numer.ai/account)
            var numeraiSecretId = "";

            // Numerai Model ID: This value is provided by Numerai Signals in their main webpage once you've logged in
            // and created a model. See (https://signals.numer.ai/models)
            var numeraiModelId = "";

            var numeraiFilename = ""; // (Optional) Replace this value with your submission filename 

            // Disable automatic exports as we manually set them
            SignalExport.AutomaticExportTimeSpan = null;

            // Set Numerai signal export provider
            SignalExport.AddSignalExportProvider(new NumeraiSignalExport(numeraiPublicId, numeraiSecretId, numeraiModelId, numeraiFilename));
        }

        public void SubmitSignals()
        {
            // Select the subset of ETF constituents we can trade
            var symbols = _securities.Where(security => security.HasData)
                            .Select(security => security.Symbol)
                            .OrderBy(symbol => symbol)
                            .ToList();
            if (symbols.Count == 0)
            {
                return;
            }

            // Get historical data
            // var history = History(symbols, 22, Resolution.Daily);

            // Create portfolio targets
            //  Numerai requires that at least one of the signals have a unique weight
            //  To ensure they are all unique, this demo gives a linear allocation to each symbol (ie. 1/55, 2/55, ..., 10/55)
            var denominator = symbols.Count * (symbols.Count + 1) / 2.0m; // sum of 1, 2, ..., symbols.Count
            var targets = symbols.Select((symbol, i) => new PortfolioTarget(symbol, (i + 1) / denominator)).ToList();

            // (Optional) Place trades
            SetHoldings(targets);

            // Send signals to Numerai
            var success = SignalExport.SetTargetPortfolio(targets.ToArray());
            if (!success)
            {
                Debug($"Couldn't send targets at {Time}");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.RemovedSecurities)
            {
                if (_securities.Contains(security))
                {
                    _securities.Remove(security);
                }
            }

            foreach (var security in changes.AddedSecurities)
            {
                if (security.Symbol != _etfSymbol)
                {
                    _securities.Add(security);
                }
            }
        }
    }
}
