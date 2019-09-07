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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    public class CustomDataUnderlyingSymbolMappingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _initialSymbolChangedEvent;
        private Symbol _equitySymbol;
        private Symbol _customDataSymbol;

        /// <summary>
        /// Adds the stock TWX so that we can test if mapping occurs to the underlying symbol in the custom data subscription
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2001, 1, 1);
            SetEndDate(2004, 1, 1);

            _equitySymbol = AddEquity("TWX", Resolution.Daily).Symbol;
            _customDataSymbol = AddData<SECReport8K>(_equitySymbol).Symbol;
        }

        /// <summary>
        /// Checks that custom data underlying symbol matches the equity symbol at the same time step
        /// </summary>
        /// <param name="data"></param>
        public override void OnData(Slice data)
        {
            if (data.SymbolChangedEvents.Any() && !_initialSymbolChangedEvent)
            {
                _initialSymbolChangedEvent = true;
                return;
            }

            if (data.SymbolChangedEvents.Any() && data.ContainsKey(_customDataSymbol) && data.ContainsKey(_equitySymbol))
            {
                if (data[_customDataSymbol].Symbol.Underlying != data[_equitySymbol].Symbol)
                {
                    throw new Exception("Underlying symbol does not match equity symbol after rename event");
                }
            }
        }

        /// <summary>
        /// Can run locally
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// Languages this test is supported for
        /// </summary>
        public Language[] Languages => new[] { Language.CSharp, Language.Python };

        /// <summary>
        /// Expected statistics
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"asdf", "asdf" }
        };
    }
}
