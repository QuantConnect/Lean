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

using Newtonsoft.Json;
using System.Globalization;

namespace QuantConnect.Tests.Optimizer
{
    internal class BacktestResult
    {
        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings { Culture = CultureInfo.InvariantCulture, NullValueHandling = NullValueHandling.Ignore};
        public Statistics Statistics { get; set; }

        public static BacktestResult Create(decimal? profit = null, decimal? drawdown = null)
        {
            return new BacktestResult
            {
                Statistics = new Statistics
                {
                    Profit = profit,
                    Drawdown = drawdown
                }
            };
        }

        public string ToJson() => JsonConvert.SerializeObject(this, _jsonSettings);
    }

    public class Statistics
    {
        public decimal? Profit { get; set; }

        public decimal? Drawdown { get; set; }
    }
}
