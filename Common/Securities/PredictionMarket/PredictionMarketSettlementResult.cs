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

namespace QuantConnect.Securities.PredictionMarket
{
    /// <summary>
    /// Represents the settlement result of a prediction market contract
    /// </summary>
    public enum PredictionMarketSettlementResult
    {
        /// <summary>
        /// The contract has not yet settled - outcome is unknown
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The contract settled with a "Yes" outcome - pays $1.00 per contract
        /// </summary>
        Yes = 1,

        /// <summary>
        /// The contract settled with a "No" outcome - pays $0.00 per contract
        /// </summary>
        No = 2
    }
}
