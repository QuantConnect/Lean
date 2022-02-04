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

using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Brokerages.Binance.Messages;

namespace QuantConnect.Tests.Brokerages.Binance
{
    [TestFixture]
    public class MarginAccountConverterTests
    {
        [Test]
        public void DeserializeJson()
        {
            var json = @"{
                ""borrowEnabled"": true,
                ""marginLevel"": ""11.64405625"",
                ""totalAssetOfBtc"": ""6.82728457"",
                ""totalLiabilityOfBtc"": ""0.58633215"",
                ""totalNetAssetOfBtc"": ""6.24095242"",
                ""tradeEnabled"": true,
                ""transferEnabled"": true,
                ""userAssets"": [
                    {
                        ""asset"": ""BTC"",
                        ""borrowed"": ""0.00000000"",
                        ""free"": ""0.00499500"",
                        ""interest"": ""0.00000000"",
                        ""locked"": ""0.00000000"",
                        ""netAsset"": ""0.00499500""
                    },
                    {
                        ""asset"": ""BNB"",
                        ""borrowed"": ""201.66666672"",
                        ""free"": ""2346.50000000"",
                        ""interest"": ""0.00000000"",
                        ""locked"": ""0.00000000"",
                        ""netAsset"": ""2144.83333328""
                    },
                    {
                        ""asset"": ""ETH"",
                        ""borrowed"": ""0.00000000"",
                        ""free"": ""0.00000000"",
                        ""interest"": ""0.00000000"",
                        ""locked"": ""0.00000000"",
                        ""netAsset"": ""0.00000000""
                    },
                    {
                        ""asset"": ""USDT"",
                        ""borrowed"": ""0.00000000"",
                        ""free"": ""0.00000000"",
                        ""interest"": ""0.00000000"",
                        ""locked"": ""0.00000000"",
                        ""netAsset"": ""0.00000000""
                    }
                ]
            }";

            var balances = JsonConvert.DeserializeObject<AccountInformation>(json, new MarginAccountConverter()).Balances
                .Cast<MarginBalance>()
                .ToArray();

            Assert.AreEqual(4, balances.Length);
            var bnb = balances.FirstOrDefault(a => a.Asset == "BNB");
            Assert.NotNull(bnb);
            Assert.AreEqual(201.66666672m, bnb.Borrowed);
            Assert.AreEqual(2144.83333328m, bnb.NetAsset);
            Assert.AreEqual(bnb.NetAsset, bnb.Amount);
        }
    }
}
