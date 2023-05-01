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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the model responsible for applying cash settlement rules
    /// </summary>
    public interface ISettlementModel
    {
        /// <summary>
        /// Applies cash settlement rules
        /// </summary>
        /// <param name="applyFundsParameters">The funds application parameters</param>
        void ApplyFunds(ApplyFundsSettlementModelParameters applyFundsParameters);

        /// <summary>
        /// Scan for pending settlements
        /// </summary>
        /// <param name="settlementParameters">The settlement parameters</param>
        void Scan(ScanSettlementModelParameters settlementParameters);
    }
}
