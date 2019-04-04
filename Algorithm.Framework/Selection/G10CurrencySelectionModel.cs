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
using System.Linq;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides an implementation of <see cref="IUniverseSelectionModel"/> that
    /// simply subscribes to G10 currencies
    /// </summary>
    public class G10CurrencySelectionModel : ManualUniverseSelectionModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="G10CurrencySelectionModel"/> class
        /// using the algorithm's security initializer and universe settings
        /// </summary>
        public G10CurrencySelectionModel()
            : base(new[]
            {
                "EURUSD",
                "GBPUSD",
                "USDJPY",
                "AUDUSD",
                "NZDUSD",
                "USDCAD",
                "USDCHF",
                "NOKUSD",
                "SEKUSD"
            }.Select(x => Symbol.Create(x, SecurityType.Forex, Market.Oanda)))
        {
        }
    }
}