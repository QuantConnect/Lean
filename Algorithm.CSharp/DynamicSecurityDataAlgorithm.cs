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
using QuantConnect.Data;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Provides an example algorithm showcasing the <see cref="Security.Data"/> features
    /// </summary>
    public class DynamicSecurityDataAlgorithm : QCAlgorithm
    {
        private Security GOOGL;
        private const string Ticker = "GOOGL";

        public override void Initialize()
        {
            SetStartDate(2015, 10, 22);
            SetEndDate(2015, 10, 30);

            GOOGL = AddEquity(Ticker, Resolution.Daily);

            AddData<SECReport8K>(Ticker);
            AddData<SECReport10Q>(Ticker);
        }

        public override void OnData(Slice slice)
        {
            dynamic securityData = GOOGL.Data;

            // check if a particular type of data is available
            if (GOOGL.Data.HasData<SECReport8K>())
            {
                // access data using C# generics for strong typing
                // GetAll<T> returns an IReadOnlyList<T>
                var sec8k = GOOGL.Data.GetAll<SECReport8K>();
                Log($"GOOGL.Data.GetAll<SECReport8K>(): {sec8k}");

                // access data using dynamic access. this is the recommended access
                // pattern for python as well.
                sec8k = securityData.SECReport8K;
                Log($"GOOGL.Data.SECReport8K: {sec8k}");
            }

            // you can access data of all types in this manner, even for TradeBar data
            if (GOOGL.Data.HasData<TradeBar>())
            {
                // Get<T> returns the last item in the list
                var tradeBar = GOOGL.Data.Get<TradeBar>();
                Log($"GOOGL.Data.Get<TradeBar>(): {JsonConvert.SerializeObject(tradeBar, Formatting.Indented)}");

                // the dynamic accessors always returns an IReadOnlyList<T>
                var tradeBars = securityData.TradeBar;
                Log($"GOOGL.Data.TradeBar: {JsonConvert.SerializeObject(tradeBar, Formatting.Indented)}");
            }
        }
    }
}
