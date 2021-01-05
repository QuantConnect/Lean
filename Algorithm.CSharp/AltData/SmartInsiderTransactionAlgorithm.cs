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
using QuantConnect.Data.Custom.SmartInsider;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    public class SmartInsiderTransactionAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2019, 3, 1);
            SetEndDate(2019, 7, 4);
            SetCash(1000000);

            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseUniverse));

            // Request underlying equity data.
            var ibm = AddEquity("IBM", Resolution.Minute).Symbol;
            // Add Smart Insider stock buyback transaction data for the underlying IBM asset
            var si = AddData<SmartInsiderTransaction>(ibm).Symbol;
            // Request 60 days of history with the SmartInsiderTransaction IBM Custom Data Symbol.
            var history = History<SmartInsiderTransaction>(si, 60, Resolution.Daily);

            // Count the number of items we get from our history request
            Debug($"We got {history.Count()} items from our history request");
        }

        public IEnumerable<Symbol> CoarseUniverse(IEnumerable<CoarseFundamental> coarse)
        {
            var symbols = coarse.Where(x => x.HasFundamentalData && x.DollarVolume > 50000000)
                .Select(x => x.Symbol)
                .Take(10);

            foreach (var symbol in symbols)
            {
                AddData<SmartInsiderTransaction>(symbol);
            }

            return symbols;
        }

        public override void OnData(Slice data)
        {
            // Get all SmartInsider data available
            var transactions = data.Get<SmartInsiderTransaction>();

            foreach (var transaction in transactions.Values)
            {
                if (transaction.VolumePercentage == null || transaction.EventType == null)
                {
                    continue;
                }

                // Using the Smart Insider transaction information, buy when company does a stock buyback
                if (transaction.EventType == SmartInsiderEventType.Transaction && transaction.VolumePercentage > 5)
                {
                    SetHoldings(transaction.Symbol.Underlying, (decimal)transaction.VolumePercentage / 100);
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var r in changes.RemovedSecurities)
            {
                // If removed from the universe, liquidate and remove the custom data from the algorithm
                Liquidate(r.Symbol);
                RemoveSecurity(QuantConnect.Symbol.CreateBase(typeof(SmartInsiderTransaction), r.Symbol, Market.USA));
            }
        }
    }
}
