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

using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.Data.Custom.Quiver
{
    /// <summary>
    /// Converts Quiver Quantitative <see cref="TransactionDirection"/> to <see cref="OrderDirection"/>
    /// </summary>
    public class TransactionDirectionJsonConverter : TypeChangeJsonConverter<OrderDirection, string>
    {
        protected override string Convert(OrderDirection value)
        {
            return value == OrderDirection.Buy ? "purchase" : "sale";
        }

        protected override OrderDirection Convert(string value)
        {
            return value.ToLowerInvariant() == "purchase" ? OrderDirection.Buy : OrderDirection.Sell;
        }
    }
}
