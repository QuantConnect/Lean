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

using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the new symbol, on a security changed event,
    /// is added to the securities collection and is tradable.
    /// This specific algorithm tests the manual rollover with the symbol changed event
    /// that is received in the <see cref="OnSymbolChangedEvents(SymbolChangedEvents)"/> handler.
    /// </summary>
    public class ManualContinuousFuturesPositionRolloverFromSymbolChangedEventHandlerRegressionAlgorithm
        : ManualContinuousFuturesPositionRolloverRegressionAlgorithm
    {
        public override void OnSymbolChangedEvents(SymbolChangedEvents symbolsChanged)
        {
            if (!Portfolio.Invested)
            {
                return;
            }

            ManualPositionsRollover(symbolsChanged);
        }
    }
}
