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

using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the proceeds of the automatic delisting liquidation of an equity held on a cash account,
    /// which is a member of more than one universe, settle and that the delisted security is removed from all its universes afterwards
    /// </summary>
    public class DelistedEquityInMultipleUniversesCashAccountSettlementRegressionAlgorithm : DelistedEquityCashAccountSettlementRegressionAlgorithm
    {
        /// <summary>
        /// Adds the equity that gets delisted, also selected by a framework universe
        /// </summary>
        protected override Symbol AddDelistedEquity()
        {
            var symbol = base.AddDelistedEquity();
            AddUniverseSelection(new ManualUniverseSelectionModel(symbol));
            return symbol;
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 89;
    }
}
