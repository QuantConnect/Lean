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

using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm tests the functionality of the TickConsolidator with tick data.
    /// The SubscriptionManager.AddConsolidator method uses a Trade TickType
    /// It checks if data consolidation occurs as expected for the given time period. If consolidation does not happen, a RegressionTestException is thrown.
    /// </summary>
    public class TickTradeBarConsolidatorWithTradeTickTypeRegressionAlgorithm : TickTradeBarConsolidatorWithDefaultTickTypeRegressionAlgorithm
    {
        protected override void AddConsolidator(TickConsolidator consolidator)
        {
            SubscriptionManager.AddConsolidator(GoldFuture.Mapped, consolidator, TickType.Trade);
        }
    }
}