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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests that we only receive the option chain for a single future contract
    /// in the option universe filter.
    /// </summary>
    public class AddFutureOptionSingleOptionChainSelectedInUniverseFilterRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _invested;
        private bool _optionFilterRan;
        private Future _es;

        public override void Initialize()
        {
            SetStartDate(2020, 9, 22);
            SetEndDate(2020, 9, 23);

            _es = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME);
            // Set an absurdly high max expiration date to load all Future contracts from disk.
            _es.SetFilter(0, 100000);

            AddFutureOption(_es.Symbol, contracts =>
            {
                _optionFilterRan = true;

                var expiry = new HashSet<DateTime>(contracts.Select(x => x.Underlying.ID.Date)).SingleOrDefault();
                var symbol = new HashSet<Symbol>(contracts.Select(x => x.Underlying)).SingleOrDefault();

                if (expiry == null || symbol == null)
                {
                    throw new InvalidOperationException("Expected a single Option contract in the chain, found 0 contracts");
                }

                return contracts;
            });
        }

        public override void OnData(Slice data)
        {
            if (_invested || !data.HasData)
            {
                return;
            }

            foreach (var future in data.FuturesChains.Values.Select(x => x.Symbol))
            {
                SetHoldings(future, 0.1m);
                _invested = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            base.OnEndOfAlgorithm();
            if (!_optionFilterRan)
            {
                throw new InvalidOperationException("Option chain filter was never ran");
            }
        }

        public bool CanRunLocally { get; } = true;
        public Language[] Languages { get; } = new[] { Language.CSharp };

        public Dictionary<string, string> ExpectedStatistics { get; }
    }
}
