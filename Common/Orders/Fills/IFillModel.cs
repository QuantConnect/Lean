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

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Represents a model that simulates order fill events
    /// </summary>
    /// <remarks>Please use<see cref="FillModel"/> as the base class for
    /// any implementations of<see cref="IFillModel"/></remarks>
    public interface IFillModel
    {
        /// <summary>
        /// Return an order event with the fill details
        /// </summary>
        /// <param name="parameters">A <see cref="FillModelParameters"/> object containing the security and order</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        Fill Fill(FillModelParameters parameters);
    }
}