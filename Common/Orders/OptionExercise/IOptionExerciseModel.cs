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

using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using System.Collections.Generic;

namespace QuantConnect.Orders.OptionExercise
{
    /// <summary>
    /// Represents a model that simulates option exercise and lapse events
    /// </summary>
    public interface IOptionExerciseModel
    {

        /// <summary>
        /// Model the option exercise 
        /// </summary>
        /// <param name="option">Option we're trading this order</param>
        /// <param name="order">Order to update</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        IEnumerable<OrderEvent> OptionExercise(Option option, OptionExerciseOrder order);

    }
}
